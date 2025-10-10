using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside;

public partial class WindowEffectsSettingsWindow : Window
{
    private readonly PluginManager _manager;
    private ComboBox? _componentBox;
    private StackPanel? _effectPanel;
    private readonly Dictionary<string, CheckBox> _effectBoxes = new();

    // Parameterless constructor for designer/XAML loader
    public WindowEffectsSettingsWindow() : this(new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify))
    {
    }

    public WindowEffectsSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(WindowEffectsSettingsWindow));

        _componentBox = this.FindControl<ComboBox>("ComponentBox");
        _effectPanel = this.FindControl<StackPanel>("EffectPanel");

        BuildComponents();
        BuildEffects();

        if (_componentBox != null)
        {
            _componentBox.SelectionChanged += (_, _) =>
            {
                var key = GetSelectedKey();
                if (key != null) LoadConfigForKey(key);
            };
            // Initialize selection
            _componentBox.SelectedIndex = 0;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildComponents()
    {
        if (_componentBox == null) return;
        var items = new List<string> { "Global (*)", "MainWindow" };
        items.AddRange(_manager.Plugins.Select(p => p.Name));
        _componentBox.ItemsSource = items.Distinct().OrderBy(x => x == "Global (*)" ? string.Empty : x).ToList();
    }

    private void BuildEffects()
    {
        if (_effectPanel == null) return;
        _effectPanel.Children.Clear();
        _effectBoxes.Clear();

        foreach (var name in WindowEffectsManager.Instance.GetRegisteredEffectNames())
        {
            var cb = new CheckBox { Content = name };
            _effectPanel.Children.Add(cb);
            _effectBoxes[name] = cb;
        }
    }

    private string? GetSelectedKey()
    {
        var label = _componentBox?.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(label)) return null;
        return label!.StartsWith("Global") ? "*" : label;
    }

    private void LoadConfigForKey(string key)
    {
        var map = SettingsManager.Settings.WindowEffects;
        var current = map.TryGetValue(key, out var list) ? list : new List<string>();

        foreach (var kv in _effectBoxes)
        {
            kv.Value.IsChecked = current.Contains(kv.Key);
        }
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var key = GetSelectedKey();
        if (key == null) return;

        var selected = _effectBoxes.Where(kv => kv.Value.IsChecked == true).Select(kv => kv.Key).ToList();

        var map = SettingsManager.Settings.WindowEffects;
        if (selected.Count == 0)
        {
            if (map.ContainsKey(key)) map.Remove(key);
        }
        else
        {
            map[key] = selected;
        }

        SettingsManager.Save();

        var msg = new WindowEffectsSettingsWindow.MessageWindow("Settings Saved", "Window effect settings have been saved. New windows will apply updated effects.");
        msg.ShowDialog(this);
        Close();
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private class MessageWindow : Window
    {
        public MessageWindow(string title, string message)
        {
            Title = title;
            Width = 360;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var msg = new TextBlock
            {
                Text = message,
                Margin = new Thickness(15),
                TextWrapping = TextWrapping.Wrap
            };

            var ok = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(5) };
            ok.Click += (_, _) => Close();

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(msg);
            panel.Children.Add(new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Children = { ok }
            });
            Content = panel;
        }
    }
}
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
    private readonly Dictionary<string, Dictionary<string, TextBox>> _paramInputs = new();

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
            var container = new StackPanel { Spacing = 4 };
            var cb = new CheckBox { Content = name };
            container.Children.Add(cb);
            _effectBoxes[name] = cb;

            // Known parameters for specific effects
            var fields = new Dictionary<string, TextBox>();
            if (name == "MagicLampMinimize")
            {
                fields["DurationMs"] = CreateParamRow(container, "DurationMs", "220");
                fields["SquashFactor"] = CreateParamRow(container, "SquashFactor", "0.75");
                fields["MinHeight"] = CreateParamRow(container, "MinHeight", "40");
            }
            else if (name == "BeamUpMinimize")
            {
                fields["DurationMs"] = CreateParamRow(container, "DurationMs", "220");
                fields["OffsetY"] = CreateParamRow(container, "OffsetY", "-80");
            }

            if (fields.Count > 0)
            {
                _paramInputs[name] = fields;
            }
            _effectPanel.Children.Add(container);
        }

        // Preload parameter values from global settings
        LoadGlobalEffectParameters();
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

    private void LoadGlobalEffectParameters()
    {
        var global = SettingsManager.Settings.WindowEffectParameters;
        foreach (var effect in _paramInputs)
        {
            if (global.TryGetValue(effect.Key, out var pmap))
            {
                foreach (var field in effect.Value)
                {
                    if (pmap.TryGetValue(field.Key, out var val))
                    {
                        field.Value.Text = val;
                    }
                }
            }
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

        // Save global parameters
        var global = SettingsManager.Settings.WindowEffectParameters;
        foreach (var effect in _paramInputs)
        {
            if (!global.TryGetValue(effect.Key, out var pmap))
            {
                pmap = new Dictionary<string, string>();
                global[effect.Key] = pmap;
            }
            foreach (var field in effect.Value)
            {
                pmap[field.Key] = field.Value.Text ?? string.Empty;
            }
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

    private TextBox CreateParamRow(Panel container, string name, string placeholder)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
        var label = new TextBlock { Text = name, Width = 120 };
        var tb = new TextBox { Watermark = placeholder, Width = 140 };
        row.Children.Add(label);
        row.Children.Add(tb);
        container.Children.Add(row);
        return tb;
    }
}
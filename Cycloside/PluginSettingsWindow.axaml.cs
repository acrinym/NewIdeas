using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using Avalonia.Media;
using System.Linq;
using System;
using System.Diagnostics;

namespace Cycloside;

public partial class PluginSettingsWindow : Window
{
    private readonly PluginManager _manager;

    public PluginSettingsWindow()
    {
        InitializeComponent();
        _manager = null!;
    }

    public PluginSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();

        ThemeManager.ApplyFromSettings(this, "Plugins");
        CursorManager.ApplyFromSettings(this, "Plugins");
        SkinManager.LoadForWindow(this);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(PluginSettingsWindow));

        BuildList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildList()
    {
        var panel = this.FindControl<StackPanel>("PluginsPanel");
        panel.Children.Clear();

        void AddPluginItem(IPlugin plugin)
        {
            var status = _manager.GetStatus(plugin);
            var label = plugin.Name;
            if (status == Plugins.PluginChangeStatus.New) label += " (NEW)";
            else if (status == Plugins.PluginChangeStatus.Updated) label += " (UPDATED)";

            var cb = new CheckBox
            {
                Content = label + (plugin.Description != null ? $" - {plugin.Description}" : "") +
                          (!string.IsNullOrEmpty(plugin.Version?.ToString()) ? $" ({plugin.Version})" : ""),
                Margin = new Thickness(0, 0, 0, 4),
                IsChecked = _manager.IsEnabled(plugin),
                Tag = plugin
            };
            cb.IsCheckedChanged += Toggle;
            panel.Children.Add(cb);
        }

        var newPlugins = _manager.Plugins.Where(p => _manager.GetStatus(p) != Plugins.PluginChangeStatus.None).ToList();
        if (newPlugins.Count > 0)
        {
            panel.Children.Add(new TextBlock { Text = "New or Updated", FontWeight = FontWeight.Bold, Margin = new Thickness(0,0,0,4) });
            foreach (var p in newPlugins)
                AddPluginItem(p);
            panel.Children.Add(new Separator { Margin = new Thickness(0,4,0,4) });
        }

        foreach (var plugin in _manager.Plugins.Except(newPlugins))
        {
            AddPluginItem(plugin);
        }
    }

    // Handles checkbox toggle
    private void Toggle(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.Tag is not IPlugin plugin)
            return;

        if (cb.IsChecked == true)
            _manager.EnablePlugin(plugin);
        else
            _manager.DisablePlugin(plugin);

        SettingsManager.Settings.PluginEnabled[plugin.Name] = cb.IsChecked == true;
        SettingsManager.Save();
    }

    private void ReloadButton_Click(object? sender, RoutedEventArgs e)
    {
        _manager.ReloadPlugins();
        BuildList();
    }

    private void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = _manager.PluginDirectory;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch { }
    }
}

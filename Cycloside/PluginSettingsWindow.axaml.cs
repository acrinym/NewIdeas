using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using System;
using System.Diagnostics;

namespace Cycloside;

public partial class PluginSettingsWindow : Window
{
    private readonly PluginManager _manager;

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
        foreach (var plugin in _manager.Plugins)
        {
            // Merge both content styles and option differences
            var cb = new CheckBox
            {
                // Combine display info from both branches
                Content = $"{plugin.Name}" +
                          (plugin.Description != null ? $" - {plugin.Description}" : "") +
                          (!string.IsNullOrEmpty(plugin.Version?.ToString()) ? $" ({plugin.Version})" : ""),
                Margin = new Thickness(0, 0, 0, 4),
                // Use plugin enabled state from manager or fallback to SettingsManager
                IsChecked = _manager.IsEnabled != null
                    ? _manager.IsEnabled(plugin)
                    : SettingsManager.Settings.PluginEnabled.TryGetValue(plugin.Name, out var en) ? en : true,
                Tag = plugin
            };

            cb.Checked += Toggle;
            cb.Unchecked += Toggle;
            panel.Children.Add(cb);
        }
    }

    // This version will work for both routed event types
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

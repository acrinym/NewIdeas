using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using System;
using System.IO;

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
            var cb = new CheckBox
            {
                Content = $"{plugin.Name} - {plugin.Description} ({plugin.Version})",
                IsChecked = _manager.IsEnabled(plugin),
                Tag = plugin
            };
            cb.Checked += Toggle;
            cb.Unchecked += Toggle;
            panel.Children.Add(cb);
        }
    }

    private void Toggle(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is IPlugin plugin)
        {
            if (cb.IsChecked == true)
                _manager.EnablePlugin(plugin);
            else
                _manager.DisablePlugin(plugin);

            SettingsManager.Settings.PluginEnabled[plugin.Name] = cb.IsChecked ?? false;
            SettingsManager.Save();
        }
    }

    private void ReloadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _manager.ReloadPlugins();
        BuildList();
    }

    private void OpenButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var path = _manager.PluginDirectory;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch { }
    }
}

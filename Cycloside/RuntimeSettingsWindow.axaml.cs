using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using System;
using System.IO;
using System.Linq;
using Cycloside.Services;

namespace Cycloside;

public partial class RuntimeSettingsWindow : Window
{
    private readonly PluginManager _manager;

    public RuntimeSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        this.FindControl<CheckBox>("IsolationBox")!.IsChecked = _manager.IsolationEnabled;
        this.FindControl<CheckBox>("CrashLogBox")!.IsChecked = _manager.CrashLoggingEnabled;
        this.FindControl<CheckBox>("BuiltInBox")!.IsChecked = SettingsManager.Settings.DisableBuiltInPlugins;

        var panel = this.FindControl<StackPanel>("SafePanel");
        if (panel != null)
        {
            panel.Children.Clear();
            foreach (var plugin in _manager.Plugins)
            {
                if (plugin.GetType().Namespace?.Contains("BuiltIn") != true) continue;
                var box = new CheckBox { Content = plugin.Name };
                box.IsChecked = SettingsManager.Settings.SafeBuiltInPlugins.TryGetValue(plugin.Name, out var val) && val;
                panel.Children.Add(box);
            }
        }
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(RuntimeSettingsWindow));
    }

    // Parameterless constructor for designer support
    public RuntimeSettingsWindow() : this(new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify))
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var iso = this.FindControl<CheckBox>("IsolationBox")?.IsChecked ?? true;
        var log = this.FindControl<CheckBox>("CrashLogBox")?.IsChecked ?? true;
        var builtIn = this.FindControl<CheckBox>("BuiltInBox")?.IsChecked ?? false;
        _manager.IsolationEnabled = iso;
        _manager.CrashLoggingEnabled = log;
        SettingsManager.Settings.PluginIsolation = iso;
        SettingsManager.Settings.PluginCrashLogging = log;
        SettingsManager.Settings.DisableBuiltInPlugins = builtIn;

        var panel = this.FindControl<StackPanel>("SafePanel");
        if (panel != null)
        {
            foreach (var child in panel.Children.OfType<CheckBox>())
            {
                SettingsManager.Settings.SafeBuiltInPlugins[child.Content?.ToString() ?? string.Empty] = child.IsChecked ?? false;
            }
        }
        SettingsManager.Save();
        Close();
    }
}

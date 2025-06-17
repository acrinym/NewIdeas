using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using System;
using System.IO;

namespace Cycloside;

public partial class RuntimeSettingsWindow : Window
{
    private readonly PluginManager _manager;

    public RuntimeSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, "Plugins");
        CursorManager.ApplyFromSettings(this, "Plugins");
        SkinManager.LoadForWindow(this);
        this.FindControl<CheckBox>("IsolationBox")!.IsChecked = _manager.IsolationEnabled;
        this.FindControl<CheckBox>("CrashLogBox")!.IsChecked = _manager.CrashLoggingEnabled;
        this.FindControl<CheckBox>("BuiltInBox")!.IsChecked = SettingsManager.Settings.DisableBuiltInPlugins;
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(RuntimeSettingsWindow));
    }

    // Parameterless constructor for designer support
    public RuntimeSettingsWindow() : this(new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), _ => Logger.Log("Designer")))
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
        SettingsManager.Save();
        Close();
    }
}

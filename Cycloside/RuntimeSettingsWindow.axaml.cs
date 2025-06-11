using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;

namespace Cycloside;

public partial class RuntimeSettingsWindow : Window
{
    private readonly PluginManager _manager;

    public RuntimeSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        this.FindControl<CheckBox>("IsolationBox").IsChecked = _manager.IsolationEnabled;
        this.FindControl<CheckBox>("CrashLogBox").IsChecked = _manager.CrashLoggingEnabled;
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(RuntimeSettingsWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var iso = this.FindControl<CheckBox>("IsolationBox").IsChecked ?? true;
        var log = this.FindControl<CheckBox>("CrashLogBox").IsChecked ?? true;
        _manager.IsolationEnabled = iso;
        _manager.CrashLoggingEnabled = log;
        SettingsManager.Settings.PluginIsolation = iso;
        SettingsManager.Settings.PluginCrashLogging = log;
        SettingsManager.Save();
        Close();
    }
}

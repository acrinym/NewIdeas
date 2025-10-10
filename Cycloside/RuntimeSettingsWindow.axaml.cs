using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using System;
using System.IO;
using System.Linq;
using Cycloside.Services;
using Avalonia.Threading;

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

        // Hook PowerShell status updates to UI
        var statusBlock = this.FindControl<TextBlock>("PwshStatusBlock");
        PowerShellManager.StatusChanged += (_, msg) =>
        {
            if (statusBlock != null)
                Dispatcher.UIThread.Post(() => statusBlock.Text = msg);
        };
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

    private async void OnInstallPowerShell(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var installBtn = this.FindControl<Button>("InstallPwshButton");
        var updateBtn = this.FindControl<Button>("UpdatePwshButton");
        var status = this.FindControl<TextBlock>("PwshStatusBlock");
        var arg = this.FindControl<TextBox>("IexArgsBox")?.Text;

        if (installBtn != null) installBtn.IsEnabled = false;
        if (updateBtn != null) updateBtn.IsEnabled = false;
        if (status != null) status.Text = "📥 Installing PowerShell...";

        await PowerShellManager.InstallPowerShellViaIexAsync(string.IsNullOrWhiteSpace(arg) ? null : arg);

        if (installBtn != null) installBtn.IsEnabled = true;
        if (updateBtn != null) updateBtn.IsEnabled = true;
    }

    private async void OnUpdatePowerShell(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var installBtn = this.FindControl<Button>("InstallPwshButton");
        var updateBtn = this.FindControl<Button>("UpdatePwshButton");
        var status = this.FindControl<TextBlock>("PwshStatusBlock");
        var arg = this.FindControl<TextBox>("IexArgsBox")?.Text;

        if (installBtn != null) installBtn.IsEnabled = false;
        if (updateBtn != null) updateBtn.IsEnabled = false;
        if (status != null) status.Text = "🔄 Updating PowerShell...";

        await PowerShellManager.UpdatePowerShellAsync(string.IsNullOrWhiteSpace(arg) ? null : arg);

        if (installBtn != null) installBtn.IsEnabled = true;
        if (updateBtn != null) updateBtn.IsEnabled = true;
    }
}

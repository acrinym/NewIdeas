using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Cycloside.Plugins;
using Cycloside;

namespace Cycloside.ViewModels;

public partial class ControlPanelViewModel : ObservableObject
{
    private readonly PluginManager _manager;
    private readonly Action _closeAction;

    [ObservableProperty]
    private bool launchAtStartup;

    [ObservableProperty]
    private string dotNetPath = string.Empty;

    public ControlPanelViewModel(PluginManager manager, Action closeAction)
    {
        _manager = manager;
        _closeAction = closeAction;
        launchAtStartup = SettingsManager.Settings.LaunchAtStartup;
        dotNetPath = SettingsManager.Settings.DotNetPath;
    }

    [RelayCommand]
    private void Save()
    {
        SettingsManager.Settings.LaunchAtStartup = LaunchAtStartup;
        SettingsManager.Settings.DotNetPath = DotNetPath;
        if (LaunchAtStartup) StartupManager.Enable(); else StartupManager.Disable();
        SettingsManager.Save();
        _closeAction();
    }

    [RelayCommand]
    private void OpenPluginManager() => new PluginSettingsWindow(_manager).Show();

    [RelayCommand]
    private void OpenThemeSettings() => new ThemeSettingsWindow(_manager).Show();

    [RelayCommand]
    private void OpenSkinEditor() => new SkinThemeEditorWindow().Show();

    [RelayCommand]
    private void OpenRuntimeSettings() => new RuntimeSettingsWindow(_manager).Show();
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Cycloside.Plugins;
using Cycloside;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cycloside.ViewModels;

public partial class ControlPanelViewModel : ObservableObject
{
    private readonly PluginManager _manager;
    private readonly Action _closeAction;

    [ObservableProperty]
    private bool launchAtStartup;

    [ObservableProperty]
    private string dotNetPath = string.Empty;

    public ObservableCollection<string> ProfileNames { get; } = new();

    [ObservableProperty]
    private string? selectedProfile;

    public ControlPanelViewModel(PluginManager manager, Action closeAction)
    {
        _manager = manager;
        _closeAction = closeAction;
        launchAtStartup = SettingsManager.Settings.LaunchAtStartup;
        dotNetPath = SettingsManager.Settings.DotNetPath;

        foreach (var name in WorkspaceProfiles.ProfileNames)
            ProfileNames.Add(name);
        selectedProfile = SettingsManager.Settings.ActiveProfile;
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

    [RelayCommand]
    private void OpenHotkeySettings() => new HotkeySettingsWindow().Show();

    [RelayCommand]
    private void OpenWindowEffectsSettings() => new WindowEffectsSettingsWindow(_manager).Show();

    [RelayCommand]
    private void ApplyProfile()
    {
        if (!string.IsNullOrWhiteSpace(SelectedProfile))
        {
            WorkspaceProfiles.Apply(SelectedProfile, _manager);
        }
    }

    [RelayCommand]
    private void EditProfiles() => new ProfileEditorWindow().Show();

    [RelayCommand]
    private void NewProfile()
    {
        var name = $"Profile {DateTime.Now:HHmmss}";
        WorkspaceProfiles.AddOrUpdate(new WorkspaceProfile { Name = name });
        ProfileNames.Add(name);
        SelectedProfile = name;
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfile)) return;
        WorkspaceProfiles.Remove(SelectedProfile);
        ProfileNames.Remove(SelectedProfile);
        if (ProfileNames.Count > 0) SelectedProfile = ProfileNames[0]; else SelectedProfile = null;
    }

    [RelayCommand]
    private void AttachAllToWorkspace()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow main
                && main.DataContext is MainWindowViewModel vm)
            {
                var existingNames = vm.WorkspaceItems.Select(w => w.Plugin.Name).ToHashSet();
                foreach (var plugin in _manager.Plugins)
                {
                    if (plugin is IWorkspaceItem ws && _manager.IsEnabled(plugin) && !existingNames.Contains(plugin.Name))
                    {
                        ws.UseWorkspace = true;
                        var view = ws.BuildWorkspaceView();
                        var vmItem = new WorkspaceItemViewModel(plugin.Name, view, plugin, AppDetachHelper);
                        vm.WorkspaceItems.Add(vmItem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"AttachAllToWorkspace failed: {ex.Message}");
        }
    }

    private void AppDetachHelper(WorkspaceItemViewModel item)
    {
        // Route to App's detach behavior by replicating minimal logic
        if (item.Plugin is IWorkspaceItem workspace)
        {
            workspace.UseWorkspace = false;
            item.Plugin.Start();
        }
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow main
            && main.DataContext is MainWindowViewModel vm)
        {
            vm.WorkspaceItems.Remove(item);
        }
    }
}

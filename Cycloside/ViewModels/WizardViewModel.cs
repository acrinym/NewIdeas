using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cycloside.ViewModels;

public partial class PluginItem : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private bool isEnabled;
}

public partial class WizardViewModel : ObservableObject
{
    [ObservableProperty]
    private int currentStep;

    [ObservableProperty]
    private string selectedTheme = string.Empty;

    [ObservableProperty]
    private string profileName = "Default";

    public ObservableCollection<string> AvailableThemes { get; } = new();

    public ObservableCollection<PluginItem> Plugins { get; } = new();

    public WizardViewModel()
    {
        LoadThemes();
        LoadPlugins();
        if (AvailableThemes.Count > 0)
            SelectedTheme = SettingsManager.Settings.ActiveSkin ?? AvailableThemes[0];
    }

    private void LoadThemes()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Skins");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*.axaml"))
            AvailableThemes.Add(Path.GetFileNameWithoutExtension(file));
    }

    private void LoadPlugins()
    {
        string[] names =
        {
            "Date/Time Overlay",
            "MP3 Player",
            "Macro",
            "Text Editor",
            "Wallpaper",
            "Clipboard Manager",
            "File Watcher",
            "Process Monitor",
            "Task Scheduler",
            "Disk Usage",
            "Log Viewer",
            "Environment Editor",
            "Jezzball",
            "Widget Host",
            "Winamp Vis Host",
            "QBasic Retro IDE"
        };
        foreach (var n in names)
            Plugins.Add(new PluginItem { Name = n, IsEnabled = true });
    }
}

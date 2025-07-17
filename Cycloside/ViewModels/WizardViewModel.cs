using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cycloside; // core models and services
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Cycloside.ViewModels
{
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

        partial void OnCurrentStepChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(NextButtonText));
            OnPropertyChanged(nameof(ProgressText));
        }
        
        private const int TotalSteps = 3; 

        public string ProgressText => $"Step {CurrentStep + 1} of {TotalSteps}";
        public bool CanGoBack => CurrentStep > 0;
        public string NextButtonText => CurrentStep < TotalSteps - 1 ? "Next" : "Finish";

        [ObservableProperty]
        private string selectedTheme = string.Empty;

        [ObservableProperty]
        private string profileName = "Default";

        public ObservableCollection<string> AvailableThemes { get; } = new();
        public ObservableCollection<PluginItem> Plugins { get; } = new();
        
        public event EventHandler? RequestClose;

        public WizardViewModel()
        {
            LoadThemes();
            LoadPlugins();
            if (AvailableThemes.Any())
            {
                SelectedTheme = AvailableThemes.Contains("MintGreen") ? "MintGreen" : AvailableThemes[0];
            }
        }
        
        [RelayCommand]
        private void Back()
        {
            if (CurrentStep > 0)
            {
                CurrentStep--;
            }
        }

        [RelayCommand]
        private void Next()
        {
            if (CurrentStep < TotalSteps - 1)
            {
                CurrentStep++;
                return;
            }
            
            SettingsManager.Settings.GlobalTheme = SelectedTheme;
            foreach (var item in Plugins)
            {
                SettingsManager.Settings.PluginEnabled[item.Name] = item.IsEnabled;
            }
            
            var profile = new WorkspaceProfile
            {
                Name = ProfileName,
                Plugins = Plugins.ToDictionary(p => p.Name, p => p.IsEnabled)
            };
            WorkspaceProfiles.AddOrUpdate(profile);
            
            SettingsManager.Settings.ActiveProfile = ProfileName;
            SettingsManager.Settings.FirstRun = false;
            SettingsManager.Save();
            
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        
        private void LoadThemes()
        {
            try
            {
                // THEME FIX: The wizard now correctly looks in the Themes/Global directory.
                var dir = Path.Combine(AppContext.BaseDirectory, "Themes", "Global");
                if (!Directory.Exists(dir))
                {
                    Logger.Log($"Wizard Error: Theme directory not found at '{dir}'");
                    return;
                }
                
                foreach (var file in Directory.GetFiles(dir, "*.axaml"))
                {
                    AvailableThemes.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Wizard Error: Failed to load themes. {ex.Message}");
            }
        }

        private void LoadPlugins()
        {
            string[] names =
            {
                "Date/Time Overlay", "MP3 Player", "Macro Engine", "Text Editor", "File Explorer", "Wallpaper Changer",
                "Clipboard Manager", "File Watcher", "Process Monitor", "Task Scheduler",
                "Disk Usage", "Log Viewer", "Environment Editor", "Jezzball",
                "Widget Host", "Winamp Vis Host", "QBasic Retro IDE", "ScreenSaver Host"
            };

            foreach (var n in names)
            {
                Plugins.Add(new PluginItem { Name = n, IsEnabled = true });
            }
        }
    }
}

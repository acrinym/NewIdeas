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
        // --- Properties for UI Binding ---

        [ObservableProperty]
        private int currentStep;

        partial void OnCurrentStepChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(NextButtonText));
            OnPropertyChanged(nameof(ProgressText));
        }
        
        private const int TotalSteps = 3; // Number of TabItems in WizardWindow

        public string ProgressText => $"Step {CurrentStep + 1} of {TotalSteps}";
        public bool CanGoBack => CurrentStep > 0;
        public string NextButtonText => CurrentStep < TotalSteps - 1 ? "Next" : "Finish";

        [ObservableProperty]
        private string selectedTheme = string.Empty;

        [ObservableProperty]
        private string profileName = "Default";

        public ObservableCollection<string> AvailableThemes { get; } = new();
        public ObservableCollection<PluginItem> Plugins { get; } = new();
        
        // --- NEW: Event to communicate with the View ---
        
        /// <summary>
        /// The View (WizardWindow) will listen for this event to know when to close.
        /// </summary>
        public event EventHandler? RequestClose;

        // --- Constructor ---

        public WizardViewModel()
        {
            LoadThemes();
            LoadPlugins();
            if (AvailableThemes.Any())
            {
                // Set the default theme to "Mint" if it exists, otherwise the first in the list.
                SelectedTheme = AvailableThemes.Contains("Mint") ? "Mint" : AvailableThemes[0];
            }
        }
        
        // --- NEW: Commands for the Back/Next Buttons ---

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
            // If we are not on the last step, just advance to the next tab.
            if (CurrentStep < TotalSteps - 1)
            {
                CurrentStep++;
                return;
            }

            // --- This is the logic from your original Next_Click on the final step ---
            
            // 1. Save all the settings gathered from the wizard
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
            
            // 2. Request the window to close by firing the event
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        
        // --- Private Helper Methods ---

        private void LoadThemes()
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "Skins");
                if (!Directory.Exists(dir)) return;
                
                foreach (var file in Directory.GetFiles(dir, "*.axaml"))
                {
                    AvailableThemes.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception)
            {
                // Could fail due to permissions, etc. Silently ignore.
            }
        }

        private void LoadPlugins()
        {
            string[] names =
            {
                "Date/Time Overlay", "MP3 Player", "Macro", "Text Editor", "Wallpaper", 
                "Clipboard Manager", "File Watcher", "Process Monitor", "Task Scheduler", 
                "Disk Usage", "Log Viewer", "Environment Editor", "Jezzball", 
                "Widget Host", "Winamp Vis Host", "QBasic Retro IDE"
            };

            foreach (var n in names)
            {
                // By default, enable all plugins for the first run.
                Plugins.Add(new PluginItem { Name = n, IsEnabled = true });
            }
        }
    }
}

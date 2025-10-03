using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside.Views
{
    /// <summary>
    /// Welcome to Cycloside - Ultimate Configuration Experience
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            InitializeConfiguration();
            SetupEventHandlers();
        }

        private void InitializeConfiguration()
        {
            LoadPluginsConfiguration();
        }

        private void LoadPluginsConfiguration()
        {
            var availablePlugins = new[]
            {
                new PluginConfigViewModel { Name = "HackerTerminalPlugin", Description = "Professional CMD terminal with hacker tools" },
                new PluginConfigViewModel { Name = "PowerShellTerminalPlugin", Description = "Advanced PowerShell integration with elevation" },
                new PluginConfigViewModel { Name = "HackersParadisePlugin", Description = "Dashboard for hacker paradise tools and features" },
                new PluginConfigViewModel { Name = "TextEditorPlugin", Description = "Multi-language code editor" },
                new PluginConfigViewModel { Name = "NotificationCenterPlugin", Description = "System notifications and alerts" },
                new PluginConfigViewModel { Name = "WallpaperPlugin", Description = "Dynamic wallpaper management" },
                new PluginConfigViewModel { Name = "ClipboardManagerPlugin", Description = "Enhanced clipboard management" },
                new PluginConfigViewModel { Name = "FileWatcherPlugin", Description = "File system monitoring" },
                new PluginConfigViewModel { Name = "MacroPlugin", Description = "Automation and macro management" },
                new PluginConfigViewModel { Name = "TaskSchedulerPlugin", Description = "Advanced task scheduling" }
            };

            foreach (var plugin in availablePlugins)
            {
                var config = ConfigurationManager.GetPluginConfig(plugin.Name);
                plugin.Enabled = config.Enabled;
                plugin.LoadOnStartup = config.LoadOnStartup;
            }

            if (PluginListControl != null)
                PluginListControl.ItemsSource = availablePlugins;
        }

        private void SetupEventHandlers()
        {
            if (ApplyButton != null)
                ApplyButton.Click += OnApplyClick;
            
            if (ResetButton != null)
                ResetButton.Click += OnResetClick;
        }

        private async void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (ApplyButton != null)
                {
                    ApplyButton.IsEnabled = false;
                    ApplyButton.Content = "🔄 Applying...";
                }

                await ApplyPluginConfigurations();
                
                var dontShowAgain = DontShowAgainCheckBox?.IsChecked ?? false;
                await ConfigurationManager.SetWelcomePreferenceAsync(!dontShowAgain);

                if (ApplyButton != null)
                    ApplyButton.Content = "✅ Applied!";
                
                await Task.Delay(1000);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                if (ApplyButton != null)
                {
                    ApplyButton.Content = "❌ Error";
                    ApplyButton.IsEnabled = true;
                }
                
                Console.WriteLine($"Configuration error: {ex.Message}");
            }
        }

        private async Task ApplyPluginConfigurations()
        {
            var plugins = PluginListControl?.ItemsSource?.Cast<PluginConfigViewModel>();

            if (plugins != null)
            {
                foreach (var plugin in plugins)
                {
                    await ConfigurationManager.SetPluginEnabledAsync(plugin.Name, plugin.Enabled);
                    await ConfigurationManager.SetPluginStartupPreferenceAsync(plugin.Name, plugin.LoadOnStartup);
                }
            }
        }

        private async void OnResetClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                await ConfigurationManager.ResetToDefaultsAsync();
                LoadPluginsConfiguration();
                
                if (ResetButton != null)
                    ResetButton.Content = "✅ Reset!";
            }
            catch (Exception ex)
            {
                if (ResetButton != null)
                    ResetButton.Content = "❌ Error";
                
                Console.WriteLine($"Reset error: {ex.Message}");
            }
        }

        public bool? DialogResult { get; set; }
    }

    /// <summary>
    /// ViewModel for plugin configuration
    /// </summary>
    public class PluginConfigViewModel
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool LoadOnStartup { get; set; } = false;
    }
}
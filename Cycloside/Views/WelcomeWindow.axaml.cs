using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside.Views
{
    /// <summary>
    /// Welcome to Cycloside - Ultimate Configuration Experience
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        private readonly ObservableCollection<PluginInfo> _pluginItems = new();

        public WelcomeWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (NullReferenceException nre)
            {
                Cycloside.Services.Logger.Error($"💥 WelcomeWindow XAML load NullReference handled: {nre.Message}");
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"💥 WelcomeWindow XAML load error: {ex.Message}");
            }

            InitializeConfiguration();
            SetupEventHandlers();
        }

        private void InitializeConfiguration()
        {
            LoadPluginsConfiguration();
        }

        private void LoadPluginsConfiguration()
        {
            // Ensure tri-state controls start from a known state
            try
            {
                if (PowerShellRadio != null) PowerShellRadio.IsChecked = false;
                if (HackerTerminalRadio != null) HackerTerminalRadio.IsChecked = false;
                if (DontShowAgainCheckBox != null)
                    DontShowAgainCheckBox.IsChecked = !ConfigurationManager.IsWelcomeEnabled;
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Welcome control pre-init failed: {ex.Message}");
            }

            var availablePlugins = new[]
            {
                new PluginInfo { Name = "HackerTerminalPlugin", Description = "Professional CMD terminal with hacker tools", IsEnabled = true, LoadOnStartup = false },
                new PluginInfo { Name = "PowerShellTerminalPlugin", Description = "Advanced PowerShell integration with elevation", IsEnabled = true, LoadOnStartup = false },
                new PluginInfo { Name = "HackersParadisePlugin", Description = "Dashboard for hacker paradise tools and features", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "TextEditorPlugin", Description = "Multi-language code editor", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "NotificationCenterPlugin", Description = "System notifications and alerts", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "WallpaperPlugin", Description = "Dynamic wallpaper management", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "ClipboardManagerPlugin", Description = "Enhanced clipboard management", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "FileWatcherPlugin", Description = "File system monitoring", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "MacroPlugin", Description = "Automation and macro management", IsEnabled = false, LoadOnStartup = false },
                new PluginInfo { Name = "TaskSchedulerPlugin", Description = "Advanced task scheduling", IsEnabled = false, LoadOnStartup = false }
            };

            foreach (var plugin in availablePlugins)
            {
                var config = ConfigurationManager.GetPluginConfig(plugin.Name);
                plugin.IsEnabled = config.Enabled;
                plugin.LoadOnStartup = config.LoadOnStartup;
            }

            // Clear and repopulate list if control is present
            if (PluginListControl != null)
            {
                PluginListControl.Items.Clear();
                foreach (var plugin in availablePlugins)
                {
                    var item = CreatePluginItem(plugin);
                    PluginListControl.Items.Add(item);
                }
            }
            else
            {
                Logger.Log("⚠️ PluginListControl not found in WelcomeWindow layout");
            }

            // Set terminal preference
            var preferredTerminal = ConfigurationManager.CurrentConfig?.PreferredTerminal ?? "PowerShell";
            if (string.Equals(preferredTerminal, "PowerShell", StringComparison.OrdinalIgnoreCase))
            {
                if (PowerShellRadio != null) PowerShellRadio.IsChecked = true;
            }
            else
            {
                if (HackerTerminalRadio != null) HackerTerminalRadio.IsChecked = true;
            }
        }

        private Control CreatePluginItem(PluginInfo plugin)
        {
            var container = new Border
            {
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(2)
            };

            var mainPanel = new StackPanel();

            // Plugin name and description
            var nameText = new TextBlock
            {
                Text = plugin.Name,
                FontWeight = FontWeight.SemiBold,
                FontSize = 14
            };

            var descText = new TextBlock
            {
                Text = plugin.Description,
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            };

            mainPanel.Children.Add(nameText);
            mainPanel.Children.Add(descText);

            // Controls panel
            var controlsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var enabledCheck = new CheckBox
            {
                Content = "Enabled",
                Margin = new Thickness(5),
                IsChecked = plugin.IsEnabled,
                Tag = plugin
            };
            enabledCheck.Click += OnPluginEnabledChanged;

            var startupCheck = new CheckBox
            {
                Content = "Load on Startup",
                Margin = new Thickness(5),
                IsChecked = plugin.LoadOnStartup,
                Tag = plugin
            };
            startupCheck.Click += OnPluginStartupChanged;

            controlsPanel.Children.Add(enabledCheck);
            controlsPanel.Children.Add(startupCheck);

            mainPanel.Children.Add(controlsPanel);

            container.Child = mainPanel;
            return container;
        }

        private async void OnPluginEnabledChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is PluginInfo plugin)
            {
                plugin.IsEnabled = checkBox.IsChecked ?? false;
                await ConfigurationManager.SetPluginEnabledAsync(plugin.Name, plugin.IsEnabled);
            }
        }

        private async void OnPluginStartupChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is PluginInfo plugin)
            {
                plugin.LoadOnStartup = checkBox.IsChecked ?? false;
                await ConfigurationManager.SetPluginStartupPreferenceAsync(plugin.Name, plugin.LoadOnStartup);
            }
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

                // Apply terminal preference
                var preferredTerminal = PowerShellRadio.IsChecked ?? false ? "PowerShell" : "HackerTerminal";
                await ConfigurationManager.SetTerminalPreferenceAsync(preferredTerminal);

                // Apply welcome preference
                var dontShowAgain = DontShowAgainCheckBox?.IsChecked ?? false;
                await ConfigurationManager.SetWelcomePreferenceAsync(!dontShowAgain);

                if (ApplyButton != null)
                    ApplyButton.Content = "✅ Applied!";

                await Task.Delay(500);

                // Set DialogResult to indicate successful completion
                DialogResult = true;

                // Close the window - the App.axaml.cs will handle the transition
                Close();
            }
            catch (Exception ex)
            {
                if (ApplyButton != null)
                {
                    ApplyButton.Content = "❌ Error";
                    ApplyButton.IsEnabled = true;
                }

                Logger.Log($"Configuration error: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void OnResetClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                await ConfigurationManager.ResetToDefaultsAsync();
                LoadPluginsConfiguration();

                if (ResetButton != null)
                {
                    ResetButton.Content = "✅ Reset!";
                    await Task.Delay(1000);
                    ResetButton.Content = "🔄 Reset to Defaults";
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Reset error: {ex.Message}");
            }
        }

        public bool? DialogResult { get; set; }
    }

    /// <summary>
    /// Plugin information model
    /// </summary>
    public class PluginInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public bool LoadOnStartup { get; set; } = false;
    }
}
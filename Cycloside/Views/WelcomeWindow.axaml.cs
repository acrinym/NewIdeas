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
        // Plugin configuration will be stored in Controls directly

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

            // Create UI items for the ListBox
            PluginListControl.Items.Clear();
            foreach (var plugin in availablePlugins)
            {
                var item = CreatePluginItem(plugin);
                PluginListControl.Items.Add(item);
            }

            // Set terminal preference
            var preferredTerminal = ConfigurationManager.CurrentConfig.PreferredTerminal;
            if (preferredTerminal.Equals("PowerShell", StringComparison.OrdinalIgnoreCase))
            {
                PowerShellRadio.IsChecked = true;
            } else
            {
                HackerTerminalRadio.IsChecked = true;
            }
        }

        private Control CreatePluginItem(PluginInfo plugin)
        {
            var container = new Border
            {
                Background = Avalonia.Media.Brushes.LightGray,
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(10),
                Margin = new Avalonia.Thickness(2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Avalonia.GridLength(1, Avalonia.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Avalonia.GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Avalonia.GridLength(80) });

            // Plugin info
            var infoPanel = new StackPanel
            {
                GridColumn = 0
            };

            var nameText = new TextBlock
            {
                Text = plugin.Name,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                FontSize = 12
            };

            var descText = new TextBlock
            {
                Text = plugin.Description,
                FontSize = 10,
                Opacity = 0.7
            };

            infoPanel.Children.Add(nameText);
            infoPanel.Children.Add(descText);

            // Enabled checkbox
            var enabledCheck = new CheckBox
            {
                Content = "Enabled",
                GridColumn = 1,
                Margin = new Avalonia.Thickness(5),
                IsChecked = plugin.IsEnabled
            };
            enabledCheck.Tag = plugin; // Store plugin reference
            enabledCheck.Checked += OnPluginEnabledChanged;

            // Startup checkbox
            var startupCheck = new CheckBox
            {
                Content = "Startup",
                GridColumn = 2,
                Margin = new Avalonia.Thickness(5),
                IsChecked = plugin.LoadOnStartup
            };
            startupCheck.Tag = plugin; // Store plugin reference
            startupCheck.Checked += OnPluginStartupChanged;

            grid.Children.Add(infoPanel);
            grid.Children.Add(enabledCheck);
            grid.Children.Add(startupCheck);

            container.Child = grid;
            return container;
        }

        private async void OnPluginEnabledChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is PluginInfo plugin)
            {
                plugin.IsEnabled = checkBox.Check?.Equals(true) ?? false;
                await ConfigurationManager.SetPluginEnabledAsync(plugin.Name, plugin.IsEnabled);
            }
        }

        private async void OnPluginStartupChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is PluginInfo plugin)
            {
                plugin.LoadOnStartup = checkBox.Check?.Equals(true) ?? false;
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
                
                Logger.Log($"Configuration error: {ex.Message}");
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
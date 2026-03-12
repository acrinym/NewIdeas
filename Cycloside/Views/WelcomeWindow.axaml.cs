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
    /// First-run setup for Cycloside's personal desktop shell.
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
                new PluginInfo { Name = "WidgetHostPlugin", DisplayName = "Widget Host", Description = "Dockable desktop gadgets and mini tools" },
                new PluginInfo { Name = "NetworkToolsPlugin", DisplayName = "Netwatch", Description = "Keep an eye on network activity from a desktop utility view" },
                new PluginInfo { Name = "WallpaperPlugin", DisplayName = "Wallpaper Studio", Description = "Dynamic wallpaper, backdrop, and desktop mood control" },
                new PluginInfo { Name = "ManagedVisHostPlugin", DisplayName = "Visual Playground", Description = "Managed audio visualizers, starfields, matrix rain, and more" },
                new PluginInfo { Name = "MP3PlayerPlugin", DisplayName = "MP3 Player", Description = "Music playback with widget and visualization hooks" },
                new PluginInfo { Name = "JezzballPlugin", DisplayName = "Jezzball", Description = "Retro arcade energy built into the desktop" },
                new PluginInfo { Name = "GweledPlugin", DisplayName = "Gweled", Description = "Small jewel-swap puzzle sessions for the retro shell" },
                new PluginInfo { Name = "TileWorldPlugin", DisplayName = "Tile World", Description = "Chip's Challenge style puzzle boards for the retro shell" },
                new PluginInfo { Name = "QBasicRetroIDEPlugin", DisplayName = "QBasic Retro IDE", Description = "QB-style coding corner for old-school experiments" },
                new PluginInfo { Name = "PowerShellTerminalPlugin", DisplayName = "PowerShell Terminal", Description = "Automation shell and system tooling" },
                new PluginInfo { Name = "HackerTerminalPlugin", DisplayName = "Classic Command Terminal", Description = "Quick old-school command shell workflow" },
                new PluginInfo { Name = "TextEditorPlugin", DisplayName = "Text Editor", Description = "Scratchpad for notes, scripts, and snippets" },
                new PluginInfo { Name = "ClipboardManagerPlugin", DisplayName = "Clipboard Manager", Description = "Clipboard history and quick snippet reuse" }
            };

            foreach (var plugin in availablePlugins)
            {
                var metadata = PluginMetadataResolver.ResolveById(plugin.Name);
                plugin.Category = metadata.Category;
                plugin.IsEnabled = metadata.EnabledByDefault;
                plugin.LoadOnStartup = metadata.EnabledByDefault;

                var config = ConfigurationManager.GetPluginConfig(plugin.Name, metadata.EnabledByDefault, plugin.DisplayName);
                plugin.IsEnabled = config.Enabled;
                plugin.LoadOnStartup = config.LoadOnStartup;
            }

            // Clear and repopulate list if control is present
            if (PluginListControl != null)
            {
                PluginListControl.Items.Clear();
                PluginCategory? currentCategory = null;

                foreach (var plugin in availablePlugins
                    .OrderBy(item => PluginMetadataResolver.GetCategorySortOrder(item.Category))
                    .ThenBy(item => item.DisplayName, StringComparer.Ordinal))
                {
                    if (currentCategory != plugin.Category)
                    {
                        PluginListControl.Items.Add(CreateCategoryHeader(plugin.Category));
                        currentCategory = plugin.Category;
                    }

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

            // Plugin name and category
            var headerPanel = new DockPanel();

            var nameText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(plugin.DisplayName) ? plugin.Name : plugin.DisplayName,
                FontWeight = FontWeight.SemiBold,
                FontSize = 14
            };

            var categoryText = new TextBlock
            {
                Text = PluginMetadataResolver.GetCategoryDisplayName(plugin.Category),
                FontSize = 11,
                Foreground = Brushes.DarkSlateBlue,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            DockPanel.SetDock(categoryText, Dock.Right);
            headerPanel.Children.Add(categoryText);
            headerPanel.Children.Add(nameText);

            var descText = new TextBlock
            {
                Text = plugin.Description,
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            };

            mainPanel.Children.Add(headerPanel);
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

        private Control CreateCategoryHeader(PluginCategory category)
        {
            return new Border
            {
                Background = Brushes.Gainsboro,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 8, 0, 4),
                Child = new TextBlock
                {
                    Text = PluginMetadataResolver.GetCategoryDisplayName(category),
                    FontWeight = FontWeight.Bold,
                    FontSize = 13
                }
            };
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
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public PluginCategory Category { get; set; } = PluginCategory.Experimental;
        public bool IsEnabled { get; set; } = true;
        public bool LoadOnStartup { get; set; } = false;
    }
}

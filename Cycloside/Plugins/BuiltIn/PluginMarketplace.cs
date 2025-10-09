using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Views;
using Cycloside.Widgets;
using Cycloside.Plugins.BuiltIn.Views;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// PLUGIN MARKETPLACE - Browse and install community plugins
    /// Provides access to the Cycloside plugin ecosystem
    /// </summary>
    public class PluginMarketplacePlugin : IPlugin
    {
        public string Name => "Plugin Marketplace";
        public string Description => "Browse, search, and install community plugins";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;

        public class PluginMarketplaceWidget : IWidget
        {
            public string Name => "Plugin Marketplace";

            private PluginMarketplaceWindow? _marketplaceWindow;

            public Control BuildView()
            {
                var mainPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10)
                };

                // Header
                var headerText = new TextBlock
                {
                    Text = "🔌 Plugin Marketplace",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var descriptionText = new TextBlock
                {
                    Text = "Discover and install plugins from the Cycloside community",
                    FontSize = 12,
                    Opacity = 0.8,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // Action buttons
                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10
                };

                var browseButton = new Button
                {
                    Content = "🛒 Browse Marketplace",
                    Background = Avalonia.Media.Brushes.DodgerBlue,
                    Foreground = Avalonia.Media.Brushes.White,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Padding = new Thickness(15, 8)
                };
                browseButton.Click += OnBrowseMarketplace;

                var manageButton = new Button
                {
                    Content = "⚙️ Manage Plugins",
                    Background = Avalonia.Media.Brushes.DarkOrange,
                    Foreground = Avalonia.Media.Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                manageButton.Click += OnManagePlugins;

                var refreshButton = new Button
                {
                    Content = "🔄 Refresh",
                    Background = Avalonia.Media.Brushes.DarkGreen,
                    Foreground = Avalonia.Media.Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                refreshButton.Click += OnRefreshRepository;

                buttonsPanel.Children.Add(browseButton);
                buttonsPanel.Children.Add(manageButton);
                buttonsPanel.Children.Add(refreshButton);

                // Status area
                var statusPanel = new Border
                {
                    Background = Avalonia.Media.Brushes.LightYellow,
                    BorderBrush = Avalonia.Media.Brushes.Goldenrod,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 15, 0, 0),
                    Padding = new Thickness(10)
                };

                var statusText = new TextBlock
                {
                    Text = "Plugin Marketplace Ready",
                    FontSize = 12
                };

                statusPanel.Child = statusText;

                mainPanel.Children.Add(headerText);
                mainPanel.Children.Add(descriptionText);
                mainPanel.Children.Add(buttonsPanel);
                mainPanel.Children.Add(statusPanel);

                var border = new Border
                {
                    Child = mainPanel,
                    Background = Avalonia.Media.Brushes.White,
                    BorderBrush = Avalonia.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10)
                };

                return border;
            }

            private void OnBrowseMarketplace(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                try
                {
                    if (_marketplaceWindow == null)
                    {
                        _marketplaceWindow = new PluginMarketplaceWindow();
                        _marketplaceWindow.Closed += (_, _) => _marketplaceWindow = null;
                    }

                    _marketplaceWindow.Show();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error opening marketplace: {ex.Message}");
                }
            }

            private void OnManagePlugins(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                try
                {
                    var installedPlugins = PluginRepository.GetInstalledPlugins();

                var listBox = new ListBox();
                foreach (var plugin in installedPlugins)
                {
                    var item = new ListBoxItem
                    {
                        Content = $"{plugin.Name} v{plugin.Version} - {plugin.Author}"
                    };
                    listBox.Items.Add(item);
                }

                var manageWindow = new Window
                {
                    Title = "⚙️ Manage Installed Plugins",
                    Width = 600,
                    Height = 400,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"📦 Installed Plugins ({installedPlugins.Count})",
                                FontSize = 16,
                                FontWeight = FontWeight.Bold,
                                Margin = new Thickness(0, 0, 0, 15)
                            },

                            new ScrollViewer
                            {
                                Content = listBox
                            }
                        },
                        Margin = new Thickness(20)
                    }
                };

                    manageWindow.Show();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error managing plugins: {ex.Message}");
                }
            }

            private async void OnRefreshRepository(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                try
                {
                    Logger.Log("🔄 Refreshing plugin repository...");

                    var button = sender as Button;
                    if (button != null)
                    {
                        button.IsEnabled = false;
                        button.Content = "⏳ Refreshing...";
                    }

                    // Reinitialize repository (this would refresh the cache)
                    await PluginRepository.InitializeAsync();

                    if (button != null)
                    {
                        button.Content = "✅ Refreshed";
                        await Task.Delay(1000);
                        button.Content = "🔄 Refresh";
                        button.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error refreshing repository: {ex.Message}");

                    var button = sender as Button;
                    if (button != null)
                    {
                        button.Content = "❌ Error";
                        await Task.Delay(1000);
                        button.Content = "🔄 Refresh";
                        button.IsEnabled = true;
                    }
                }
            }
        }

        public IWidget? Widget => new PluginMarketplaceWidget();

        public void Start()
        {
            Logger.Log("🚀 Plugin Marketplace Plugin started - Ready to browse community plugins!");

            // Initialize the repository system
            _ = PluginRepository.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🛑 Plugin Marketplace Plugin stopped");
        }
    }
}

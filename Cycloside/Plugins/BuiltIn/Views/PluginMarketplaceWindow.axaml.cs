using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn.Views
{
    /// <summary>
    /// Plugin Marketplace - Browse, search, and install community plugins
    /// </summary>
    public partial class PluginMarketplaceWindow : Window
    {
        private List<PluginManifest> _availablePlugins = new();
        private List<PluginManifest> _installedPlugins = new();
        private PluginManifest? _selectedPlugin;

        public PluginMarketplaceWindow()
        {
            InitializeComponent();
            _ = InitializeMarketplaceAsync();
            SetupEventHandlers();
        }

        private void InitializeMarketplace()
        {
            // Load installed plugins
            _installedPlugins = PluginRepository.GetInstalledPlugins();

            // Set initial status
            UpdateStatus("Loading plugins from repository...");
        }

        private async Task InitializeMarketplaceAsync()
        {
            try
            {
                // Load available plugins from repository
                _availablePlugins = await PluginRepository.DiscoverPluginsAsync();

                // Update installed status for each plugin
                foreach (var plugin in _availablePlugins)
                {
                    plugin.IsInstalled = _installedPlugins.Any(p => p.Name == plugin.Name);
                }

                // Populate the plugin list
                PopulatePluginList();

                UpdateStatus($"Found {_availablePlugins.Count} plugins available");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading plugins: {ex.Message}");
                Logger.Log($"Plugin marketplace error: {ex}");
            }
        }

        private void PopulatePluginList()
        {
            PluginListBox.Items.Clear();

            foreach (var plugin in _availablePlugins)
            {
                var item = CreatePluginListItem(plugin);
                PluginListBox.Items.Add(item);
            }
        }

        private Control CreatePluginListItem(PluginManifest plugin)
        {
            var container = new Border
            {
                Background = plugin.IsInstalled ? Brushes.LightGreen : Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(2),
                Padding = new Thickness(10)
            };

            // Plugin info
            var infoPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = plugin.Name,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold
            };

            var descText = new TextBlock
            {
                Text = plugin.Description,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8,
                Margin = new Thickness(0, 3, 0, 0)
            };

            // Rating and downloads
            var statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var ratingText = new TextBlock
            {
                Text = $"⭐ {plugin.Rating:F1}",
                FontSize = 11,
                Foreground = Brushes.Goldenrod
            };

            var downloadsText = new TextBlock
            {
                Text = $"{plugin.Downloads:N0} downloads",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(10, 0, 0, 0)
            };

            statsPanel.Children.Add(ratingText);
            statsPanel.Children.Add(downloadsText);

            infoPanel.Children.Add(nameText);
            infoPanel.Children.Add(descText);
            infoPanel.Children.Add(statsPanel);

            // Install button
            var installButton = new Button
            {
                Content = plugin.IsInstalled ? "✅ Installed" : "📥 Install",
                Background = plugin.IsInstalled ? Brushes.Green : Brushes.DodgerBlue,
                Foreground = Brushes.White,
                FontSize = 11,
                Padding = new Thickness(6, 3),
                Margin = new Thickness(5, 0, 0, 0),
                Tag = plugin
            };

            if (!plugin.IsInstalled)
            {
                installButton.Click += OnInstallPluginClick;
            }

            // Layout the controls horizontally
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { infoPanel, installButton }
            };

            container.Child = itemPanel;
            return container;
        }

        private async void OnInstallPluginClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PluginManifest plugin)
            {
                button.IsEnabled = false;
                button.Content = "⏳ Installing...";

                var success = await PluginRepository.InstallPluginAsync(plugin);

                if (success)
                {
                    button.Content = "✅ Installed";
                    button.Background = Brushes.Green;
                    plugin.IsInstalled = true;

                    // Refresh installed plugins list
                    _installedPlugins = PluginRepository.GetInstalledPlugins();
                }
                else
                {
                    button.Content = "❌ Failed";
                    button.Background = Brushes.Red;
                    button.IsEnabled = true;

                    await Task.Delay(2000);
                    button.Content = "📥 Install";
                    button.Background = Brushes.DodgerBlue;
                    button.IsEnabled = true;
                }
            }
        }

        private void SetupEventHandlers()
        {
            SearchBox.TextChanged += OnSearchTextChanged;
            PluginListBox.SelectionChanged += OnPluginSelectionChanged;

            if (RefreshButton != null)
                RefreshButton.Click += OnRefreshClick;

            // Subscribe to repository events
            PluginRepository.PluginDiscovered += OnPluginDiscovered;
            PluginRepository.PluginInstalled += OnPluginInstalled;
            PluginRepository.PluginInstallFailed += OnPluginInstallFailed;
        }

        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            var searchTerm = SearchBox.Text?.ToLowerInvariant() ?? "";

            if (string.IsNullOrEmpty(searchTerm))
            {
                PopulatePluginList();
            }
            else
            {
                var filteredPlugins = _availablePlugins.Where(p =>
                    p.Name.ToLowerInvariant().Contains(searchTerm) ||
                    p.Description.ToLowerInvariant().Contains(searchTerm) ||
                    (p.Tags?.Any(t => t.ToLowerInvariant().Contains(searchTerm)) ?? false)
                ).ToList();

                PluginListBox.Items.Clear();
                foreach (var plugin in filteredPlugins)
                {
                    var item = CreatePluginListItem(plugin);
                    PluginListBox.Items.Add(item);
                }
            }
        }

        private void OnPluginSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (PluginListBox.SelectedItem is Control selectedItem)
            {
                // Find the plugin associated with this item
                _selectedPlugin = FindPluginFromControl(selectedItem);

                if (_selectedPlugin != null)
                {
                    ShowPluginDetails(_selectedPlugin);
                }
            }
        }

        private PluginManifest? FindPluginFromControl(Control control)
        {
            // This is a simplified approach - in production would use proper data binding
            var button = control.FindControl<Button>("InstallButton");
            return button?.Tag as PluginManifest;
        }

        private void ShowPluginDetails(PluginManifest plugin)
        {
            if (PluginDetailsPanel != null)
            {
                PluginDetailsPanel.IsVisible = true;

                // Update all the detail fields
                if (PluginTitle != null) PluginTitle.Text = plugin.Name;
                if (PluginDescription != null) PluginDescription.Text = plugin.Description;
                if (PluginAuthor != null) PluginAuthor.Text = plugin.Author;
                if (PluginVersion != null) PluginVersion.Text = plugin.Version;
                if (PluginRating != null) PluginRating.Text = plugin.Rating.ToString("F1");
                if (PluginDownloads != null) PluginDownloads.Text = $"{plugin.Downloads:N0}";

                // Update tags
                if (PluginTags != null)
                {
                    PluginTags.Children.Clear();
                    if (plugin.Tags != null)
                    {
                        foreach (var tag in plugin.Tags)
                        {
                            var tagBorder = new Border
                            {
                                Background = Brushes.LightBlue,
                                CornerRadius = new CornerRadius(3),
                                Padding = new Thickness(5, 2),
                                Margin = new Thickness(2)
                            };

                            var tagText = new TextBlock
                            {
                                Text = tag,
                                FontSize = 10,
                                Foreground = Brushes.DarkBlue
                            };

                            tagBorder.Child = tagText;
                            PluginTags.Children.Add(tagBorder);
                        }
                    }
                }

                // Update screenshots
                if (PluginScreenshotsPanel != null)
                {
                    ScreenshotsContainer.Children.Clear();
                    if (plugin.Screenshots != null && plugin.Screenshots.Any())
                    {
                        PluginScreenshotsPanel.IsVisible = true;
                        foreach (var screenshot in plugin.Screenshots)
                        {
                            var image = new Image
                            {
                                Width = 200,
                                Height = 150,
                                Margin = new Thickness(5),
                                Stretch = Stretch.UniformToFill
                            };

                            // In production, would load actual images
                            image.Source = CreatePlaceholderImage();

                            ScreenshotsContainer.Children.Add(image);
                        }
                    }
                    else
                    {
                        PluginScreenshotsPanel.IsVisible = false;
                    }
                }
            }
        }

        private IImage CreatePlaceholderImage()
        {
            // Create a simple colored rectangle as placeholder
            var drawingGroup = new DrawingGroup();
            using (var context = drawingGroup.Open())
            {
                var rect = new Rect(0, 0, 200, 150);
                context.DrawRectangle(Brushes.LightGray, null, rect);

                // Simple text drawing - simplified for Avalonia compatibility
                var textBlock = new TextBlock
                {
                    Text = "Screenshot",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground = Brushes.Gray
                };

                // For now, just return a simple colored image
                context.DrawRectangle(Brushes.LightGray, null, rect);
            }

            return new DrawingImage(drawingGroup);
        }

        private async void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            await InitializeMarketplaceAsync();
        }

        private void UpdateStatus(string message)
        {
            if (StatusText != null)
                StatusText.Text = message;

            Logger.Log($"Plugin Marketplace: {message}");
        }

        // Event handlers for repository events
        private void OnPluginDiscovered(object? sender, PluginDiscoveryEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var item = CreatePluginListItem(e.Plugin);
                PluginListBox.Items.Add(item);
            });
        }

        private void OnPluginInstalled(object? sender, PluginInstallEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateStatus($"✅ Plugin installed: {e.Plugin.Name}");

                if (e.Plugin == _selectedPlugin)
                {
                    ShowPluginDetails(e.Plugin);
                }
            });
        }

        private void OnPluginInstallFailed(object? sender, PluginInstallEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateStatus($"❌ Plugin installation failed: {e.Plugin.Name} - {e.ErrorMessage}");
            });
        }
    }
}

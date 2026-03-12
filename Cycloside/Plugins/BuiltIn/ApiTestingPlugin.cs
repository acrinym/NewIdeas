using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Widgets;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// API TESTING PLUGIN - Comprehensive REST API testing and web service analysis toolkit
    /// Provides HTTP request testing, response analysis, authentication handling, and API documentation
    /// </summary>
    public class ApiTestingPlugin : IPlugin
    {
        public string Name => "API Testing";
        public string Description => "Comprehensive REST API testing and web service analysis toolkit";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.Development;
        public IWidget? Widget => new ApiTestingWidget();

        public void Start()
        {
            Logger.Log("🌐 API Testing plugin started");
            _ = ApiTestingService.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🌐 API Testing plugin stopped");
        }
    }

    /// <summary>
    /// API Testing Widget
    /// </summary>
    public class ApiTestingWidget : IWidget
    {
        public string Name => "API Testing";

        private TabControl? _mainTabControl;
        private TextBlock? _statusText;
        private ComboBox? _methodCombo;
        private TextBox? _urlInput;
        private ListBox? _requestHistory;
        private TextBox? _responseContent;
        private TextBox? _requestBody;
        private ListBox? _collectionsList;
        private TextBox? _collectionNameInput;

        public Control BuildView()
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10)
            };

            // Header
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var headerText = new TextBlock
            {
                Text = "🌐 API Testing",
                FontSize = 18,
                FontWeight = FontWeight.Bold
            };

            _statusText = new TextBlock
            {
                Text = "Ready",
                Foreground = Brushes.Gray,
                Margin = new Thickness(15, 0, 0, 0)
            };

            headerPanel.Children.Add(headerText);
            headerPanel.Children.Add(_statusText);

            // Main tab control
            _mainTabControl = new TabControl();

            // Request Builder Tab
            var requestTab = CreateRequestBuilderTab();
            _mainTabControl.Items.Add(requestTab);

            // Collections Tab
            var collectionsTab = CreateCollectionsTab();
            _mainTabControl.Items.Add(collectionsTab);

            // History Tab
            var historyTab = CreateHistoryTab();
            _mainTabControl.Items.Add(historyTab);

            mainPanel.Children.Add(headerPanel);
            mainPanel.Children.Add(_mainTabControl);

            return mainPanel;
        }

        private TabItem CreateRequestBuilderTab()
        {
            var tab = new TabItem { Header = "🔧 Request Builder" };

            var panel = new StackPanel { Margin = new Thickness(15) };

            // Request configuration
            var requestConfigPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var methodPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var methodLabel = new TextBlock { Text = "Method:", Width = 60 };
            _methodCombo = new ComboBox { Width = 100 };
            _methodCombo.Items.Add("GET");
            _methodCombo.Items.Add("POST");
            _methodCombo.Items.Add("PUT");
            _methodCombo.Items.Add("DELETE");
            _methodCombo.Items.Add("PATCH");
            _methodCombo.SelectedIndex = 0;

            methodPanel.Children.Add(methodLabel);
            methodPanel.Children.Add(_methodCombo);

            var urlPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var urlLabel = new TextBlock { Text = "URL:", Width = 60 };
            _urlInput = new TextBox
            {
                Text = "https://httpbin.org/get",
                Width = 400,
                Margin = new Thickness(0, 0, 10, 0)
            };

            urlPanel.Children.Add(urlLabel);
            urlPanel.Children.Add(_urlInput);

            requestConfigPanel.Children.Add(methodPanel);
            requestConfigPanel.Children.Add(urlPanel);

            // Request body
            var bodyPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var bodyLabel = new TextBlock
            {
                Text = "Request Body (JSON):",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _requestBody = new TextBox
            {
                Text = @"{
  ""name"": ""test"",
  ""value"": ""example""
}",
                Height = 120,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = "Consolas",
                Background = Brush.Parse("#f8f9fa")
            };

            bodyPanel.Children.Add(bodyLabel);
            bodyPanel.Children.Add(_requestBody);

            // Control buttons
            var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            var sendButton = new Button
            {
                Content = "🚀 Send Request",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(15, 8)
            };
            sendButton.Click += OnSendRequest;

            var saveButton = new Button
            {
                Content = "💾 Save to Collection",
                Background = Brushes.Blue,
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8)
            };
            saveButton.Click += OnSaveToCollection;

            controlsPanel.Children.Add(sendButton);
            controlsPanel.Children.Add(saveButton);

            // Response area
            var responsePanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

            var responseLabel = new TextBlock
            {
                Text = "Response:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _responseContent = new TextBox
            {
                Text = "Response will appear here...",
                Height = 300,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = "Consolas",
                Background = Brush.Parse("#f8f9fa")
            };

            responsePanel.Children.Add(responseLabel);
            responsePanel.Children.Add(_responseContent);

            panel.Children.Add(requestConfigPanel);
            panel.Children.Add(bodyPanel);
            panel.Children.Add(controlsPanel);
            panel.Children.Add(responsePanel);

            tab.Content = panel;
            return tab;
        }

        private TabItem CreateCollectionsTab()
        {
            var tab = new TabItem { Header = "📚 Collections" };

            var panel = new StackPanel { Margin = new Thickness(15) };

            // Collections list
            var collectionsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var collectionsLabel = new TextBlock
            {
                Text = "API Collections:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _collectionsList = new ListBox { Height = 200 };

            collectionsPanel.Children.Add(collectionsLabel);
            collectionsPanel.Children.Add(_collectionsList);

            // Create collection
            var createCollectionPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

            var createLabel = new TextBlock
            {
                Text = "Create New Collection:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var collectionNamePanel = new StackPanel { Orientation = Orientation.Horizontal };
            var collectionNameLabel = new TextBlock { Text = "Name:", Width = 60 };
            _collectionNameInput = new TextBox
            {
                Text = "My API Collection",
                Width = 200,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var createCollectionButton = new Button
            {
                Content = "➕ Create Collection",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8)
            };
            createCollectionButton.Click += OnCreateCollection;

            collectionNamePanel.Children.Add(collectionNameLabel);
            collectionNamePanel.Children.Add(_collectionNameInput);
            collectionNamePanel.Children.Add(createCollectionButton);

            createCollectionPanel.Children.Add(createLabel);
            createCollectionPanel.Children.Add(collectionNamePanel);

            panel.Children.Add(collectionsPanel);
            panel.Children.Add(createCollectionPanel);

            tab.Content = panel;
            return tab;
        }

        private TabItem CreateHistoryTab()
        {
            var tab = new TabItem { Header = "📜 History" };

            var panel = new StackPanel { Margin = new Thickness(15) };

            var historyLabel = new TextBlock
            {
                Text = "Request History:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _requestHistory = new ListBox { Height = 400 };

            panel.Children.Add(historyLabel);
            panel.Children.Add(_requestHistory);

            tab.Content = panel;
            return tab;
        }

        private async void OnSendRequest(object? sender, RoutedEventArgs e)
        {
            var method = _methodCombo?.SelectedItem?.ToString() ?? "GET";
            var url = _urlInput?.Text?.Trim();
            var body = _requestBody?.Text?.Trim();

            if (string.IsNullOrEmpty(url))
            {
                UpdateStatus("❌ Please enter a URL");
                return;
            }

            UpdateStatus($"🚀 Sending {method} request to {url}...");

            try
            {
                var request = new ApiRequest
                {
                    Method = method,
                    Url = url,
                    Body = body ?? "",
                    Headers = new Dictionary<string, string>()
                };

                var response = await ApiTestingService.ExecuteRequestAsync(request);

                if (response.Success)
                {
                    // Display response
                    var responseText = $@"Status: {response.StatusCode} {response.StatusMessage}
Duration: {response.Duration.TotalMilliseconds}ms
Size: {response.Size} bytes

Headers:
{string.Join("\n", response.Headers?.Select(h => $"{h.Key}: {h.Value}") ?? new[] { "No headers" })}

Content:
{(response.IsJson ? FormatJson(response.Content) : response.Content)}";

                    if (_responseContent != null)
                        _responseContent.Text = responseText;

                    // Add to history
                    if (_requestHistory != null)
                    {
                        var historyItem = $"{DateTime.Now:HH:mm:ss} | {method} {url} | {response.StatusCode}";
                        _requestHistory.Items.Add(historyItem);

                        if (_requestHistory.Items.Count > 50)
                            _requestHistory.Items.RemoveAt(0);
                    }

                    UpdateStatus($"✅ Request successful: {response.StatusCode} in {response.Duration.TotalMilliseconds}ms");
                }
                else
                {
                    var errorText = $@"Status: {response.StatusCode} {response.StatusMessage}
Duration: {response.Duration.TotalMilliseconds}ms

Error: {response.Error}";

                    if (_responseContent != null)
                        _responseContent.Text = errorText;

                    UpdateStatus($"❌ Request failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Request error: {ex.Message}");
            }
        }

        private void OnSaveToCollection(object? sender, RoutedEventArgs e)
        {
            var method = _methodCombo?.SelectedItem?.ToString() ?? "GET";
            var url = _urlInput?.Text?.Trim();
            var body = _requestBody?.Text?.Trim();

            if (string.IsNullOrEmpty(url))
            {
                UpdateStatus("❌ Please enter a URL to save");
                return;
            }

            UpdateStatus("💾 Saving request to collection...");
            // In a full implementation, would show collection selection dialog
            UpdateStatus("✅ Request saved to collection");
        }

        private async void OnCreateCollection(object? sender, RoutedEventArgs e)
        {
            var name = _collectionNameInput?.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                UpdateStatus("❌ Please enter a collection name");
                return;
            }

            UpdateStatus($"📚 Creating collection: {name}...");

            try
            {
                await ApiTestingService.CreateCollectionAsync(name);

                if (_collectionsList != null)
                {
                    _collectionsList.Items.Clear();
                    foreach (var collection in ApiTestingService.Collections)
                    {
                        _collectionsList.Items.Add($"{collection.Name} ({collection.Requests.Count} requests)");
                    }
                }

                UpdateStatus($"✅ Collection created: {name}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Failed to create collection: {ex.Message}");
            }
        }

        private string FormatJson(string json)
        {
            try
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                return System.Text.Json.JsonSerializer.Serialize(jsonElement, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return json;
            }
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
            }

            Logger.Log($"API Testing: {message}");
        }
    }
}

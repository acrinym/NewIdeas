using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Widgets;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Database Manager Plugin - Professional database management and SQL editing toolkit
    /// Supports SQL Server, MySQL, PostgreSQL, and SQLite with query execution and result visualization
    /// </summary>
    public class DatabaseManagerPlugin : IPlugin
    {
        public string Name => "Database Manager";
        public string Description => "Professional database management and SQL editing toolkit";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.Development;
        public IWidget? Widget => new DatabaseManagerWidget();

        public void Start()
        {
            Logger.Log("🗄️ Database Manager plugin started");
            _ = DatabaseManager.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🗄️ Database Manager plugin stopped");
        }
    }

    /// <summary>
    /// Database Manager Widget
    /// </summary>
    public class DatabaseManagerWidget : IWidget
    {
        public string Name => "Database Manager";

        private TabControl? _mainTabControl;
        private TextBlock? _statusText;
        private ListBox? _connectionsList;
        private TextBox? _queryInput;
        private ListBox? _queryResults;
        private TextBox? _connectionStringInput;
        private ComboBox? _providerCombo;

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
                Text = "🗄️ Database Manager",
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

            // Connections Tab
            var connectionsTab = CreateConnectionsTab();
            _mainTabControl.Items.Add(connectionsTab);

            // Query Tab
            var queryTab = CreateQueryTab();
            _mainTabControl.Items.Add(queryTab);

            // Schema Tab
            var schemaTab = CreateSchemaTab();
            _mainTabControl.Items.Add(schemaTab);

            mainPanel.Children.Add(headerPanel);
            mainPanel.Children.Add(_mainTabControl);

            return mainPanel;
        }

        private TabItem CreateConnectionsTab()
        {
            var tab = new TabItem { Header = "🔗 Connections" };

            var panel = new StackPanel { Margin = new Thickness(15) };

            // Connection list
            var connectionsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var connectionsLabel = new TextBlock
            {
                Text = "Database Connections:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _connectionsList = new ListBox { Height = 200 };

            connectionsPanel.Children.Add(connectionsLabel);
            connectionsPanel.Children.Add(_connectionsList);

            // Add connection panel
            var addConnectionPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

            var addConnectionLabel = new TextBlock
            {
                Text = "Add New Connection:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var connectionForm = new StackPanel();

            var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var nameLabel = new TextBlock { Text = "Name:", Width = 80 };
            var nameInput = new TextBox { Text = "MyDatabase", Width = 200 };

            namePanel.Children.Add(nameLabel);
            namePanel.Children.Add(nameInput);

            var providerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var providerLabel = new TextBlock { Text = "Provider:", Width = 80 };
            _providerCombo = new ComboBox { Width = 200 };
            _providerCombo.Items.Add("SQL Server");
            _providerCombo.Items.Add("MySQL");
            _providerCombo.Items.Add("PostgreSQL");
            _providerCombo.Items.Add("SQLite");
            _providerCombo.SelectedIndex = 0;

            providerPanel.Children.Add(providerLabel);
            providerPanel.Children.Add(_providerCombo);

            var connectionStringPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var connectionStringLabel = new TextBlock { Text = "Connection String:", Width = 80 };
            _connectionStringInput = new TextBox
            {
                Text = "Server=localhost;Database=mydb;User Id=sa;Password=password;",
                Width = 400,
                Height = 60,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };

            connectionStringPanel.Children.Add(connectionStringLabel);
            connectionStringPanel.Children.Add(_connectionStringInput);

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            var testButton = new Button
            {
                Content = "🧪 Test Connection",
                Background = Brushes.Blue,
                Foreground = Brushes.White,
                Padding = new Thickness(10, 5)
            };
            testButton.Click += OnTestConnection;

            var addButton = new Button
            {
                Content = "➕ Add Connection",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                Padding = new Thickness(10, 5)
            };
            addButton.Click += OnAddConnection;

            buttonsPanel.Children.Add(testButton);
            buttonsPanel.Children.Add(addButton);

            connectionForm.Children.Add(namePanel);
            connectionForm.Children.Add(providerPanel);
            connectionForm.Children.Add(connectionStringPanel);
            connectionForm.Children.Add(buttonsPanel);

            addConnectionPanel.Children.Add(addConnectionLabel);
            addConnectionPanel.Children.Add(connectionForm);

            panel.Children.Add(connectionsPanel);
            panel.Children.Add(addConnectionPanel);

            tab.Content = panel;
            return tab;
        }

        private TabItem CreateQueryTab()
        {
            var tab = new TabItem { Header = "⚡ Query" };

            var panel = new StackPanel { Margin = new Thickness(15) };

            // Connection selector
            var connectionSelector = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var connectionSelectorLabel = new TextBlock
            {
                Text = "Select Database Connection:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var connectionSelectorCombo = new ComboBox { Width = 300 };
            connectionSelectorCombo.SelectionChanged += OnConnectionSelected;

            connectionSelector.Children.Add(connectionSelectorLabel);
            connectionSelector.Children.Add(connectionSelectorCombo);

            // Query input
            var queryPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            var queryLabel = new TextBlock
            {
                Text = "SQL Query:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _queryInput = new TextBox
            {
                Text = "SELECT * FROM users LIMIT 100;",
                Height = 100,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = "Consolas"
            };

            queryPanel.Children.Add(queryLabel);
            queryPanel.Children.Add(_queryInput);

            // Execute button
            var executeButton = new Button
            {
                Content = "⚡ Execute Query",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(15, 8)
            };
            executeButton.Click += OnExecuteQuery;

            // Query results
            var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

            var resultsLabel = new TextBlock
            {
                Text = "Query Results:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _queryResults = new ListBox { Height = 300 };

            resultsPanel.Children.Add(resultsLabel);
            resultsPanel.Children.Add(_queryResults);

            panel.Children.Add(connectionSelector);
            panel.Children.Add(queryPanel);
            panel.Children.Add(executeButton);
            panel.Children.Add(resultsPanel);

            tab.Content = panel;
            return tab;
        }

        private TabItem CreateSchemaTab()
        {
            var tab = new TabItem { Header = "📊 Schema" };

            var panel = new StackPanel { Margin = new Thickness(15) };

            var schemaLabel = new TextBlock
            {
                Text = "Database Schema:",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var schemaText = new TextBox
            {
                Text = "Select a connection and click 'Load Schema' to view database structure.",
                Height = 400,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = "Consolas",
                Background = Brushes.LightGray
            };

            var loadSchemaButton = new Button
            {
                Content = "📊 Load Schema",
                Background = Brushes.Blue,
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8),
                Margin = new Thickness(0, 15, 0, 0)
            };
            loadSchemaButton.Click += OnLoadSchema;

            panel.Children.Add(schemaLabel);
            panel.Children.Add(schemaText);
            panel.Children.Add(loadSchemaButton);

            tab.Content = panel;
            return tab;
        }

        private void OnTestConnection(object? sender, RoutedEventArgs e)
        {
            var name = "TestConnection";
            var provider = _providerCombo?.SelectedItem?.ToString()?.ToLower().Replace(" ", "") ?? "sqlserver";
            var connectionString = _connectionStringInput?.Text?.Trim();

            if (string.IsNullOrEmpty(connectionString))
            {
                UpdateStatus("❌ Please enter a connection string");
                return;
            }

            UpdateStatus("🧪 Testing connection...");
            _ = TestConnectionAsync(name, provider, connectionString);
        }

        private async Task TestConnectionAsync(string name, string provider, string connectionString)
        {
            try
            {
                var connection = await DatabaseManager.CreateConnectionAsync(name, provider, connectionString, false);

                if (connection != null && connection.IsConnected)
                {
                    UpdateStatus("✅ Connection test successful");
                    DatabaseManager.CloseConnection(connection.Id);
                }
                else
                {
                    UpdateStatus("❌ Connection test failed");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Connection test failed: {ex.Message}");
            }
        }

        private void OnAddConnection(object? sender, RoutedEventArgs e)
        {
            var name = "NewConnection";
            var provider = _providerCombo?.SelectedItem?.ToString()?.ToLower().Replace(" ", "") ?? "sqlserver";
            var connectionString = _connectionStringInput?.Text?.Trim();

            if (string.IsNullOrEmpty(connectionString))
            {
                UpdateStatus("❌ Please enter a connection string");
                return;
            }

            UpdateStatus("➕ Adding connection...");
            _ = AddConnectionAsync(name, provider, connectionString);
        }

        private async Task AddConnectionAsync(string name, string provider, string connectionString)
        {
            try
            {
                var connection = await DatabaseManager.CreateConnectionAsync(name, provider, connectionString);

                if (connection != null)
                {
                    UpdateStatus("✅ Connection added successfully");
                    RefreshConnectionsList();
                }
                else
                {
                    UpdateStatus("❌ Failed to add connection");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Failed to add connection: {ex.Message}");
            }
        }

        private void OnExecuteQuery(object? sender, RoutedEventArgs e)
        {
            var query = _queryInput?.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                UpdateStatus("❌ Please enter a SQL query");
                return;
            }

            UpdateStatus("⚡ Executing query...");
            _ = ExecuteQueryAsync(query);
        }

        private async Task ExecuteQueryAsync(string query)
        {
            try
            {
                // For demo, use the first available connection
                var connection = DatabaseManager.Connections.FirstOrDefault(c => c.IsConnected);
                if (connection == null)
                {
                    UpdateStatus("❌ No active database connections");
                    return;
                }

                var result = await DatabaseManager.ExecuteQueryAsync(connection.Id, query);

                if (result != null)
                {
                    if (result.Success)
                    {
                        _queryResults?.Items.Clear();

                        if (result.Rows.Any())
                        {
                            // Show column headers
                            var header = string.Join(" | ", result.ColumnNames);
                            _queryResults?.Items.Add($"📊 {header}");

                            // Show data rows (limit to first 50)
                            foreach (var row in result.Rows.Take(50))
                            {
                                var values = result.ColumnNames.Select(col => row[col]?.ToString() ?? "NULL");
                                _queryResults?.Items.Add($"   {string.Join(" | ", values)}");
                            }

                            if (result.Rows.Count > 50)
                            {
                                _queryResults?.Items.Add($"   ... and {result.Rows.Count - 50} more rows");
                            }
                        }

                        UpdateStatus($"✅ Query executed: {result.RowCount} rows in {result.ExecutionTime.TotalMilliseconds}ms");
                    }
                    else
                    {
                        UpdateStatus($"❌ Query failed: {result.ErrorMessage}");
                    }
                }
                else
                {
                    UpdateStatus("❌ Query execution failed");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Query execution error: {ex.Message}");
            }
        }

        private void OnConnectionSelected(object? sender, SelectionChangedEventArgs e)
        {
            // Connection selection logic
            UpdateStatus("Connection selected");
        }

        private void OnLoadSchema(object? sender, RoutedEventArgs e)
        {
            UpdateStatus("📊 Loading schema...");
            _ = LoadSchemaAsync();
        }

        private async Task LoadSchemaAsync()
        {
            try
            {
                var connection = DatabaseManager.Connections.FirstOrDefault(c => c.IsConnected);
                if (connection == null)
                {
                    UpdateStatus("❌ No active database connections");
                    return;
                }

                var schema = await DatabaseManager.GetSchemaAsync(connection.Id);

                if (schema != null)
                {
                    UpdateStatus($"✅ Schema loaded: {schema.Tables.Count} tables, {schema.Views.Count} views");
                    // Would display schema in the UI
                }
                else
                {
                    UpdateStatus("❌ Failed to load schema");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Schema loading failed: {ex.Message}");
            }
        }

        private void RefreshConnectionsList()
        {
            if (_connectionsList != null)
            {
                _connectionsList.Items.Clear();
                foreach (var connection in DatabaseManager.Connections)
                {
                    var status = connection.IsConnected ? "✅" : "❌";
                    _connectionsList.Items.Add($"{status} {connection.Name} ({connection.Provider})");
                }
            }
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
            }

            Logger.Log($"Database Manager: {message}");
        }
    }
}

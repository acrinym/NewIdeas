using System;
using System.Collections.ObjectModel;
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
    /// HARDWARE MONITOR - Comprehensive system monitoring and performance analysis
    /// Provides real-time CPU, memory, disk, and network monitoring with alerting
    /// </summary>
    public class HardwareMonitorPlugin : IPlugin
    {
        public string Name => "Hardware Monitor";
        public string Description => "Comprehensive system monitoring and performance analysis";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;

        public class HardwareMonitorWidget : IWidget
        {
            public string Name => "Hardware Monitor";

            private TabControl? _mainTabControl;
            private TextBlock? _statusText;
            private ListBox? _performanceHistory;
            private ListBox? _processList;
            private ListBox? _alertsList;
            private TextBlock? _cpuText;
            private TextBlock? _memoryText;
            private TextBlock? _diskText;
            private TextBlock? _networkText;

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
                    Text = "🔧 Hardware Monitor",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold
                };

                var statusPanel = new Border
                {
                    Background = Brushes.LightGray,
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 4),
                    Margin = new Thickness(15, 0, 0, 0)
                };

                _statusText = new TextBlock
                {
                    Text = "Ready",
                    FontSize = 12
                };

                statusPanel.Child = _statusText;

                headerPanel.Children.Add(headerText);
                headerPanel.Children.Add(statusPanel);

                // Main tab control
                _mainTabControl = new TabControl();

                // Overview Tab
                var overviewTab = CreateOverviewTab();
                _mainTabControl.Items.Add(overviewTab);

                // Performance History Tab
                var historyTab = CreatePerformanceHistoryTab();
                _mainTabControl.Items.Add(historyTab);

                // Processes Tab
                var processesTab = CreateProcessesTab();
                _mainTabControl.Items.Add(processesTab);

                // Alerts Tab
                var alertsTab = CreateAlertsTab();
                _mainTabControl.Items.Add(alertsTab);

                mainPanel.Children.Add(headerPanel);
                mainPanel.Children.Add(_mainTabControl);

                var border = new Border
                {
                    Child = mainPanel,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10)
                };

                return border;
            }

            private TabItem CreateOverviewTab()
            {
                var tab = new TabItem { Header = "📊 Overview" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Real-time metrics
                var metricsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

                var metricsGrid = new Grid();
                metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                metricsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                metricsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                // CPU
                var cpuPanel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
                var cpuLabel = new TextBlock { Text = "CPU Usage", FontWeight = FontWeight.Bold };
                _cpuText = new TextBlock { Text = "0%", FontSize = 24, Foreground = Brushes.Blue };
                cpuPanel.Children.Add(cpuLabel);
                cpuPanel.Children.Add(_cpuText);

                // Memory
                var memoryPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                var memoryLabel = new TextBlock { Text = "Memory Usage", FontWeight = FontWeight.Bold };
                _memoryText = new TextBlock { Text = "0%", FontSize = 24, Foreground = Brushes.Green };
                memoryPanel.Children.Add(memoryLabel);
                memoryPanel.Children.Add(_memoryText);

                metricsGrid.Children.Add(cpuPanel);
                metricsGrid.Children.Add(memoryPanel);
                Grid.SetColumn(memoryPanel, 1);

                // Disk
                var diskPanel = new StackPanel { Margin = new Thickness(0, 10, 10, 0) };
                var diskLabel = new TextBlock { Text = "Disk Usage", FontWeight = FontWeight.Bold };
                _diskText = new TextBlock { Text = "0%", FontSize = 24, Foreground = Brushes.Orange };
                diskPanel.Children.Add(diskLabel);
                diskPanel.Children.Add(_diskText);

                // Network
                var networkPanel = new StackPanel { Margin = new Thickness(10, 10, 0, 0) };
                var networkLabel = new TextBlock { Text = "Network Usage", FontWeight = FontWeight.Bold };
                _networkText = new TextBlock { Text = "0 MB/s", FontSize = 24, Foreground = Brushes.Purple };
                networkPanel.Children.Add(networkLabel);
                networkPanel.Children.Add(_networkText);

                metricsGrid.Children.Add(diskPanel);
                metricsGrid.Children.Add(networkPanel);
                Grid.SetRow(diskPanel, 1);
                Grid.SetColumn(networkPanel, 1);

                metricsPanel.Children.Add(metricsGrid);

                // Controls
                var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                var startButton = new Button
                {
                    Content = "▶️ Start Monitoring",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                startButton.Click += OnStartMonitoring;

                var stopButton = new Button
                {
                    Content = "⏹️ Stop Monitoring",
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                stopButton.Click += OnStopMonitoring;

                var refreshButton = new Button
                {
                    Content = "🔄 Refresh",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                refreshButton.Click += OnRefreshData;

                controlsPanel.Children.Add(startButton);
                controlsPanel.Children.Add(stopButton);
                controlsPanel.Children.Add(refreshButton);

                panel.Children.Add(metricsPanel);
                panel.Children.Add(controlsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreatePerformanceHistoryTab()
            {
                var tab = new TabItem { Header = "📈 Performance History" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                var historyLabel = new TextBlock
                {
                    Text = "📊 Performance History:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _performanceHistory = new ListBox { Height = 400 };

                panel.Children.Add(historyLabel);
                panel.Children.Add(_performanceHistory);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateProcessesTab()
            {
                var tab = new TabItem { Header = "⚙️ Processes" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                var processesLabel = new TextBlock
                {
                    Text = "🔧 Top Memory Processes:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _processList = new ListBox { Height = 400 };

                panel.Children.Add(processesLabel);
                panel.Children.Add(_processList);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateAlertsTab()
            {
                var tab = new TabItem { Header = "🚨 System Alerts" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                var alertsLabel = new TextBlock
                {
                    Text = "⚠️ Active System Alerts:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _alertsList = new ListBox { Height = 400 };

                panel.Children.Add(alertsLabel);
                panel.Children.Add(_alertsList);

                tab.Content = panel;
                return tab;
            }

            private async void OnStartMonitoring(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("📊 Starting hardware monitoring...");

                var success = await HardwareMonitor.StartMonitoringAsync();
                if (success)
                {
                    UpdateStatus("✅ Hardware monitoring active");
                    SubscribeToHardwareEvents();
                }
                else
                {
                    UpdateStatus("❌ Failed to start monitoring");
                }
            }

            private async void OnStopMonitoring(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🛑 Stopping hardware monitoring...");
                await HardwareMonitor.StopMonitoringAsync();
                UpdateStatus("✅ Hardware monitoring stopped");
            }

            private void OnRefreshData(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🔄 Refreshing system data...");

                // Refresh system information
                var systemInfo = HardwareMonitor.GetSystemInformation();
                Logger.Log($"System: {systemInfo.MachineName} - {systemInfo.OsVersion}");

                UpdateStatus("✅ System data refreshed");
            }

            private void SubscribeToHardwareEvents()
            {
                HardwareMonitor.PerformanceDataUpdated += OnPerformanceDataUpdated;
                HardwareMonitor.SystemAlertTriggered += OnSystemAlertTriggered;
                HardwareMonitor.ProcessInfoUpdated += OnProcessInfoUpdated;
            }

            private void OnPerformanceDataUpdated(object? sender, PerformanceDataEventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_cpuText != null)
                        _cpuText.Text = $"{e.Snapshot.CpuUsage}%";

                    if (_memoryText != null)
                        _memoryText.Text = $"{e.Snapshot.MemoryUsage.UsagePercent}%";

                    if (_diskText != null)
                        _diskText.Text = $"{e.Snapshot.DiskUsage.UsagePercent}%";

                    if (_networkText != null)
                    {
                        var sent = e.Snapshot.NetworkUsage.BytesSent / 1024 / 1024;
                        var received = e.Snapshot.NetworkUsage.BytesReceived / 1024 / 1024;
                        _networkText.Text = $"{received:F1}↓ / {sent:F1}↑ MB/s";
                    }

                    // Add to history
                    if (_performanceHistory != null)
                    {
                        var historyItem = $"{e.Snapshot.Timestamp:HH:mm:ss} | CPU: {e.Snapshot.CpuUsage}% | Mem: {e.Snapshot.MemoryUsage.UsagePercent}% | Disk: {e.Snapshot.DiskUsage.UsagePercent}%";
                        _performanceHistory.Items.Add(historyItem);

                        if (_performanceHistory.Items.Count > 100)
                            _performanceHistory.Items.RemoveAt(0);
                    }
                });
            }

            private void OnSystemAlertTriggered(object? sender, SystemAlertEventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_alertsList != null)
                    {
                        var severityColor = e.Alert.Severity switch
                        {
                            AlertSeverity.Critical => Brushes.Red,
                            AlertSeverity.Warning => Brushes.Orange,
                            _ => Brushes.Blue
                        };

                        var alertItem = $"{e.Alert.Timestamp:HH:mm:ss} | {e.Alert.Title} | {e.Alert.Message}";
                        _alertsList.Items.Add(alertItem);

                        if (_alertsList.Items.Count > 50)
                            _alertsList.Items.RemoveAt(0);
                    }
                });
            }

            private void OnProcessInfoUpdated(object? sender, ProcessInfoEventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_processList != null)
                    {
                        _processList.Items.Clear();
                        foreach (var process in e.Processes)
                        {
                            var memoryMB = process.MemoryUsage / 1024 / 1024;
                            var processItem = $"{process.Name} | PID: {process.Id} | Mem: {memoryMB:F1} MB | CPU: {process.CpuUsage}%";
                            _processList.Items.Add(processItem);
                        }
                    }
                });
            }

            private void UpdateStatus(string message)
            {
                if (_statusText != null)
                {
                    _statusText.Text = message;
                }

                Logger.Log($"Hardware Monitor: {message}");
            }
        }

        public IWidget? Widget => new HardwareMonitorWidget();

        public void Start()
        {
            Logger.Log("🔧 Hardware Monitor Plugin started - System monitoring active!");

            // Initialize Hardware Monitor service
            _ = HardwareMonitor.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🛑 Hardware Monitor Plugin stopped");
        }
    }
}

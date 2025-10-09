using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    /// NETWORK TOOLS - Comprehensive network analysis and security toolkit
    /// Provides packet sniffing, port scanning, HTTP inspection, and traffic monitoring
/// </summary>
public class NetworkToolsPlugin : IPlugin
{
    public string Name => "Network Tools";
        public string Description => "Comprehensive network analysis and security toolkit";
        public Version Version => new(1, 0, 0);
    public bool ForceDefaultTheme => false;

        public class NetworkToolsWidget : IWidget
        {
            public string Name => "Network Tools";

            private TabControl? _mainTabControl;
            private TextBlock? _statusText;
            private ListBox? _packetList;
            private ListBox? _portScanResults;
            private ListBox? _httpRequests;
            private ComboBox? _interfaceSelector;
            private TextBox? _targetHostInput;
            private TextBox? _portRangeInput;

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
                    Text = "🌐 Network Tools",
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

                // Packet Sniffer Tab
                var packetTab = CreatePacketSnifferTab();
                _mainTabControl.Items.Add(packetTab);

                // Port Scanner Tab
                var portTab = CreatePortScannerTab();
                _mainTabControl.Items.Add(portTab);

                // HTTP Inspector Tab
                var httpTab = CreateHttpInspectorTab();
                _mainTabControl.Items.Add(httpTab);

                // Network Monitor Tab
                var monitorTab = CreateNetworkMonitorTab();
                _mainTabControl.Items.Add(monitorTab);

                // MAC/IP Spoofing Tab
                var spoofingTab = CreateSpoofingTab();
                _mainTabControl.Items.Add(spoofingTab);

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

            private TabItem CreatePacketSnifferTab()
            {
                var tab = new TabItem { Header = "📡 Packet Sniffer" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Controls
                var controlsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var interfacePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

                var interfaceLabel = new TextBlock { Text = "Interface:", Margin = new Thickness(0, 0, 10, 0) };
                _interfaceSelector = new ComboBox { Width = 200 };

                // Populate interfaces
                foreach (var interfaceInfo in NetworkTools.NetworkInterfaces)
                {
                    _interfaceSelector.Items.Add(interfaceInfo.Name);
                }

                interfacePanel.Children.Add(interfaceLabel);
                interfacePanel.Children.Add(_interfaceSelector);

                var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                var startButton = new Button
                {
                    Content = "▶️ Start Capture",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                startButton.Click += OnStartPacketCapture;

                var stopButton = new Button
                {
                    Content = "⏹️ Stop Capture",
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                stopButton.Click += OnStopPacketCapture;

                var clearButton = new Button
                {
                    Content = "🗑️ Clear",
                    Background = Brushes.Gray,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                clearButton.Click += OnClearPackets;

                buttonsPanel.Children.Add(startButton);
                buttonsPanel.Children.Add(stopButton);
                buttonsPanel.Children.Add(clearButton);

                controlsPanel.Children.Add(interfacePanel);
                controlsPanel.Children.Add(buttonsPanel);

                // Packet list
                var packetPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var packetLabel = new TextBlock
                {
                    Text = "📊 Captured Packets:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _packetList = new ListBox { Height = 300 };

                packetPanel.Children.Add(packetLabel);
                packetPanel.Children.Add(_packetList);

                panel.Children.Add(controlsPanel);
                panel.Children.Add(packetPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreatePortScannerTab()
            {
                var tab = new TabItem { Header = "🔍 Port Scanner" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Target input
                var targetPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var targetLabel = new TextBlock { Text = "Target Host:", FontWeight = FontWeight.Bold };
                _targetHostInput = new TextBox
                {
                    Text = "localhost",
                    Width = 200,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var portLabel = new TextBlock { Text = "Port Range:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
                _portRangeInput = new TextBox
                {
                    Text = "1-1024",
                    Width = 200,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                targetPanel.Children.Add(targetLabel);
                targetPanel.Children.Add(_targetHostInput);
                targetPanel.Children.Add(portLabel);
                targetPanel.Children.Add(_portRangeInput);

                // Scan button
                var scanButton = new Button
                {
                    Content = "🔍 Start Scan",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold,
                    Padding = new Thickness(15, 8),
                    Margin = new Thickness(0, 15, 0, 0)
                };
                scanButton.Click += OnStartPortScan;

                // Results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };

                var resultsLabel = new TextBlock
                {
                    Text = "📊 Scan Results:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _portScanResults = new ListBox { Height = 300 };

                resultsPanel.Children.Add(resultsLabel);
                resultsPanel.Children.Add(_portScanResults);

                panel.Children.Add(targetPanel);
                panel.Children.Add(scanButton);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateHttpInspectorTab()
            {
                var tab = new TabItem { Header = "🌐 HTTP Inspector" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Controls
                var controlsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var monitorButton = new Button
                {
                    Content = "▶️ Start Monitoring",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                monitorButton.Click += OnStartHttpMonitoring;

                var stopButton = new Button
                {
                    Content = "⏹️ Stop Monitoring",
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8),
                    Margin = new Thickness(10, 0, 0, 0)
                };
                stopButton.Click += OnStopHttpMonitoring;

                controlsPanel.Children.Add(monitorButton);
                controlsPanel.Children.Add(stopButton);

                // HTTP requests list
                var requestsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var requestsLabel = new TextBlock
                {
                    Text = "📋 HTTP Requests:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _httpRequests = new ListBox { Height = 400 };

                requestsPanel.Children.Add(requestsLabel);
                requestsPanel.Children.Add(_httpRequests);

                panel.Children.Add(controlsPanel);
                panel.Children.Add(requestsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateNetworkMonitorTab()
            {
                var tab = new TabItem { Header = "📊 Network Monitor" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Network interfaces
                var interfacesPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

                var interfacesLabel = new TextBlock
                {
                    Text = "🌐 Network Interfaces:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var interfacesList = new ListBox { Height = 150 };

                foreach (var interfaceInfo in NetworkTools.NetworkInterfaces)
                {
                    interfacesList.Items.Add($"{interfaceInfo.Name} - {interfaceInfo.IPv4Address} ({interfaceInfo.Type})");
                }

                interfacesPanel.Children.Add(interfacesLabel);
                interfacesPanel.Children.Add(interfacesList);

                // Statistics
                var statsPanel = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };

                var statsLabel = new TextBlock
                {
                    Text = "📈 Network Statistics:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var statsGrid = new Grid();
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var packetsLabel = new TextBlock { Text = "📦 Packets Captured:" };
                var packetsValue = new TextBlock { Text = "0", FontWeight = FontWeight.Bold };

                var portsLabel = new TextBlock { Text = "🔍 Ports Scanned:" };
                var portsValue = new TextBlock { Text = "0", FontWeight = FontWeight.Bold };

                var requestsLabel = new TextBlock { Text = "🌐 HTTP Requests:" };
                var requestsValue = new TextBlock { Text = "0", FontWeight = FontWeight.Bold };

                statsGrid.Children.Add(packetsLabel);
                statsGrid.Children.Add(packetsValue);
                Grid.SetColumn(packetsValue, 1);

                statsGrid.Children.Add(portsLabel);
                statsGrid.Children.Add(portsValue);
                Grid.SetColumn(portsValue, 1);
                Grid.SetRow(portsLabel, 1);

                statsGrid.Children.Add(requestsLabel);
                statsGrid.Children.Add(requestsValue);
                Grid.SetColumn(requestsValue, 1);
                Grid.SetRow(requestsLabel, 2);

                statsPanel.Children.Add(statsLabel);
                statsPanel.Children.Add(statsGrid);

                panel.Children.Add(interfacesPanel);
                panel.Children.Add(statsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateSpoofingTab()
            {
                var tab = new TabItem { Header = "🔒 MAC/IP Spoofing" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Warning panel
                var warningPanel = new Border
                {
                    Background = Brushes.Orange,
                    BorderBrush = Brushes.DarkOrange,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 0, 20),
                    Padding = new Thickness(10)
                };

                var warningText = new TextBlock
                {
                    Text = "⚠️ WARNING: MAC and IP spoofing can violate network policies, break connectivity, and may be illegal in some jurisdictions. Use only for authorized testing and educational purposes.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.DarkRed,
                    FontWeight = FontWeight.Bold
                };

                warningPanel.Child = warningText;
                panel.Children.Add(warningPanel);

                // MAC Address Spoofing
                var macPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

                var macLabel = new TextBlock
                {
                    Text = "🔗 MAC Address Spoofing:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var macControls = new StackPanel { Spacing = 10 };

                var macInterfacePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                var macInterfaceLabel = new TextBlock { Text = "Interface:", Margin = new Thickness(0, 0, 10, 0) };
                var macInterfaceCombo = new ComboBox { Width = 200 };

                // Populate MAC interfaces
                foreach (var interfaceInfo in NetworkTools.NetworkInterfaces)
                {
                    macInterfaceCombo.Items.Add(interfaceInfo.Name);
                }

                macInterfacePanel.Children.Add(macInterfaceLabel);
                macInterfacePanel.Children.Add(macInterfaceCombo);

                var macInputPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                var macLabel2 = new TextBlock { Text = "New MAC:", Margin = new Thickness(0, 0, 10, 0) };
                var macInput = new TextBox
                {
                    Text = NetworkTools.GenerateRandomMacAddress(),
                    Width = 150,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var generateMacButton = new Button
                {
                    Content = "🎲 Random",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(8, 4)
                };
                generateMacButton.Click += (_, _) =>
                {
                    macInput.Text = NetworkTools.GenerateRandomMacAddress();
                };

                macInputPanel.Children.Add(macLabel2);
                macInputPanel.Children.Add(macInput);
                macInputPanel.Children.Add(generateMacButton);

                var macButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var changeMacButton = new Button
                {
                    Content = "🔄 Change MAC",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                changeMacButton.Click += async (_, _) =>
                {
                    var interfaceName = macInterfaceCombo.SelectedItem?.ToString();
                    var newMac = macInput.Text;

                    if (string.IsNullOrEmpty(interfaceName) || string.IsNullOrEmpty(newMac))
                    {
                        UpdateStatus("❌ Please select interface and enter MAC address");
                        return;
                    }

                    UpdateStatus($"🔄 Changing MAC address of {interfaceName}...");
                    var success = await NetworkTools.ChangeMacAddressAsync(interfaceName, newMac);

                    if (success)
                    {
                        UpdateStatus($"✅ MAC address changed to {newMac}");
                    }
                    else
                    {
                        UpdateStatus("❌ MAC address change failed");
                    }
                };

                var restoreMacButton = new Button
                {
                    Content = "🔄 Restore MAC",
                    Background = Brushes.Orange,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                restoreMacButton.Click += async (_, _) =>
                {
                    var interfaceName = macInterfaceCombo.SelectedItem?.ToString();

                    if (string.IsNullOrEmpty(interfaceName))
                    {
                        UpdateStatus("❌ Please select interface");
                        return;
                    }

                    UpdateStatus($"🔄 Restoring MAC address of {interfaceName}...");
                    var success = await NetworkTools.RestoreMacAddressAsync(interfaceName);

                    if (success)
                    {
                        UpdateStatus("✅ MAC address restored");
                    }
                    else
                    {
                        UpdateStatus("❌ MAC address restoration failed");
                    }
                };

                macButtonsPanel.Children.Add(changeMacButton);
                macButtonsPanel.Children.Add(restoreMacButton);

                macControls.Children.Add(macInterfacePanel);
                macControls.Children.Add(macInputPanel);
                macControls.Children.Add(macButtonsPanel);

                macPanel.Children.Add(macLabel);
                macPanel.Children.Add(macControls);

                // IP Address Spoofing
                var ipPanel = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };

                var ipLabel = new TextBlock
                {
                    Text = "🌐 IP Address Spoofing:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var ipControls = new StackPanel { Spacing = 10 };

                var ipInterfacePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                var ipInterfaceLabel = new TextBlock { Text = "Interface:", Margin = new Thickness(0, 0, 10, 0) };
                var ipInterfaceCombo = new ComboBox { Width = 200 };

                // Populate IP interfaces
                foreach (var interfaceInfo in NetworkTools.NetworkInterfaces)
                {
                    ipInterfaceCombo.Items.Add(interfaceInfo.Name);
                }

                ipInterfacePanel.Children.Add(ipInterfaceLabel);
                ipInterfacePanel.Children.Add(ipInterfaceCombo);

                var ipInputPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                var ipLabel2 = new TextBlock { Text = "New IP:", Margin = new Thickness(0, 0, 10, 0) };
                var ipInput = new TextBox
                {
                    Text = "192.168.1.100",
                    Width = 120,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var subnetLabel = new TextBlock { Text = "Subnet:", Margin = new Thickness(0, 0, 10, 0) };
                var subnetInput = new TextBox
                {
                    Text = "255.255.255.0",
                    Width = 120,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var gatewayLabel = new TextBlock { Text = "Gateway:", Margin = new Thickness(0, 0, 10, 0) };
                var gatewayInput = new TextBox
                {
                    Text = "192.168.1.1",
                    Width = 120
                };

                ipInputPanel.Children.Add(ipLabel2);
                ipInputPanel.Children.Add(ipInput);
                ipInputPanel.Children.Add(subnetLabel);
                ipInputPanel.Children.Add(subnetInput);
                ipInputPanel.Children.Add(gatewayLabel);
                ipInputPanel.Children.Add(gatewayInput);

                var ipButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var spoofIpButton = new Button
                {
                    Content = "🌐 Spoof IP",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                spoofIpButton.Click += async (_, _) =>
                {
                    var interfaceName = ipInterfaceCombo.SelectedItem?.ToString();
                    var newIp = ipInput.Text;
                    var subnet = subnetInput.Text;
                    var gateway = gatewayInput.Text;

                    if (string.IsNullOrEmpty(interfaceName) || string.IsNullOrEmpty(newIp))
                    {
                        UpdateStatus("❌ Please select interface and enter IP address");
                        return;
                    }

                    UpdateStatus($"🌐 Spoofing IP address of {interfaceName} to {newIp}...");
                    var success = await NetworkTools.SpoofIpAddressAsync(interfaceName, newIp, subnet, gateway);

                    if (success)
                    {
                        UpdateStatus($"✅ IP address spoofed to {newIp}");
                    }
                    else
                    {
                        UpdateStatus("❌ IP address spoofing failed");
                    }
                };

                var restoreIpButton = new Button
                {
                    Content = "🔄 Restore IP",
                    Background = Brushes.Orange,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                restoreIpButton.Click += async (_, _) =>
                {
                    var interfaceName = ipInterfaceCombo.SelectedItem?.ToString();

                    if (string.IsNullOrEmpty(interfaceName))
                    {
                        UpdateStatus("❌ Please select interface");
                        return;
                    }

                    UpdateStatus($"🔄 Restoring IP configuration of {interfaceName}...");
                    var success = await NetworkTools.RestoreIpConfigurationAsync(interfaceName);

                    if (success)
                    {
                        UpdateStatus("✅ IP configuration restored");
                    }
                    else
                    {
                        UpdateStatus("❌ IP configuration restoration failed");
                    }
                };

                ipButtonsPanel.Children.Add(spoofIpButton);
                ipButtonsPanel.Children.Add(restoreIpButton);

                ipControls.Children.Add(ipInterfacePanel);
                ipControls.Children.Add(ipInputPanel);
                ipControls.Children.Add(ipButtonsPanel);

                ipPanel.Children.Add(ipLabel);
                ipPanel.Children.Add(ipControls);

                panel.Children.Add(macPanel);
                panel.Children.Add(ipPanel);

                tab.Content = panel;
                return tab;
            }

            private async void OnStartPacketCapture(object? sender, RoutedEventArgs e)
            {
                var interfaceName = _interfaceSelector?.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(interfaceName))
                {
                    UpdateStatus("❌ Please select a network interface");
                    return;
                }

                UpdateStatus($"📡 Starting packet capture on {interfaceName}...");

                var success = await NetworkTools.StartPacketCaptureAsync(interfaceName);
                if (success)
                {
                    UpdateStatus("✅ Packet capture started");
                    SubscribeToNetworkEvents();
                }
                else
                {
                    UpdateStatus("❌ Failed to start packet capture");
                }
            }

            private async void OnStopPacketCapture(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🛑 Stopping packet capture...");
                await NetworkTools.StopPacketCaptureAsync();
                UpdateStatus("✅ Packet capture stopped");
            }

            private void OnClearPackets(object? sender, RoutedEventArgs e)
            {
                NetworkTools.CapturedPackets.Clear();
                UpdateStatus("🗑️ Packets cleared");
            }

            private async void OnStartPortScan(object? sender, RoutedEventArgs e)
            {
                var targetHost = _targetHostInput?.Text?.Trim();
                if (string.IsNullOrEmpty(targetHost))
                {
                    UpdateStatus("❌ Please enter a target host");
                    return;
                }

                // Parse port range
                var portRange = _portRangeInput?.Text?.Trim() ?? "1-1024";
                var parts = portRange.Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[0], out var startPort) || !int.TryParse(parts[1], out var endPort))
                {
                    UpdateStatus("❌ Invalid port range format. Use '1-1024'");
                    return;
                }

                UpdateStatus($"🔍 Starting port scan on {targetHost}:{startPort}-{endPort}...");

                var success = await NetworkTools.StartPortScanAsync(targetHost, startPort, endPort);
                if (success)
                {
                    UpdateStatus("✅ Port scan started");
                }
                else
                {
                    UpdateStatus("❌ Failed to start port scan");
                }
            }

            private async void OnStartHttpMonitoring(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🌐 Starting HTTP traffic monitoring...");

                var success = await NetworkTools.StartHttpMonitoringAsync();
                if (success)
                {
                    UpdateStatus("✅ HTTP monitoring started");
                }
                else
                {
                    UpdateStatus("❌ Failed to start HTTP monitoring");
                }
            }

            private async void OnStopHttpMonitoring(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🛑 Stopping HTTP monitoring...");
                await NetworkTools.StopHttpMonitoringAsync();
                UpdateStatus("✅ HTTP monitoring stopped");
            }

            private void SubscribeToNetworkEvents()
            {
                NetworkTools.PacketCaptured += OnPacketCaptured;
                NetworkTools.PortScanCompleted += OnPortScanCompleted;
                NetworkTools.HttpRequestCaptured += OnHttpRequestCaptured;
            }

            private void OnPacketCaptured(object? sender, NetworkPacketEventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_packetList != null)
                    {
                        var item = $"{e.Packet.Timestamp:HH:mm:ss} | {e.Packet.Protocol} | {e.Packet.Source}:{e.Packet.Destination} | {e.Packet.Size} bytes";
                        _packetList.Items.Add(item);

                        // Keep only recent packets
                        if (_packetList.Items.Count > 100)
                        {
                            _packetList.Items.RemoveAt(0);
                        }

                        if (_packetList.Items.Count > 0)
                        {
                            var last = _packetList.Items[^1];
                            if (last != null)
                                _packetList.ScrollIntoView(last);
                        }
                    }
                });
            }

            private void OnPortScanCompleted(object? sender, PortScanEventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_portScanResults != null)
                    {
                        var item = $"{e.Result.Port} | {e.Result.Status} | {e.Result.Service} | {e.Result.ResponseTime}ms";
                        _portScanResults.Items.Add(item);

                        if (_portScanResults.Items.Count > 0)
                        {
                            var last = _portScanResults.Items[^1];
                            if (last != null)
                                _portScanResults.ScrollIntoView(last);
                        }
                    }
                });
            }

            private void OnHttpRequestCaptured(object? sender, HttpRequestEventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_httpRequests != null)
                    {
                        var item = $"{e.Request.Method} {e.Request.Url} | {e.Request.StatusCode} | {e.Request.ResponseSize} bytes | {e.Request.ResponseTime}ms";
                        _httpRequests.Items.Add(item);

                        // Keep only recent requests
                        if (_httpRequests.Items.Count > 50)
                        {
                            _httpRequests.Items.RemoveAt(0);
                        }

                        if (_httpRequests.Items.Count > 0)
                        {
                            var last = _httpRequests.Items[^1];
                            if (last != null)
                                _httpRequests.ScrollIntoView(last);
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

                Logger.Log($"Network Tools: {message}");
            }
        }

        public IWidget? Widget => new NetworkToolsWidget();

        public void Start()
        {
            Logger.Log("🌐 Network Tools Plugin started - Ready for network analysis!");

            // Initialize Network Tools service
            _ = NetworkTools.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🛑 Network Tools Plugin stopped");
        }
    }
}
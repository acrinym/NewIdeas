using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Widgets.Animations;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// A network monitoring widget that displays network interface statistics
/// </summary>
public class NetworkMonitorWidget : BaseWidget
{
    private Border? _container;
    private StackPanel? _metricsPanel;
    private Timer? _updateTimer;
    private readonly Dictionary<string, long> _previousBytesReceived = new();
    private readonly Dictionary<string, long> _previousBytesSent = new();
    private DateTime _lastUpdateTime = DateTime.Now;
    
    public override string Name => "Network Monitor";
    public override string Description => "Monitor network interface statistics and bandwidth usage";
    public override string Category => "System";
    public override string Icon => "network";
    public override (double Width, double Height) DefaultSize => (300, 250);
    public override (double Width, double Height) MinimumSize => (250, 200);
    public override bool IsResizable => true;
    
    public override WidgetConfigurationSchema ConfigurationSchema => new()
    {
        Properties = GetConfigurationProperties(),
        DefaultValues = GetDefaultConfiguration()
    };
    
    protected override List<WidgetConfigurationProperty> GetConfigurationProperties()
    {
        return new List<WidgetConfigurationProperty>
        {
            new()
            {
                Name = "updateInterval",
                DisplayName = "Update Interval (seconds)",
                Description = "How often to update network statistics",
                Type = WidgetPropertyType.Integer,
                DefaultValue = 2,
                IsRequired = false
            },
            new()
            {
                Name = "showDownload",
                DisplayName = "Show Download Speed",
                Description = "Display download speed information",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "showUpload",
                DisplayName = "Show Upload Speed",
                Description = "Display upload speed information",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "showTotalBytes",
                DisplayName = "Show Total Bytes",
                Description = "Display total bytes transferred",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = false,
                IsRequired = false
            },
            new()
            {
                Name = "selectedInterface",
                DisplayName = "Network Interface",
                Description = "Select which network interface to monitor (empty for all active)",
                Type = WidgetPropertyType.String,
                DefaultValue = "",
                IsRequired = false
            },
            new()
            {
                Name = "animateUpdates",
                DisplayName = "Animate Updates",
                Description = "Enable animations when values update",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            }
        };
    }
    
    protected override Dictionary<string, object> GetDefaultConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["updateInterval"] = 2,
            ["showDownload"] = true,
            ["showUpload"] = true,
            ["showTotalBytes"] = false,
            ["selectedInterface"] = "",
            ["animateUpdates"] = true
        };
    }
    
    public override Control BuildView(WidgetContext context)
    {
        var theme = context.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        
        // Create main container
        _container = new Border
        {
            Background = theme.BackgroundBrush,
            BorderBrush = theme.BorderBrush,
            BorderThickness = new Avalonia.Thickness(theme.BorderThickness),
            CornerRadius = new Avalonia.CornerRadius(theme.CornerRadius),
            Padding = new Avalonia.Thickness(theme.Padding)
        };
        
        // Create main panel
        var mainPanel = new StackPanel
        {
            Spacing = 8
        };
        
        // Title with icon
        var titlePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8
        };
        
        var icon = new TextBlock
        {
            Text = "🌐",
            FontSize = theme.FontSize + 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var title = new TextBlock
        {
            Text = "Network Monitor",
            FontSize = theme.FontSize + 1,
            FontWeight = FontWeight.Bold,
            Foreground = theme.ForegroundBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        titlePanel.Children.Add(icon);
        titlePanel.Children.Add(title);
        mainPanel.Children.Add(titlePanel);
        
        // Metrics panel
        _metricsPanel = new StackPanel
        {
            Spacing = 6
        };
        mainPanel.Children.Add(_metricsPanel);
        
        _container.Child = mainPanel;
        
        // Initial metrics display
        UpdateNetworkMetrics(theme);
        
        return _container;
    }
    
    public override async Task OnInitializeAsync(WidgetContext context)
    {
        await base.OnInitializeAsync(context);
        
        // Initialize previous values for all network interfaces
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                           ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPv4Statistics();
                _previousBytesReceived[ni.Name] = stats.BytesReceived;
                _previousBytesSent[ni.Name] = stats.BytesSent;
            }
            
            _lastUpdateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            PublishEvent("error", $"Failed to initialize network monitoring: {ex.Message}");
        }
    }
    
    protected override async Task OnActivateInternalAsync()
    {
        await base.OnActivateInternalAsync();
        
        // Start update timer
        var updateInterval = GetConfigurationValue("updateInterval", 2);
        _updateTimer = new Timer(async _ => await UpdateNetworkMetricsAsync(), 
                               null, TimeSpan.Zero, TimeSpan.FromSeconds(updateInterval));
        
        // Animate widget appearance
        if (_container != null)
        {
            await WidgetAnimations.FadeInAsync(_container);
        }
    }
    
    protected override async Task OnDeactivateInternalAsync()
    {
        await base.OnDeactivateInternalAsync();
        
        // Stop update timer
        _updateTimer?.Dispose();
        _updateTimer = null;
    }
    
    protected override async Task OnConfigurationChangedInternalAsync(Dictionary<string, object> newConfiguration)
    {
        await base.OnConfigurationChangedInternalAsync(newConfiguration);
        
        // Update timer interval if changed
        var newInterval = GetConfigurationValue("updateInterval", 2);
        _updateTimer?.Dispose();
        _updateTimer = new Timer(async _ => await UpdateNetworkMetricsAsync(), 
                               null, TimeSpan.Zero, TimeSpan.FromSeconds(newInterval));
        
        // Rebuild view to reflect configuration changes
        var theme = _context?.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        UpdateNetworkMetrics(theme);
    }
    
    protected override async Task OnThemeChangedInternalAsync(string themeName)
    {
        await base.OnThemeChangedInternalAsync(themeName);
        
        // Get the current theme
        var theme = _context?.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        
        // Update container appearance
        if (_container != null)
        {
            _container.Background = theme.BackgroundBrush;
            _container.BorderBrush = theme.BorderBrush;
            _container.BorderThickness = new Avalonia.Thickness(theme.BorderThickness);
            _container.CornerRadius = new Avalonia.CornerRadius(theme.CornerRadius);
            _container.Padding = new Avalonia.Thickness(theme.Padding);
        }
        
        // Update text colors
        UpdateTextColors(theme);
    }
    
    private async Task UpdateNetworkMetricsAsync()
    {
        try
        {
            var theme = _context?.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateNetworkMetrics(theme));
        }
        catch (Exception ex)
        {
            PublishEvent("error", $"Failed to update network metrics: {ex.Message}");
        }
    }
    
    private void UpdateNetworkMetrics(WidgetTheme theme)
    {
        if (_metricsPanel == null) return;
        
        _metricsPanel.Children.Clear();
        
        try
        {
            var selectedInterface = GetConfigurationValue("selectedInterface", "");
            var showDownload = GetConfigurationValue("showDownload", true);
            var showUpload = GetConfigurationValue("showUpload", true);
            var showTotalBytes = GetConfigurationValue("showTotalBytes", false);
            var animateUpdates = GetConfigurationValue("animateUpdates", true);
            
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                           ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            if (!string.IsNullOrEmpty(selectedInterface))
            {
                interfaces = interfaces.Where(ni => ni.Name.Equals(selectedInterface, StringComparison.OrdinalIgnoreCase));
            }
            
            var currentTime = DateTime.Now;
            var timeDiff = (currentTime - _lastUpdateTime).TotalSeconds;
            
            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPv4Statistics();
                var interfacePanel = CreateInterfacePanel(ni, stats, timeDiff, theme, 
                                                        showDownload, showUpload, showTotalBytes);
                _metricsPanel.Children.Add(interfacePanel);
                
                // Update previous values
                _previousBytesReceived[ni.Name] = stats.BytesReceived;
                _previousBytesSent[ni.Name] = stats.BytesSent;
                
                // Animate if enabled
                if (animateUpdates)
                {
                    _ = Task.Run(async () => await WidgetAnimations.PulseAsync(interfacePanel, 200));
                }
            }
            
            _lastUpdateTime = currentTime;
            
            if (!_metricsPanel.Children.Any())
            {
                var noDataText = new TextBlock
                {
                    Text = "No active network interfaces found",
                    Foreground = theme.ForegroundBrush,
                    FontSize = theme.FontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 10)
                };
                _metricsPanel.Children.Add(noDataText);
            }
        }
        catch (Exception ex)
        {
            var errorText = new TextBlock
            {
                Text = $"Error: {ex.Message}",
                Foreground = theme.ErrorBrush,
                FontSize = theme.FontSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10)
            };
            _metricsPanel.Children.Add(errorText);
        }
    }
    
    private Border CreateInterfacePanel(NetworkInterface ni, IPv4InterfaceStatistics stats, 
                                      double timeDiff, WidgetTheme theme,
                                      bool showDownload, bool showUpload, bool showTotalBytes)
    {
        var panel = new StackPanel
        {
            Spacing = 4
        };
        
        // Interface name
        var nameText = new TextBlock
        {
            Text = ni.Name,
            FontWeight = FontWeight.Bold,
            Foreground = theme.AccentBrush,
            FontSize = theme.FontSize
        };
        panel.Children.Add(nameText);
        
        // Calculate speeds
        var downloadSpeed = 0.0;
        var uploadSpeed = 0.0;
        
        if (_previousBytesReceived.ContainsKey(ni.Name) && timeDiff > 0)
        {
            downloadSpeed = (stats.BytesReceived - _previousBytesReceived[ni.Name]) / timeDiff;
            uploadSpeed = (stats.BytesSent - _previousBytesSent[ni.Name]) / timeDiff;
        }
        
        // Download speed
        if (showDownload)
        {
            var downloadText = new TextBlock
            {
                Text = $"↓ {FormatBytes(downloadSpeed)}/s",
                Foreground = theme.SuccessBrush,
                FontSize = theme.FontSize - 1
            };
            panel.Children.Add(downloadText);
        }
        
        // Upload speed
        if (showUpload)
        {
            var uploadText = new TextBlock
            {
                Text = $"↑ {FormatBytes(uploadSpeed)}/s",
                Foreground = theme.WarningBrush,
                FontSize = theme.FontSize - 1
            };
            panel.Children.Add(uploadText);
        }
        
        // Total bytes
        if (showTotalBytes)
        {
            var totalText = new TextBlock
            {
                Text = $"Total: ↓{FormatBytes(stats.BytesReceived)} ↑{FormatBytes(stats.BytesSent)}",
                Foreground = theme.ForegroundBrush,
                FontSize = theme.FontSize - 2,
                Opacity = 0.7
            };
            panel.Children.Add(totalText);
        }
        
        return new Border
        {
            Child = panel,
            Background = theme.SecondaryBrush,
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(8),
            Margin = new Avalonia.Thickness(0, 2)
        };
    }
    
    private void UpdateTextColors(WidgetTheme theme)
    {
        if (_container?.Child is StackPanel mainPanel)
        {
            // Update title color
            if (mainPanel.Children[0] is StackPanel titlePanel && 
                titlePanel.Children[1] is TextBlock titleText)
            {
                titleText.Foreground = theme.ForegroundBrush;
            }
        }
    }
    
    private static string FormatBytes(double bytes)
    {
        if (bytes < 0) return "0 B";
        
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        
        while (bytes >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            bytes /= 1024;
            suffixIndex++;
        }
        
        return $"{bytes:F1} {suffixes[suffixIndex]}";
    }
    
    public override async Task<Dictionary<string, object>> ExportDataAsync()
    {
        var data = await base.ExportDataAsync();
        data["previousBytesReceived"] = _previousBytesReceived;
        data["previousBytesSent"] = _previousBytesSent;
        data["lastUpdateTime"] = _lastUpdateTime;
        return data;
    }
    
    public override async Task ImportDataAsync(Dictionary<string, object> data)
    {
        await base.ImportDataAsync(data);
        
        if (data.ContainsKey("lastUpdateTime"))
        {
            _lastUpdateTime = Convert.ToDateTime(data["lastUpdateTime"]);
        }
    }
}
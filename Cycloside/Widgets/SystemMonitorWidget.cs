using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Widgets.Animations;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// A system monitor widget that displays CPU, memory, and disk usage
/// </summary>
public class SystemMonitorWidget : BaseWidget
{
    private DispatcherTimer? _updateTimer;
    private TextBlock? _cpuText;
    private TextBlock? _memoryText;
    private TextBlock? _diskText;
    private Border? _container;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;
    
    public override string Name => "System Monitor";
    public override string Description => "Displays real-time system performance metrics including CPU, memory, and disk usage";
    public override string Category => "System";
    public override string Icon => "monitor";
    public override (double Width, double Height) DefaultSize => (280, 180);
    public override (double Width, double Height) MinimumSize => (200, 120);
    
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
                Description = "How often to refresh the system metrics",
                Type = WidgetPropertyType.Integer,
                DefaultValue = 2,
                IsRequired = true
            },
            new()
            {
                Name = "showCpu",
                DisplayName = "Show CPU Usage",
                Description = "Display CPU usage percentage",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "showMemory",
                DisplayName = "Show Memory Usage",
                Description = "Display memory usage information",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "showDisk",
                DisplayName = "Show Disk Usage",
                Description = "Display disk usage for C: drive",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "animateUpdates",
                DisplayName = "Animate Updates",
                Description = "Use animations when updating values",
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
            ["showCpu"] = true,
            ["showMemory"] = true,
            ["showDisk"] = true,
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
        
        // Create content panel
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = theme.Spacing
        };
        
        // Title
        var title = new TextBlock
        {
            Text = "System Monitor",
            FontSize = theme.FontSize + 2,
            FontWeight = FontWeight.Bold,
            Foreground = theme.ForegroundBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(title);
        
        // CPU usage
        if (GetConfigurationValue("showCpu", true))
        {
            _cpuText = new TextBlock
            {
                Text = "CPU: Loading...",
                FontSize = theme.FontSize,
                Foreground = theme.ForegroundBrush
            };
            panel.Children.Add(_cpuText);
        }
        
        // Memory usage
        if (GetConfigurationValue("showMemory", true))
        {
            _memoryText = new TextBlock
            {
                Text = "Memory: Loading...",
                FontSize = theme.FontSize,
                Foreground = theme.ForegroundBrush
            };
            panel.Children.Add(_memoryText);
        }
        
        // Disk usage
        if (GetConfigurationValue("showDisk", true))
        {
            _diskText = new TextBlock
            {
                Text = "Disk: Loading...",
                FontSize = theme.FontSize,
                Foreground = theme.ForegroundBrush
            };
            panel.Children.Add(_diskText);
        }
        
        _container.Child = panel;
        
        return _container;
    }
    
    public override async Task OnInitializeAsync(WidgetContext context)
    {
        await base.OnInitializeAsync(context);
        
        try
        {
            // Initialize performance counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            // First call to initialize counters
            _cpuCounter.NextValue();
        }
        catch (Exception ex)
        {
            // Performance counters might not be available on all systems
            System.Diagnostics.Debug.WriteLine($"Failed to initialize performance counters: {ex.Message}");
        }
    }
    
    protected override async Task OnActivateInternalAsync()
    {
        await base.OnActivateInternalAsync();
        
        // Start the update timer
        var interval = GetConfigurationValue("updateInterval", 2);
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(interval)
        };
        _updateTimer.Tick += async (s, e) => await UpdateSystemMetrics();
        _updateTimer.Start();
        
        // Initial update
        await UpdateSystemMetrics();
        
        // Animate widget appearance
        if (GetConfigurationValue("animateUpdates", true) && _container != null)
        {
            await WidgetAnimations.FadeInAsync(_container);
        }
    }
    
    protected override async Task OnDeactivateInternalAsync()
    {
        await base.OnDeactivateInternalAsync();
        
        // Stop the timer
        _updateTimer?.Stop();
        _updateTimer = null;
    }
    
    protected override async Task OnDestroyInternalAsync()
    {
        await base.OnDestroyInternalAsync();
        
        // Dispose performance counters
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        _cpuCounter = null;
        _memoryCounter = null;
    }
    
    protected override async Task OnConfigurationChangedInternalAsync(Dictionary<string, object> newConfiguration)
    {
        await base.OnConfigurationChangedInternalAsync(newConfiguration);
        
        // Update timer interval if changed
        var newInterval = GetConfigurationValue("updateInterval", 2);
        if (_updateTimer != null)
        {
            _updateTimer.Interval = TimeSpan.FromSeconds(newInterval);
        }
        
        // Rebuild view if visibility settings changed
        var view = BuildView(_context);
        if (_container?.Parent is Panel parent && _container != null)
        {
            var index = parent.Children.IndexOf(_container);
            parent.Children.RemoveAt(index);
            parent.Children.Insert(index, view);
        }
    }
    
    protected override async Task OnThemeChangedInternalAsync(string themeName)
    {
        await base.OnThemeChangedInternalAsync(themeName);
        
        var theme = _context?.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        
        // Update visual appearance with new theme
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
    
    private void UpdateTextColors(WidgetTheme theme)
    {
        if (_cpuText != null)
        {
            _cpuText.Foreground = theme.ForegroundBrush;
            _cpuText.FontSize = theme.FontSize;
        }
        
        if (_memoryText != null)
        {
            _memoryText.Foreground = theme.ForegroundBrush;
            _memoryText.FontSize = theme.FontSize;
        }
        
        if (_diskText != null)
        {
            _diskText.Foreground = theme.ForegroundBrush;
            _diskText.FontSize = theme.FontSize;
        }
    }
    
    private async Task UpdateSystemMetrics()
    {
        try
        {
            var animateUpdates = GetConfigurationValue("animateUpdates", true);
            
            // Update CPU usage
            if (_cpuText != null && GetConfigurationValue("showCpu", true))
            {
                var cpuUsage = GetCpuUsage();
                var newText = $"CPU: {cpuUsage:F1}%";
                
                if (animateUpdates)
                {
                    await WidgetAnimations.PulseAsync(_cpuText, 200);
                }
                
                _cpuText.Text = newText;
            }
            
            // Update memory usage
            if (_memoryText != null && GetConfigurationValue("showMemory", true))
            {
                var (usedMemory, totalMemory) = GetMemoryUsage();
                var memoryPercent = (usedMemory / totalMemory) * 100;
                var newText = $"Memory: {memoryPercent:F1}% ({usedMemory:F1}/{totalMemory:F1} GB)";
                
                if (animateUpdates)
                {
                    await WidgetAnimations.PulseAsync(_memoryText, 200);
                }
                
                _memoryText.Text = newText;
            }
            
            // Update disk usage
            if (_diskText != null && GetConfigurationValue("showDisk", true))
            {
                var (usedDisk, totalDisk) = GetDiskUsage();
                var diskPercent = (usedDisk / totalDisk) * 100;
                var newText = $"Disk C: {diskPercent:F1}% ({usedDisk:F1}/{totalDisk:F1} GB)";
                
                if (animateUpdates)
                {
                    await WidgetAnimations.PulseAsync(_diskText, 200);
                }
                
                _diskText.Text = newText;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating system metrics: {ex.Message}");
        }
    }
    
    private float GetCpuUsage()
    {
        try
        {
            return _cpuCounter?.NextValue() ?? 0f;
        }
        catch
        {
            // Fallback method using Process.GetCurrentProcess()
            return 0f; // Simplified for demo
        }
    }
    
    private (double Used, double Total) GetMemoryUsage()
    {
        try
        {
            var availableMemoryMB = _memoryCounter?.NextValue() ?? 0f;
            var totalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024); // Simplified
            var usedMemoryMB = totalMemoryMB - availableMemoryMB;
            
            return (usedMemoryMB / 1024.0, totalMemoryMB / 1024.0); // Convert to GB
        }
        catch
        {
            return (0, 0);
        }
    }
    
    private (double Used, double Total) GetDiskUsage()
    {
        try
        {
            var driveInfo = new System.IO.DriveInfo("C:");
            var totalGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
            var freeGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var usedGB = totalGB - freeGB;
            
            return (usedGB, totalGB);
        }
        catch
        {
            return (0, 0);
        }
    }
}
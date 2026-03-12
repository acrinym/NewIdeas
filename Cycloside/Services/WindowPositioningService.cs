using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Cycloside.Models;
using System;
using System.Linq;

namespace Cycloside.Services;

/// <summary>
/// Service for applying window positions from startup configuration
/// </summary>
public class WindowPositioningService
{
    private static WindowPositioningService? _instance;
    private StartupConfiguration? _configuration;

    public static WindowPositioningService Instance => _instance ??= new WindowPositioningService();

    private WindowPositioningService() { }

    /// <summary>
    /// Initialize the service with startup configuration
    /// </summary>
    public void Initialize(StartupConfiguration? configuration)
    {
        _configuration = configuration;
        Logger.Log($"🎯 WindowPositioningService initialized with {_configuration?.PluginConfigs.Count ?? 0} plugin configs");
    }

    /// <summary>
    /// Apply saved window position to a plugin window
    /// </summary>
    /// <param name="window">The window to position</param>
    /// <param name="pluginName">Name of the plugin that owns this window</param>
    public void ApplyPosition(Window window, string pluginName)
    {
        if (_configuration == null)
        {
            Logger.Log($"⚠️ WindowPositioningService: No configuration available for {pluginName}");
            return;
        }

        var position = _configuration.GetPluginPosition(pluginName);
        if (position == null)
        {
            Logger.Log($"📍 No saved position for {pluginName}, using default");
            return;
        }

        try
        {
            ApplyWindowPosition(window, position);
            Logger.Log($"✅ Applied saved position to {pluginName}: {position.Preset} on Monitor {position.MonitorIndex}");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to apply position to {pluginName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply window position based on configuration
    /// </summary>
    private void ApplyWindowPosition(Window window, WindowStartupPosition position)
    {
        // Get screen information
        var screens = window.Screens.All.ToList();
        if (screens.Count == 0)
        {
            Logger.Log("⚠️ No screens detected, cannot apply position");
            return;
        }

        // Select target screen
        var screen = position.MonitorIndex >= 0 && position.MonitorIndex < screens.Count
            ? screens[position.MonitorIndex]
            : screens[0]; // Fallback to primary

        var workingArea = screen.WorkingArea;

        // Apply size if specified
        if (position.Width.HasValue && position.Height.HasValue)
        {
            window.Width = position.Width.Value;
            window.Height = position.Height.Value;
        }

        // Calculate position based on preset or custom coordinates
        PixelPoint targetPosition;

        if (position.Preset == WindowPositionPreset.Custom && position.X.HasValue && position.Y.HasValue)
        {
            // Use custom coordinates relative to the monitor
            targetPosition = new PixelPoint(
                workingArea.X + position.X.Value,
                workingArea.Y + position.Y.Value
            );
        }
        else
        {
            // Calculate position based on preset
            targetPosition = CalculatePresetPosition(window, workingArea, position.Preset);
        }

        // Apply the position
        window.Position = targetPosition;
    }

    /// <summary>
    /// Calculate window position based on preset
    /// </summary>
    private PixelPoint CalculatePresetPosition(Window window, PixelRect workingArea, WindowPositionPreset preset)
    {
        // Get window size (use default if not set yet)
        int windowWidth = window.Width > 0 ? (int)window.Width : 400;
        int windowHeight = window.Height > 0 ? (int)window.Height : 300;

        int x, y;
        const int edgeMargin = 20; // Margin from screen edges

        switch (preset)
        {
            case WindowPositionPreset.Center:
                x = workingArea.X + (workingArea.Width - windowWidth) / 2;
                y = workingArea.Y + (workingArea.Height - windowHeight) / 2;
                break;

            case WindowPositionPreset.TopLeft:
                x = workingArea.X + edgeMargin;
                y = workingArea.Y + edgeMargin;
                break;

            case WindowPositionPreset.TopRight:
                x = workingArea.X + workingArea.Width - windowWidth - edgeMargin;
                y = workingArea.Y + edgeMargin;
                break;

            case WindowPositionPreset.BottomLeft:
                x = workingArea.X + edgeMargin;
                y = workingArea.Y + workingArea.Height - windowHeight - edgeMargin;
                break;

            case WindowPositionPreset.BottomRight:
                x = workingArea.X + workingArea.Width - windowWidth - edgeMargin;
                y = workingArea.Y + workingArea.Height - windowHeight - edgeMargin;
                break;

            case WindowPositionPreset.LeftEdge:
                x = workingArea.X + edgeMargin;
                y = workingArea.Y + (workingArea.Height - windowHeight) / 2;
                break;

            case WindowPositionPreset.RightEdge:
                x = workingArea.X + workingArea.Width - windowWidth - edgeMargin;
                y = workingArea.Y + (workingArea.Height - windowHeight) / 2;
                break;

            case WindowPositionPreset.TopEdge:
                x = workingArea.X + (workingArea.Width - windowWidth) / 2;
                y = workingArea.Y + edgeMargin;
                break;

            case WindowPositionPreset.BottomEdge:
                x = workingArea.X + (workingArea.Width - windowWidth) / 2;
                y = workingArea.Y + workingArea.Height - windowHeight - edgeMargin;
                break;

            default:
                // Fallback to center
                x = workingArea.X + (workingArea.Width - windowWidth) / 2;
                y = workingArea.Y + (workingArea.Height - windowHeight) / 2;
                break;
        }

        return new PixelPoint(x, y);
    }

    /// <summary>
    /// Get configuration for a specific plugin
    /// </summary>
    public WindowStartupPosition? GetPosition(string pluginName)
    {
        return _configuration?.GetPluginPosition(pluginName);
    }

    /// <summary>
    /// Check if a position is configured for a plugin
    /// </summary>
    public bool HasPosition(string pluginName)
    {
        return _configuration?.GetPluginPosition(pluginName) != null;
    }
}

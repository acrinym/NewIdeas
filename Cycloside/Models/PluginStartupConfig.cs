using System;
using System.Collections.Generic;

namespace Cycloside.Models;

/// <summary>
/// Configuration for how a plugin should behave on startup
/// </summary>
public class PluginStartupConfig
{
    /// <summary>Plugin name (must match IPlugin.Name)</summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>Whether this plugin should auto-start</summary>
    public bool EnableOnStartup { get; set; } = true;

    /// <summary>Window position when plugin starts (null = default)</summary>
    public WindowStartupPosition? Position { get; set; }
}

/// <summary>
/// Defines where a plugin window should appear on startup
/// </summary>
public class WindowStartupPosition
{
    /// <summary>Monitor index (0 = primary, 1 = secondary, etc.)</summary>
    public int MonitorIndex { get; set; } = 0;

    /// <summary>X position relative to monitor (null = center)</summary>
    public int? X { get; set; }

    /// <summary>Y position relative to monitor (null = center)</summary>
    public int? Y { get; set; }

    /// <summary>Window width (null = plugin default)</summary>
    public int? Width { get; set; }

    /// <summary>Window height (null = plugin default)</summary>
    public int? Height { get; set; }

    /// <summary>Predefined position preset</summary>
    public WindowPositionPreset Preset { get; set; } = WindowPositionPreset.Center;
}

/// <summary>
/// Common window position presets for easy configuration
/// </summary>
public enum WindowPositionPreset
{
    /// <summary>Center of monitor</summary>
    Center,

    /// <summary>Top-left corner</summary>
    TopLeft,

    /// <summary>Top-right corner</summary>
    TopRight,

    /// <summary>Bottom-left corner</summary>
    BottomLeft,

    /// <summary>Bottom-right corner</summary>
    BottomRight,

    /// <summary>Left edge (vertically centered)</summary>
    LeftEdge,

    /// <summary>Right edge (vertically centered)</summary>
    RightEdge,

    /// <summary>Top edge (horizontally centered)</summary>
    TopEdge,

    /// <summary>Bottom edge (horizontally centered)</summary>
    BottomEdge,

    /// <summary>Use custom X/Y coordinates</summary>
    Custom
}

/// <summary>
/// Complete startup configuration for all plugins
/// </summary>
public class StartupConfiguration
{
    /// <summary>Whether first-launch setup has been completed</summary>
    public bool HasCompletedFirstLaunch { get; set; } = false;

    /// <summary>Plugin startup configurations</summary>
    public List<PluginStartupConfig> PluginConfigs { get; set; } = new();

    /// <summary>
    /// Gets or creates configuration for a plugin
    /// </summary>
    public PluginStartupConfig GetOrCreateConfig(string pluginName)
    {
        var existing = PluginConfigs.Find(c => c.PluginName == pluginName);
        if (existing != null) return existing;

        var newConfig = new PluginStartupConfig { PluginName = pluginName };
        PluginConfigs.Add(newConfig);
        return newConfig;
    }

    /// <summary>
    /// Checks if a plugin is enabled for startup
    /// </summary>
    public bool IsPluginEnabled(string pluginName)
    {
        var config = PluginConfigs.Find(c => c.PluginName == pluginName);
        return config?.EnableOnStartup ?? false;
    }

    /// <summary>
    /// Sets whether a plugin is enabled for startup
    /// </summary>
    public void SetPluginEnabled(string pluginName, bool enabled)
    {
        var config = GetOrCreateConfig(pluginName);
        config.EnableOnStartup = enabled;
    }

    /// <summary>
    /// Gets the startup position for a plugin
    /// </summary>
    public WindowStartupPosition? GetPluginPosition(string pluginName)
    {
        var config = PluginConfigs.Find(c => c.PluginName == pluginName);
        return config?.Position;
    }

    /// <summary>
    /// Sets the startup position for a plugin
    /// </summary>
    public void SetPluginPosition(string pluginName, WindowStartupPosition position)
    {
        var config = GetOrCreateConfig(pluginName);
        config.Position = position;
    }
}

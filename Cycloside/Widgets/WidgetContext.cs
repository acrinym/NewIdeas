using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// Context information provided to IWidgetV2 widgets
/// </summary>
public class WidgetContext
{
    /// <summary>
    /// Theme manager for accessing themes
    /// </summary>
    public WidgetThemeManager ThemeManager { get; set; } = null!;

    /// <summary>
    /// Widget configuration data
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Unique instance identifier
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the host view model
    /// </summary>
    public EnhancedWidgetHostViewModel HostViewModel { get; set; } = null!;

    /// <summary>
    /// Current theme
    /// </summary>
    public WidgetTheme CurrentTheme => ThemeManager.GetCurrentTheme();
    
    /// <summary>
    /// Current theme (alias for CurrentTheme)
    /// </summary>
    public WidgetTheme Theme => CurrentTheme;
    
    /// <summary>
    /// Event publisher for widget events
    /// </summary>
    public Action<string, object?> EventPublisher { get; set; } = (eventName, data) => { };
    
    /// <summary>
    /// Command executor for widget commands
    /// </summary>
    public Func<string, Task> CommandExecutor { get; set; } = async (command) => await Task.CompletedTask;

    /// <summary>
    /// Get configuration value with default fallback
    /// </summary>
    public T GetConfigValue<T>(string key, T defaultValue = default!)
    {
        if (Configuration.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Set configuration value
    /// </summary>
    public void SetConfigValue<T>(string key, T value)
    {
        if (value != null)
        {
            Configuration[key] = value;
        }
        else
        {
            Configuration.Remove(key);
        }
    }

    /// <summary>
    /// Check if configuration contains a key
    /// </summary>
    public bool HasConfigValue(string key)
    {
        return Configuration.ContainsKey(key);
    }

    /// <summary>
    /// Remove configuration value
    /// </summary>
    public bool RemoveConfigValue(string key)
    {
        return Configuration.Remove(key);
    }

    /// <summary>
    /// Clear all configuration
    /// </summary>
    public void ClearConfiguration()
    {
        Configuration.Clear();
    }
}
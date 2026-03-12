using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// Enhanced widget interface with lifecycle methods, configuration support, and better extensibility
/// </summary>
public interface IWidgetV2 : IWidget
{
    /// <summary>
    /// Widget version for compatibility checking
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Widget description for UI display
    /// </summary>
    new string Description { get; }

    /// <summary>
    /// Widget category for organization
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Widget icon path or resource key
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Whether this widget supports multiple instances
    /// </summary>
    bool SupportsMultipleInstances { get; }

    /// <summary>
    /// Default size for the widget
    /// </summary>
    (double Width, double Height) DefaultSize { get; }

    /// <summary>
    /// Minimum size constraints
    /// </summary>
    (double Width, double Height) MinimumSize { get; }

    /// <summary>
    /// Maximum size constraints (use double.PositiveInfinity for unlimited)
    /// </summary>
    (double Width, double Height) MaximumSize { get; }

    /// <summary>
    /// Whether the widget can be resized
    /// </summary>
    bool IsResizable { get; }

    /// <summary>
    /// Configuration schema for the widget settings
    /// </summary>
    WidgetConfigurationSchema ConfigurationSchema { get; }

    /// <summary>
    /// Called when the widget is first created
    /// </summary>
    Task OnInitializeAsync(WidgetContext context);

    /// <summary>
    /// Called when the widget is activated/shown
    /// </summary>
    Task OnActivateAsync();

    /// <summary>
    /// Called when the widget is deactivated/hidden
    /// </summary>
    Task OnDeactivateAsync();

    /// <summary>
    /// Called when the widget is being destroyed
    /// </summary>
    Task OnDestroyAsync();

    /// <summary>
    /// Called when widget configuration changes
    /// </summary>
    Task OnConfigurationChangedAsync(Dictionary<string, object> configuration);

    /// <summary>
    /// Called when widget theme/skin changes
    /// </summary>
    Task OnThemeChangedAsync(string themeName);

    /// <summary>
    /// Build the widget view with current configuration
    /// </summary>
    Control BuildView(WidgetContext context);

    /// <summary>
    /// Get the configuration UI for this widget
    /// </summary>
    Control? GetConfigurationView(WidgetContext context);

    /// <summary>
    /// Validate configuration values
    /// </summary>
    WidgetValidationResult ValidateConfiguration(Dictionary<string, object> configuration);

    /// <summary>
    /// Export widget data for backup/sharing
    /// </summary>
    Task<Dictionary<string, object>> ExportDataAsync();

    /// <summary>
    /// Import widget data from backup/sharing
    /// </summary>
    Task ImportDataAsync(Dictionary<string, object> data);

    /// <summary>
    /// Handle widget-specific commands
    /// </summary>
    Task<object?> HandleCommandAsync(string command, Dictionary<string, object>? parameters = null);
}



/// <summary>
/// Configuration schema for widget settings
/// </summary>
public class WidgetConfigurationSchema
{
    public List<WidgetConfigurationProperty> Properties { get; set; } = new();
    public Dictionary<string, object> DefaultValues { get; set; } = new();
}

/// <summary>
/// Individual configuration property definition
/// </summary>
public class WidgetConfigurationProperty
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required WidgetPropertyType Type { get; set; }
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public Dictionary<string, object>? ValidationRules { get; set; }
    public List<WidgetPropertyOption>? Options { get; set; } // For dropdown/radio types
}

/// <summary>
/// Property type enumeration
/// </summary>
public enum WidgetPropertyType
{
    String,
    Integer,
    Double,
    Boolean,
    Color,
    Font,
    File,
    Directory,
    Dropdown,
    MultiSelect,
    Slider,
    DatePicker,
    TimePicker
}

/// <summary>
/// Option for dropdown/radio properties
/// </summary>
public class WidgetPropertyOption
{
    public required string Value { get; set; }
    public required string DisplayText { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Validation result for widget configuration
/// </summary>
public class WidgetValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
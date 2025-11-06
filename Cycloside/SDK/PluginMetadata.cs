namespace Cycloside.Plugins;

/// <summary>
/// Extended metadata interface for plugins that want to control
/// their default behavior and categorization.
/// </summary>
public interface IPluginMetadata
{
    /// <summary>
    /// Plugin category for organization and default behavior.
    /// Core categories (DesktopCustomization, RetroComputing) are always enabled.
    /// </summary>
    PluginCategory Category { get; }

    /// <summary>
    /// Whether this plugin should be enabled on first launch.
    /// After first launch, user preferences take precedence.
    /// </summary>
    bool EnabledByDefault { get; }

    /// <summary>
    /// Whether this is a core plugin that defines the platform experience.
    /// Core plugins are prominently featured in UI.
    /// </summary>
    bool IsCore { get; }
}

/// <summary>
/// Helper to determine plugin default state based on category.
/// </summary>
public static class PluginDefaults
{
    /// <summary>
    /// Gets whether plugins in this category should be enabled by default
    /// on first launch.
    /// </summary>
    public static bool IsEnabledByDefault(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => true,  // Always enabled
            PluginCategory.RetroComputing => true,        // Always enabled
            PluginCategory.TinkererTools => true,         // Enabled by default
            PluginCategory.Utilities => true,             // Enabled by default
            PluginCategory.Entertainment => true,         // Enabled by default
            PluginCategory.Development => false,          // User must enable
            PluginCategory.Security => false,             // User must enable
            PluginCategory.Experimental => false,         // User must enable
            _ => false
        };
    }

    /// <summary>
    /// Gets whether plugins in this category are considered "core" features.
    /// </summary>
    public static bool IsCore(PluginCategory category)
    {
        return category is PluginCategory.DesktopCustomization
                        or PluginCategory.RetroComputing;
    }
}

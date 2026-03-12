namespace Cycloside.Plugins;

/// <summary>
/// Optional metadata that lets plugins opt into category-aware UI and defaults.
/// </summary>
public interface IPluginMetadata
{
    /// <summary>
    /// Stable key used for first-run configuration and other persisted plugin preferences.
    /// If omitted, the host falls back to the plugin implementation type name.
    /// </summary>
    string? PluginId => null;

    /// <summary>
    /// Category used for grouping in plugin UIs.
    /// </summary>
    PluginCategory Category => PluginCategory.Experimental;

    /// <summary>
    /// Whether the plugin should be enabled by default on first launch.
    /// </summary>
    bool EnabledByDefault => PluginDefaults.IsEnabledByDefault(Category);

    /// <summary>
    /// Whether the plugin is part of the core shell experience.
    /// </summary>
    bool IsCore => PluginDefaults.IsCore(Category);
}

/// <summary>
/// Default behaviors attached to plugin categories.
/// </summary>
public static class PluginDefaults
{
    public static bool IsEnabledByDefault(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => true,
            PluginCategory.RetroComputing => true,
            PluginCategory.TinkererTools => true,
            PluginCategory.Utilities => true,
            PluginCategory.Entertainment => true,
            PluginCategory.Development => false,
            PluginCategory.Security => false,
            PluginCategory.Experimental => false,
            _ => false
        };
    }

    public static bool IsCore(PluginCategory category)
    {
        return category is PluginCategory.DesktopCustomization or PluginCategory.RetroComputing;
    }
}

/// <summary>
/// Resolved metadata used by the host after applying built-in defaults and optional plugin overrides.
/// </summary>
public sealed class PluginMetadataInfo
{
    public PluginMetadataInfo(string pluginId, PluginCategory category, bool enabledByDefault, bool isCore)
    {
        PluginId = pluginId;
        Category = category;
        EnabledByDefault = enabledByDefault;
        IsCore = isCore;
    }

    public string PluginId { get; }
    public PluginCategory Category { get; }
    public bool EnabledByDefault { get; }
    public bool IsCore { get; }
}

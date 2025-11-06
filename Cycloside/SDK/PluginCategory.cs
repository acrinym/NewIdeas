namespace Cycloside.Plugins;

/// <summary>
/// Categories for organizing plugins by their primary purpose.
/// Used to determine default enabled state and UI grouping.
/// </summary>
public enum PluginCategory
{
    /// <summary>Core desktop customization features (always enabled)</summary>
    DesktopCustomization,

    /// <summary>Retro computing and gaming (always enabled)</summary>
    RetroComputing,

    /// <summary>Power user automation tools (enabled by default)</summary>
    TinkererTools,

    /// <summary>Basic desktop utilities (enabled by default)</summary>
    Utilities,

    /// <summary>Developer tools (disabled by default)</summary>
    Development,

    /// <summary>Security/network tools (disabled by default, consider archiving)</summary>
    Security,

    /// <summary>Entertainment and media</summary>
    Entertainment,

    /// <summary>Experimental or uncategorized</summary>
    Experimental
}

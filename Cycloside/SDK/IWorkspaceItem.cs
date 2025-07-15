using Avalonia.Controls;

namespace Cycloside.Plugins;

/// <summary>
/// Optional interface for plugins that can render their UI inside the
/// unified workspace. Implementing plugins should return a control that
/// represents their main view when docked or tabbed.
/// </summary>
public interface IWorkspaceItem
{
    /// <summary>
    /// Builds the view used when the plugin is hosted in the workspace.
    /// </summary>
    Control BuildWorkspaceView();

    /// <summary>
    /// Set by the host when the plugin is opened inside the workspace so
    /// the plugin can avoid showing its own window.
    /// </summary>
    bool UseWorkspace { get; set; }
}

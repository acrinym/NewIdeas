using System;

namespace Cycloside.Plugins;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    Cycloside.Widgets.IWidget? Widget { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin should block
    /// component-specific skins from being applied, forcing it to
    /// always use the global application theme.
    /// </summary>
    bool ForceDefaultTheme { get; }

    /// <summary>
    /// Called by the host when the plugin should create its UI and begin
    /// processing. Implementations may open windows or attach controls to
    /// the workspace.
    /// </summary>
    void Start();

    /// <summary>
    /// Called when the plugin should clean up resources and close any UI.
    /// </summary>
    void Stop();
}

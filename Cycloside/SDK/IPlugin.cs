using System;

namespace Cycloside.Plugins;

/// <summary>
/// Base contract that all Cycloside plugins implement. A plugin exposes
/// metadata along with <see cref="Start"/> and <see cref="Stop"/> lifecycle
/// methods. Implementations may also provide an optional <see cref="Widgets.IWidget"/>
/// for desktop display.
/// </summary>
public interface IPlugin
{
    /// <summary>Human friendly name displayed in menus.</summary>
    string Name { get; }

    /// <summary>Description shown in the plugin manager.</summary>
    string Description { get; }

    /// <summary>Semantic version of the plugin.</summary>
    Version Version { get; }

    /// <summary>Optional widget instance hosted by the widget system.</summary>
    Cycloside.Widgets.IWidget? Widget { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin should block
    /// component-specific skins from being applied, forcing it to
    /// always use the global application theme.
    /// </summary>
    bool ForceDefaultTheme { get; }

    /// <summary>Called when the plugin is activated by the host.</summary>
    void Start();

    /// <summary>Called when the plugin is deactivated or the application shuts down.</summary>
    void Stop();
}

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

    void Start();
    void Stop();
}

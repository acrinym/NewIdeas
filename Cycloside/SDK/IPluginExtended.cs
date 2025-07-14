using System;

namespace Cycloside.Plugins;

public interface IPluginExtended : IPlugin
{
    /// <summary>
    /// Invoked after the application's settings file has been written.
    /// Plugins can persist state to their own storage at this point.
    /// </summary>
    void OnSettingsSaved();

    /// <summary>
    /// Allows the plugin to respond to an unhandled exception thrown from
    /// its <see cref="IPlugin.Start"/> or other methods. Returning normally
    /// should keep the host running.
    /// </summary>
    void OnCrash(Exception ex);
}

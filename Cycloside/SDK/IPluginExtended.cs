using System;

namespace Cycloside.Plugins;

/// <summary>
/// Optional extension interface for plugins that want notifications about
/// global events such as settings changes or crash handling.
/// </summary>
public interface IPluginExtended : IPlugin
{
    /// <summary>
    /// Called after <see cref="SettingsManager.Save"/> completes so the plugin
    /// can refresh any cached options.
    /// </summary>
    void OnSettingsSaved();

    /// <summary>
    /// Invoked when the plugin throws during <see cref="IPlugin.Start"/> or
    /// <see cref="IPlugin.Stop"/>. Use this to clean up persistent state.
    /// </summary>
    void OnCrash(Exception ex);
}

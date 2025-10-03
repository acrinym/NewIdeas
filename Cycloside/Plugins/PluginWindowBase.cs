using Avalonia.Controls;
using Cycloside.Services;
using System;

namespace Cycloside.Plugins
{
    /// <summary>
    /// Base class for plugin windows that automatically handles theme and skin cleanup.
    /// Inherit from this class to ensure proper resource management for plugin windows.
    /// </summary>
    public class PluginWindowBase : Window
    {
        /// <summary>
        /// Gets or sets the plugin associated with this window.
        /// </summary>
        public IPlugin? Plugin { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindowBase"/> class.
        /// </summary>
        public PluginWindowBase()
        {
            // Subscribe to the Closed event to clean up resources
            Closed += PluginWindowBase_Closed;
        }

        /// <summary>
        /// Applies the appropriate theme and skin for the plugin.
        /// </summary>
        /// <param name="plugin">The plugin associated with this window.</param>
        public void ApplyPluginThemeAndSkin(IPlugin plugin)
        {
            Plugin = plugin;
            ThemeManager.ApplyForPlugin(this, plugin);
        }

        /// <summary>
        /// Handles the Closed event to clean up resources.
        /// </summary>
        private void PluginWindowBase_Closed(object? sender, EventArgs e)
        {
            // Clean up themes and skins to prevent memory leaks
            ThemeManager.RemoveComponentThemes(this);
            SkinManager.RemoveAllSkinsFrom(this);
            
            // Unsubscribe from the event to prevent memory leaks
            Closed -= PluginWindowBase_Closed;
            
            Logger.Log($"Cleaned up resources for plugin window: {GetType().Name}");
        }
    }
}
using Avalonia.Controls;
using Cycloside.Services;
using System;
using System.Linq;

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

            // Subscribe to global theme/skin changes
            Services.ThemeManager.ThemeChanged += OnGlobalThemeChanged;
            Services.SkinManager.SkinChanged += OnGlobalSkinChanged;
        }

        /// <summary>
        /// Applies the appropriate theme and skin for the plugin.
        /// </summary>
        /// <param name="plugin">The plugin associated with this window.</param>
        public void ApplyPluginThemeAndSkin(IPlugin plugin)
        {
            Plugin = plugin;
            ThemeManager.ApplyForPlugin(this, plugin);

            // Apply plugin-specific theming if supported
            if (plugin is IThemablePlugin themablePlugin && themablePlugin.ParticipateInAutomaticTheming)
            {
                ApplyThemablePluginHooks(themablePlugin);
            }
        }

        /// <summary>
        /// Applies theming hooks for plugins that implement IThemablePlugin
        /// </summary>
        private void ApplyThemablePluginHooks(IThemablePlugin themablePlugin)
        {
            try
            {
                // Apply theme classes for skin targeting
                var themeClasses = themablePlugin.ThemeClasses.ToList();
                if (themeClasses.Any())
                {
                    Classes.AddRange(themeClasses);
                    Logger.Log($"Applied theme classes to plugin window: {string.Join(", ", themeClasses)}");
                }

                // Apply current theme/skin state
                themablePlugin.OnGlobalThemeChanged(Services.ThemeManager.CurrentTheme, Services.ThemeManager.CurrentVariant.ToString());
                themablePlugin.OnGlobalSkinChanged(Services.SkinManager.CurrentSkin);

                Logger.Log($"Applied theming hooks to plugin: {themablePlugin.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying theming hooks to plugin '{themablePlugin.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Handles global theme changes
        /// </summary>
        private void OnGlobalThemeChanged(object? sender, Services.ThemeChangedEventArgs e)
        {
            if (Plugin is IThemablePlugin themablePlugin && themablePlugin.ParticipateInAutomaticTheming)
            {
                try
                {
                    themablePlugin.OnGlobalThemeChanged(e.ThemeName, e.Variant.ToString());

                    // Reapply theme classes if they changed
                    var currentClasses = Classes.ToList();
                    var expectedClasses = themablePlugin.ThemeClasses.ToList();

                    var classesToRemove = currentClasses.Except(expectedClasses).ToList();
                    var classesToAdd = expectedClasses.Except(currentClasses).ToList();

                    foreach (var cls in classesToRemove)
                        Classes.Remove(cls);

                    Classes.AddRange(classesToAdd);

                    Logger.Log($"Updated theme-dependent classes for plugin: {themablePlugin.Name}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error in plugin theme change handler: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles global skin changes
        /// </summary>
        private void OnGlobalSkinChanged(object? sender, Services.SkinChangedEventArgs e)
        {
            if (Plugin is IThemablePlugin themablePlugin && themablePlugin.ParticipateInAutomaticTheming)
            {
                try
                {
                    themablePlugin.OnGlobalSkinChanged(e.SkinName);
                    Logger.Log($"Applied skin change to plugin: {themablePlugin.Name}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error in plugin skin change handler: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the Closed event to clean up resources.
        /// </summary>
        private void PluginWindowBase_Closed(object? sender, EventArgs e)
        {
            // Clean up themes and skins to prevent memory leaks
            ThemeManager.RemoveComponentThemes(this);
            SkinManager.RemoveAllSkinsFrom(this);

            // Unsubscribe from events
            Services.ThemeManager.ThemeChanged -= OnGlobalThemeChanged;
            Services.SkinManager.SkinChanged -= OnGlobalSkinChanged;
            Closed -= PluginWindowBase_Closed;

            Logger.Log($"Cleaned up resources for plugin window: {GetType().Name}");
        }
    }
}
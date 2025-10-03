using Avalonia.Controls;
using System.Collections.Generic;

namespace Cycloside.Plugins
{
    /// <summary>
    /// Interface for plugins that can be themed and skinned dynamically.
    /// Implement this interface to provide custom theming hooks for plugin windows.
    /// </summary>
    public interface IThemablePlugin : IPlugin
    {
        /// <summary>
        /// Gets the available skin names for this plugin.
        /// Return empty collection if no custom skins are supported.
        /// </summary>
        IEnumerable<string> GetAvailableSkins();

        /// <summary>
        /// Gets the available theme names for this plugin.
        /// Return empty collection if no custom themes are supported.
        /// </summary>
        IEnumerable<string> GetAvailableThemes();

        /// <summary>
        /// Applies a skin to the plugin's elements.
        /// </summary>
        /// <param name="skinName">The name of the skin to apply</param>
        /// <param name="elements">The UI elements to apply the skin to</param>
        void ApplySkin(string skinName, params Control[] elements);

        /// <summary>
        /// Applies a theme to the plugin's elements.
        /// </summary>
        /// <param name="themeName">The name of the theme to apply</param>
        /// <param name="elements">The UI elements to apply the theme to</param>
        void ApplyTheme(string themeName, params Control[] elements);

        /// <summary>
        /// Called when the global theme changes.
        /// Allows the plugin to respond to application-wide theme changes.
        /// </summary>
        /// <param name="themeName">The new global theme name</param>
        /// <param name="variant">The new theme variant (Light/Dark)</param>
        void OnGlobalThemeChanged(string themeName, string variant);

        /// <summary>
        /// Called when the global skin changes.
        /// Allows the plugin to respond to application-wide skin changes.
        /// </summary>
        /// <param name="skinName">The new global skin name</param>
        void OnGlobalSkinChanged(string? skinName);

        /// <summary>
        /// Determines if the plugin should participate in automatic theme/skin application.
        /// Return false if the plugin manages its own theming entirely.
        /// </summary>
        bool ParticipateInAutomaticTheming { get; }

        /// <summary>
        /// Gets CSS-like classes that should be applied to the plugin's main window for skin targeting.
        /// </summary>
        IEnumerable<string> ThemeClasses { get; }
    }

    /// <summary>
    /// Interface for widgets that can be themed and skinned.
    /// Widgets are lighter-weight than plugins but still support theming integration.
    /// </summary>
    public interface IThemableWidget
    {
        /// <summary>
        /// Gets the widget's identifier for skin targeting.
        /// </summary>
        string WidgetId { get; }

        /// <summary>
        /// Applies semantic tokens to the widget.
        /// Called when the global theme changes.
        /// </summary>
        void ApplySemanticTokens();

        /// <summary>
        /// Gets CSS-like classes for skin targeting.
        /// </summary>
        IEnumerable<string> GetThemeClasses();

        /// <summary>
        /// Determines if the widget should auto-refresh when themes change.
        /// </summary>
        bool AutoRefreshOnThemeChange { get; }
    }
}

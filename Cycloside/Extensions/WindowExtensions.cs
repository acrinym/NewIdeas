using Avalonia.Controls;
using Cycloside.Controls;
using Cycloside.Helpers;
using System;

namespace Cycloside.Extensions
{
    /// <summary>
    /// Extension methods for Window class to simplify decoration integration.
    /// </summary>
    public static class WindowExtensions
    {
        /// <summary>
        /// Converts a regular Window to a DecoratedWindow, transferring all properties and content.
        /// Returns the new decorated window without showing it.
        /// </summary>
        /// <param name="window">The window to convert</param>
        /// <returns>New DecoratedWindow with same content and properties</returns>
        public static DecoratedWindow ToDecoratedWindow(this Window window)
        {
            return WindowDecorationHelper.WrapExistingWindow(window);
        }

        /// <summary>
        /// Replaces this window with a decorated version and shows it.
        /// This method closes the current window.
        /// </summary>
        /// <param name="window">The window to replace</param>
        /// <param name="onClosed">Optional callback when new window closes</param>
        /// <returns>The new DecoratedWindow (already shown)</returns>
        public static DecoratedWindow ReplaceWithDecorated(
            this Window window,
            Action? onClosed = null)
        {
            return WindowDecorationHelper.ReplaceWithDecoratedWindow(window, onClosed);
        }

        /// <summary>
        /// Checks if this window should be themed based on its title.
        /// </summary>
        /// <param name="window">Window to check</param>
        /// <returns>True if window should be themed</returns>
        public static bool ShouldBeThemed(this Window window)
        {
            return WindowDecorationHelper.ShouldThemeWindow(window.Title ?? "");
        }

        /// <summary>
        /// Applies standard Cycloside window settings including positioning and effects.
        /// </summary>
        /// <param name="window">Window to configure</param>
        /// <param name="pluginName">Plugin name for configuration lookup</param>
        /// <returns>The same window for method chaining</returns>
        public static TWindow WithStandardSettings<TWindow>(
            this TWindow window,
            string pluginName) where TWindow : Window
        {
            WindowDecorationHelper.ApplyStandardWindowSettings(window, pluginName);
            return window;
        }

        /// <summary>
        /// Shows the window and logs it.
        /// </summary>
        /// <param name="window">Window to show</param>
        /// <param name="pluginName">Plugin name for logging</param>
        /// <returns>The same window for method chaining</returns>
        public static TWindow ShowWithLogging<TWindow>(
            this TWindow window,
            string pluginName) where TWindow : Window
        {
            Logger.Log($"📖 {pluginName}: Opening window '{window.Title}'");
            window.Show();
            return window;
        }

        /// <summary>
        /// Fluent API for creating and configuring a DecoratedWindow.
        /// </summary>
        /// <param name="window">DecoratedWindow to configure</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Configured window for method chaining</returns>
        public static DecoratedWindow Configure(
            this DecoratedWindow window,
            Action<DecoratedWindow> configure)
        {
            configure(window);
            return window;
        }

        /// <summary>
        /// Sets window content with automatic padding.
        /// </summary>
        /// <param name="window">Window to configure</param>
        /// <param name="content">Content control</param>
        /// <param name="padding">Padding around content (default: 16)</param>
        /// <returns>Window for method chaining</returns>
        public static TWindow WithPaddedContent<TWindow>(
            this TWindow window,
            Control content,
            double padding = 16) where TWindow : Window
        {
            window.Content = WindowDecorationHelper.CreateContentPanel(content, padding);
            return window;
        }
    }
}

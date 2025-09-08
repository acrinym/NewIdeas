using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cycloside.Plugins;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages the application of global and component-specific themes.
    /// This has been rewritten to be more robust and correct.
    /// </summary>
    public static class ThemeManager
    {
        // All themes are stored under the 'Themes/Global' subdirectory. The
        // editor and setup wizard already point there, but the ThemeManager
        // previously used the parent directory which caused missing theme
        // errors at runtime.
        private static string ThemeDir => Path.Combine(AppContext.BaseDirectory, "Themes", "Global");

        /// <summary>
        /// Applies the application-wide global theme from settings.
        /// Falls back to a default theme if the setting is invalid.
        /// </summary>
        public static void LoadGlobalThemeFromSettings()
        {
            var themeName = SettingsManager.Settings.GlobalTheme;

            if (string.IsNullOrWhiteSpace(themeName))
            {
                themeName = "MintGreen"; // A safe default
            }

            if (!LoadGlobalTheme(themeName) && themeName != "MintGreen")
            {
                LoadGlobalTheme("MintGreen"); // Fallback on failure
            }
        }

        /// <summary>
        /// Applies a single global theme to the entire application.
        /// It clears any previously loaded global theme first.
        /// </summary>
        public static bool LoadGlobalTheme(string themeName)
        {
            if (Application.Current == null)
            {
                Logger.Log("ThemeManager: Application.Current is null, cannot load theme");
                return false;
            }

            var file = Path.Combine(ThemeDir, $"{themeName}.axaml");
            if (!File.Exists(file))
            {
                Logger.Log($"Global theme '{themeName}' not found at '{file}'.");
                // FIXED: Try to create a default theme if none exists
                if (themeName != "MintGreen")
                {
                    Logger.Log("Attempting to load fallback theme 'MintGreen'");
                    return LoadGlobalTheme("MintGreen");
                }
                return false;
            }

            // Remove any existing global theme to prevent conflicts
            var existing = Application.Current.Styles.OfType<StyleInclude>()
                .FirstOrDefault(s => s.Source?.OriginalString.Contains("Themes/Global") == true);
            if (existing != null)
            {
                Application.Current.Styles.Remove(existing);
                Logger.Log($"Removed existing theme: {existing.Source}");
            }

            try
            {
                // FIXED: Use proper file URI construction for theme files
                var fileUri = new Uri(file);
                var newThemeStyle = new StyleInclude(fileUri);
                Application.Current.Styles.Add(newThemeStyle);
                SettingsManager.Settings.GlobalTheme = themeName;
                SettingsManager.Save();
                Logger.Log($"Successfully loaded theme '{themeName}' from '{file}'");
                
                // FIXED: Force refresh of all existing windows
                RefreshAllWindows();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load theme '{themeName}': {ex.Message}");
                Logger.Log($"Theme file path: {file}");
                Logger.Log($"Theme directory exists: {Directory.Exists(ThemeDir)}");
                Logger.Log($"Theme file exists: {File.Exists(file)}");
                Logger.Log($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Logger.Log($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Forces all existing windows to refresh their styling.
        /// This ensures theme changes apply immediately without restart.
        /// </summary>
        private static void RefreshAllWindows()
        {
            try
            {
                // Get all open windows and force them to refresh
                var windows = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.Windows.ToList()
                    : new List<Window>();

                foreach (var window in windows)
                {
                    // Force the window to invalidate its visual tree
                    window.InvalidateVisual();
                    
                    // Re-apply component themes if they exist
                    var windowName = window.GetType().Name;
                    ApplyComponentTheme(window, windowName);
                    
                    Logger.Log($"Refreshed styling for window: {windowName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error refreshing windows: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the global theme and any component-specific themes
        /// based on the current settings. This is the main entry point for plugins.
        /// </summary>
        public static void ApplyFromSettings(Window window, string componentName)
        {
            // The global theme is loaded once at startup, so we just need to apply component themes here.
            ApplyComponentTheme(window, componentName);
        }

        /// <summary>
        /// Applies themes for a plugin window. This first applies the generic
        /// "Plugins" theme, then the plugin specific theme using the plugin's
        /// <see cref="IPlugin.Name"/>. If <see cref="IPlugin.ForceDefaultTheme"/>
        /// is true, no component themes are applied.
        /// </summary>
        /// <param name="window">The window to theme.</param>
        /// <param name="plugin">The plugin instance.</param>
        public static void ApplyForPlugin(Window window, Cycloside.Plugins.IPlugin plugin)
        {
            if (plugin.ForceDefaultTheme)
                return;

            ApplyComponentTheme(window, "Plugins");
            ApplyComponentTheme(window, plugin.Name);

            if (SettingsManager.Settings.PluginSkins.TryGetValue(plugin.Name, out var skin) && !string.IsNullOrEmpty(skin))
            {
                SkinManager.ApplySkinTo(window, skin);
            }
        }

        /// <summary>
        /// Applies a specific theme to a single window, overriding the global theme for that window only.
        /// </summary>
        public static void ApplyComponentTheme(StyledElement element, string componentName)
        {
            if (SettingsManager.Settings.ComponentThemes.TryGetValue(componentName, out var themeName) && !string.IsNullOrEmpty(themeName))
            {
                var file = Path.Combine(ThemeDir, $"{themeName}.axaml");
                if (!File.Exists(file))
                {
                    Logger.Log($"Component theme '{themeName}' for '{componentName}' not found.");
                    return;
                }

                try
                {
                    // FIXED: Use proper file URI for component themes
                    var fileUri = new Uri(file);
                    var themeStyle = new StyleInclude(fileUri);
                    element.Styles.Add(themeStyle);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to apply component theme '{themeName}' to '{componentName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Refreshes component themes for all existing windows.
        /// This is called when component theme settings change.
        /// </summary>
        public static void RefreshComponentThemes()
        {
            try
            {
                var windows = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.Windows.ToList()
                    : new List<Window>();

                foreach (var window in windows)
                {
                    var windowName = window.GetType().Name;
                    
                    // Clear existing component themes
                    var existingThemes = window.Styles.OfType<StyleInclude>()
                        .Where(s => s.Source?.OriginalString.Contains("Themes/Global") == true)
                        .ToList();
                    
                    foreach (var theme in existingThemes)
                    {
                        window.Styles.Remove(theme);
                    }
                    
                    // Re-apply component theme
                    ApplyComponentTheme(window, windowName);
                    
                    // Force visual refresh
                    window.InvalidateVisual();
                    
                    Logger.Log($"Refreshed component theme for window: {windowName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error refreshing component themes: {ex.Message}");
            }
        }
    }
}

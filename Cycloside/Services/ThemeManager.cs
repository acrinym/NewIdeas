using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages the application of global and component-specific themes.
    /// This has been rewritten to be more robust and correct.
    /// </summary>
    public static class ThemeManager
    {
        private static string ThemeDir => Path.Combine(AppContext.BaseDirectory, "Themes");

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
            if (Application.Current == null) return false;

            var file = Path.Combine(ThemeDir, $"{themeName}.axaml");
            if (!File.Exists(file))
            {
                Logger.Log($"Global theme '{themeName}' not found at '{file}'.");
                return false;
            }

            // Remove any existing global theme to prevent conflicts
            var existing = Application.Current.Styles.OfType<StyleInclude>()
                .FirstOrDefault(s => s.Source?.OriginalString.Contains("/Themes/") == true);
            if (existing != null)
            {
                Application.Current.Styles.Remove(existing);
            }

            try
            {
                var newThemeStyle = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside"))
                {
                    Source = new Uri(file)
                };
                Application.Current.Styles.Add(newThemeStyle);
                SettingsManager.Settings.GlobalTheme = themeName;
                SettingsManager.Save();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load theme '{themeName}': {ex.Message}");
                return false;
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
                    var themeStyle = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside"))
                    {
                        Source = new Uri(file)
                    };
                    element.Styles.Add(themeStyle);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to apply component theme '{themeName}' to '{componentName}': {ex.Message}");
                }
            }
        }
    }
}

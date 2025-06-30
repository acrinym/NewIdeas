using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    public static class ThemeManager
    {
        private static string GlobalThemeDir => Path.Combine(AppContext.BaseDirectory, "Themes", "Global");
        private static string SkinDir => Path.Combine(AppContext.BaseDirectory, "Skins");

        /// <summary>
        /// Applies the application-wide global theme from settings.
        /// Falls back to a default theme if the setting is invalid.
        /// </summary>
        public static void LoadGlobalThemeFromSettings()
        {
            var themeName = SettingsManager.Settings.GlobalTheme;

            // If the configured theme name is empty, default to "MintGreen"
            if (string.IsNullOrWhiteSpace(themeName))
            {
                themeName = "MintGreen";
            }
            
            LoadGlobalTheme(themeName);
        }

        /// <summary>
        /// Applies a single global theme to the entire application.
        /// It clears any previously loaded global theme first.
        /// </summary>
        public static void LoadGlobalTheme(string themeName)
        {
            if (Application.Current == null) return;

            var file = Path.Combine(GlobalThemeDir, $"{themeName}.axaml");
            if (!File.Exists(file))
            {
                Logger.Log($"Global theme '{themeName}' not found at '{file}'.");
                return;
            }

            // Remove any existing global theme to prevent conflicts
            var existing = Application.Current.Styles.OfType<StyleInclude>()
                .FirstOrDefault(x => x.Source?.OriginalString.Contains("/Themes/Global/") == true);
            if (existing != null)
            {
                Application.Current.Styles.Remove(existing);
            }

            var newThemeStyle = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside"))
            {
                Source = new Uri(file)
            };
            Application.Current.Styles.Add(newThemeStyle);
            SettingsManager.Settings.GlobalTheme = themeName; // Save the active theme
            SettingsManager.Save();
        }

        /// <summary>
        /// Applies the global theme and any component specific skins
        /// based on the current settings. This provides a single
        /// convenience entry point for plugins to style their windows.
        /// </summary>
        /// <param name="window">The window or control to skin.</param>
        /// <param name="componentName">The component identifier used when looking up skins.</param>
        public static void ApplyFromSettings(Window window, string componentName)
        {
            LoadGlobalThemeFromSettings();
            ApplyComponentSkins(window, componentName);
        }

        /// <summary>
        /// Applies component-specific skins from settings to a given window or control.
        /// This method is the missing link that connects the ComponentSkins setting to the UI.
        /// </summary>
        /// <param name="element">The UI element (e.g., a Window) to apply skins to.</param>
        /// <param name="componentName">The name of the component (e.g., a plugin's name).</param>
        public static void ApplyComponentSkins(StyledElement element, string componentName)
        {
            // First, apply any wildcard skins meant for all components
            ApplySkinsForComponent(element, "*");

            // Then, apply the specific component's skin, which will override the wildcard if needed
            ApplySkinsForComponent(element, componentName);
        }

        private static void ApplySkinsForComponent(StyledElement element, string componentName)
        {
            if (!SettingsManager.Settings.ComponentSkins.TryGetValue(componentName, out var skinNames))
            {
                return; // No skins defined for this component
            }

            foreach (var skinName in skinNames)
            {
                var file = Path.Combine(SkinDir, $"{skinName}.axaml");
                if (!File.Exists(file))
                {
                    Logger.Log($"Component skin '{skinName}' for '{componentName}' not found at '{file}'.");
                    continue;
                }

                var skinStyle = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside"))
                {
                    Source = new Uri(file)
                };
                element.Styles.Add(skinStyle);
            }
        }
    }
}

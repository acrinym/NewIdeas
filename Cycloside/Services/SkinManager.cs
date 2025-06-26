using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    public static class SkinManager
    {
        private static string SkinDir => Path.Combine(AppContext.BaseDirectory, "Skins");

        /// <summary>
        /// Loads the specified style file into <see cref="Application.Current"/> global styles.
        /// Any previously loaded style matching <paramref name="identifier"/> is removed first.
        /// </summary>
        public static void LoadIntoApplication(string file, string identifier)
        {
            if (Application.Current == null) return;

            if (!File.Exists(file))
            {
                Logger.Log($"Skin file not found at '{file}'.");
                return;
            }

            var existing = Application.Current.Styles.OfType<StyleInclude>()
                .FirstOrDefault(x => x.Source?.OriginalString.Contains(identifier) == true);
            if (existing != null)
                Application.Current.Styles.Remove(existing);

            var style = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside"))
            {
                Source = new Uri(file)
            };
            Application.Current.Styles.Add(style);
        }

        /// <summary>
        /// Applies a specific skin directly to a UI element (like a Window or Control).
        /// This style is layered on top of the global theme.
        /// </summary>
        public static void ApplySkinTo(StyledElement element, string skinName)
        {
            var file = Path.Combine(SkinDir, $"{skinName}.axaml");
            if (!File.Exists(file))
            {
                Logger.Log($"Component skin '{skinName}' not found at '{file}'.");
                return;
            }

            if (IsFileATheme(file))
            {
                Logger.Log($"Warning: The file '{skinName}.axaml' appears to be a global theme but is being applied as a component skin. This may cause unexpected visual results.");
            }

            var skinStyle = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside"))
            {
                Source = new Uri(file)
            };
            element.Styles.Add(skinStyle);
        }

        /// <summary>
        /// Looks up skins for the given component in settings and applies them to the element.
        /// </summary>
        public static void ApplyFromSettings(StyledElement element, string component)
        {
            var map = SettingsManager.Settings.ComponentSkins;
            if (map.TryGetValue(component, out var skins))
            {
                foreach (var skin in skins)
                    ApplySkinTo(element, skin);
            }
        }

        private static bool IsFileATheme(string path)
        {
            try
            {
                var content = File.ReadAllText(path);
                return content.Contains("ApplicationBackgroundBrush") || content.Contains("ThemeForegroundColor");
            }
            catch
            {
                return false;
            }
        }
    }
}

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

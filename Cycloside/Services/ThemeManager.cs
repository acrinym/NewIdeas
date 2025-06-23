using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    public static class ThemeManager
    {
        private static string ThemeDir => Path.Combine(AppContext.BaseDirectory, "Themes", "Global");

        /// <summary>
        /// Applies a single global theme to the entire application.
        /// It clears any previously loaded global theme first.
        /// </summary>
        public static void LoadGlobalTheme(string themeName)
        {
            if (Application.Current == null) return;

            var file = Path.Combine(ThemeDir, $"{themeName}.axaml");
            if (!File.Exists(file))
            {
                Logger.Log($"Global theme '{themeName}' not found at '{file}'.");
                return;
            }

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
        }
    }
}

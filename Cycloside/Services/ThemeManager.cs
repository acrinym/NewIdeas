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
            var file = Path.Combine(ThemeDir, $"{themeName}.axaml");
            SkinManager.LoadIntoApplication(file, "/Themes/Global/");
        }
    }
}

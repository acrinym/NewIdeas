using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cycloside;

public static class SkinManager
{
    private static string SkinDir => Path.Combine(AppContext.BaseDirectory, "Skins");

    /// <summary>
    /// Loads the current skin (application-wide) from file in SkinDir, based on SettingsManager.Settings.ActiveSkin.
    /// </summary>
    public static void LoadCurrent()
    {
        var name = SettingsManager.Settings.ActiveSkin;
        var file = Path.Combine(SkinDir, $"{name}.axaml");
        if (File.Exists(file) && Application.Current != null)
        {
            var style = new StyleInclude(new Uri("resm:Styles?assembly=Cycloside")) { Source = new Uri(file) };
            Application.Current.Styles.Add(style);
        }
    }

    /// <summary>
    /// Applies a skin to a specific StyledElement, using avares:// URI. Useful for dynamic or per-component theming.
    /// </summary>
    public static void ApplySkin(StyledElement element, string name)
    {
        var uri = new Uri($"avares://Cycloside/Skins/{name}.axaml");
        var include = new StyleInclude(uri) { Source = uri };
        element.Styles.Add(include);
    }
}

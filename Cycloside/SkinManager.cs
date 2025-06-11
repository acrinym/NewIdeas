using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.IO;

namespace Cycloside;

public static class SkinManager
{
    private static string SkinDir => Path.Combine(AppContext.BaseDirectory, "Skins");

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
}

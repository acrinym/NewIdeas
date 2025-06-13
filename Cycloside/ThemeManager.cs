using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cycloside;

public class ThemeSnapshot
{
    public string Theme { get; set; } = "MintGreen";
    public Dictionary<string, string> ComponentThemes { get; set; } = new();
    public string Skin { get; set; } = "Default";
}

public static class ThemeManager
{
    private static void Apply(Styles styles, string name)
    {
        var uri = new Uri($"avares://Cycloside/Themes/Global/{name}.axaml");
        var include = new StyleInclude(uri) { Source = uri };
        var existing = styles.OfType<StyleInclude>()
            .FirstOrDefault(x => x.Source?.OriginalString.Contains("/Themes/") == true);
        if (existing != null)
            styles.Remove(existing);
        styles.Add(include);
    }

    public static void ApplyTheme(Application app, string name) => Apply(app.Styles, name);

    public static void ApplyTheme(StyledElement element, string name) => Apply(element.Styles, name);

    public static void ApplyFromSettings(StyledElement element, string component)
    {
        if (SettingsManager.Settings.ComponentThemes.TryGetValue(component, out var theme))
            ApplyTheme(element, theme);
    }

    public static void SaveSnapshot(string name)
    {
        var snap = new ThemeSnapshot
        {
            Theme = SettingsManager.Settings.Theme,
            ComponentThemes = new Dictionary<string, string>(SettingsManager.Settings.ComponentThemes),
            Skin = SettingsManager.Settings.ActiveSkin
        };
        SettingsManager.Settings.SavedThemes[name] = snap;
        SettingsManager.Save();
    }

    public static void RestoreSnapshot(string name)
    {
        if (SettingsManager.Settings.SavedThemes.TryGetValue(name, out var snap))
        {
            SettingsManager.Settings.Theme = snap.Theme;
            SettingsManager.Settings.ComponentThemes = new Dictionary<string, string>(snap.ComponentThemes);
            SettingsManager.Settings.ActiveSkin = snap.Skin;
            SettingsManager.Save();
        }
    }

    public static void ResetToDefault()
    {
        SaveSnapshot($"savethm{SettingsManager.Settings.SavedThemes.Count + 1}");
        SettingsManager.Settings.Theme = "MintGreen";
        SettingsManager.Settings.ComponentThemes.Clear();
        SettingsManager.Settings.ActiveSkin = "Default";
        SettingsManager.Save();
    }
}

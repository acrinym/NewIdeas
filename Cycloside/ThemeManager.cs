using System;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cycloside;

public static class ThemeManager
{
    private static void Apply(Styles styles, string name)
    {
        var uri = new Uri($"avares://Cycloside/Themes/{name}.axaml");
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
}

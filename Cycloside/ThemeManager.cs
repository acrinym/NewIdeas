using System;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace Cycloside;

public static class ThemeManager
{
    public static void ApplyTheme(Application app, string name)
    {
        var uri = new Uri($"avares://Cycloside/Themes/{name}.axaml");
        var include = new StyleInclude(uri) { Source = uri };
        var existing = app.Styles.OfType<StyleInclude>()
            .FirstOrDefault(x => x.Source?.OriginalString.Contains("/Themes/") == true);
        if (existing != null)
            app.Styles.Remove(existing);
        app.Styles.Add(include);
    }
}

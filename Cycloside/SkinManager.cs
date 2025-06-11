using System;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.Styling;

namespace Cycloside;

public static class SkinManager
{
    public static void ApplySkin(StyledElement element, string name)
    {
        var uri = new Uri($"avares://Cycloside/Skins/{name}.axaml");
        var include = new StyleInclude(uri) { Source = uri };
        element.Styles.Add(include);
    }
}

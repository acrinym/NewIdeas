using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class WallpaperWindow : Window
{
    public WallpaperWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

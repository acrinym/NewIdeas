using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class DateTimeOverlayWindow : Window
{
    public DateTimeOverlayWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

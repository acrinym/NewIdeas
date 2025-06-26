using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class ClipboardManagerWindow : Window
{
    public ClipboardManagerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

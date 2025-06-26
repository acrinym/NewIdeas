using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class MacroWindow : Window
{
    public MacroWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

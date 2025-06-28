using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class TerminalWindow : Window
{
    public TerminalWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class LogViewerWindow : Window
{
    public LogViewerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

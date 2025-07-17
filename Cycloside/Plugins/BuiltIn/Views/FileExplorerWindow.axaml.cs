using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class FileExplorerWindow : Window
{
    public FileExplorerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

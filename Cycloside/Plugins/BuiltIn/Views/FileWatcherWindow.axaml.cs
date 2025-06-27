using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class FileWatcherWindow : Window
{
    public FileWatcherWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

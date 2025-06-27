using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class ProcessMonitorWindow : Window
{
    public ProcessMonitorWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

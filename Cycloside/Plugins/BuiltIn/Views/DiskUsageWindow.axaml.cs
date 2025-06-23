using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class DiskUsageWindow : Window
{
    public DiskUsageWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

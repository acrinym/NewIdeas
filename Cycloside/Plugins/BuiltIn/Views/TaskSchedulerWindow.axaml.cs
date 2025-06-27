using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class TaskSchedulerWindow : Window
{
    public TaskSchedulerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

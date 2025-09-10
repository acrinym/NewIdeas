using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn.Views
{
    public partial class TaskManagerWindow : Window
    {
        public TaskManagerWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

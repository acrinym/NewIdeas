using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class EnvironmentEditorWindow : Window
{
    public EnvironmentEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

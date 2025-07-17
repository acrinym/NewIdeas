using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class CodeEditorWindow : Window
{
    public CodeEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

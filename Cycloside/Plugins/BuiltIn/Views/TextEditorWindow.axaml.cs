using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside.Plugins.BuiltIn;

public partial class TextEditorWindow : Window
{
    public TextEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

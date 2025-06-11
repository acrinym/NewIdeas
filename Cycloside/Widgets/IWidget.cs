using Avalonia.Controls;

namespace Cycloside.Widgets;

public interface IWidget
{
    string Name { get; }
    Control BuildView();
}

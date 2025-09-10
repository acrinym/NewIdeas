using Avalonia.Controls;
using Cycloside.Widgets;

namespace Cycloside.SDK.Examples.KitchenSink
{
    public class KitchenSinkWidget : IWidget
    {
        public string Name => "Kitchen Sink Widget";
        public Control BuildView() => new TextBlock { Text = "Hello from the Kitchen Sink Widget!" };
    }
}

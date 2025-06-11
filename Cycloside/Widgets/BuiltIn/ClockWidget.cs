using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace Cycloside.Widgets.BuiltIn;

public class ClockWidget : IWidget
{
    public string Name => "Clock";

    public Control BuildView()
    {
        var text = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 14
        };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => text.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();
        return new Border
        {
            Background = Brushes.Black,
            Opacity = 0.7,
            Padding = new Thickness(4),
            Child = text
        };
    }
}

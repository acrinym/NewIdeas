using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Widgets.BuiltIn;

public class Mp3Widget : IWidget
{
    private readonly MP3PlayerPlugin _plugin;

    public Mp3Widget(MP3PlayerPlugin plugin)
    {
        _plugin = plugin;
    }

    public string Name => "MP3 Player";

    public Control BuildView()
    {
        var openButton = new Button { Content = "Open", Command = _plugin.OpenFilesCommand };
        var prevButton = new Button { Content = "Prev", Command = _plugin.PreviousCommand };
        var playButton = new Button { Content = "Play", Command = _plugin.PlayCommand };
        var pauseButton = new Button { Content = "Pause", Command = _plugin.PauseCommand };
        var stopButton = new Button { Content = "Stop", Command = _plugin.StopPlaybackCommand };
        var nextButton = new Button { Content = "Next", Command = _plugin.NextCommand };

        var panel = new StackPanel { Orientation = Orientation.Horizontal, DataContext = _plugin };
        panel.Children.Add(openButton);
        panel.Children.Add(prevButton);
        panel.Children.Add(playButton);
        panel.Children.Add(pauseButton);
        panel.Children.Add(stopButton);
        panel.Children.Add(nextButton);

        return new Border
        {
            Background = Brushes.Black,
            Opacity = 0.7,
            Padding = new Thickness(4),
            Child = panel
        };
    }
}

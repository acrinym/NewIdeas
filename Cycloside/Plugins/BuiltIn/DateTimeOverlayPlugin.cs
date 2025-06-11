using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using System;

namespace Cycloside.Plugins.BuiltIn;

public class DateTimeOverlayPlugin : IPlugin
{
    private Window? _window;
    private DispatcherTimer? _timer;

    public string Name => "Date/Time Overlay";
    public string Description => "Displays the current date and time in a small overlay.";
    public Version Version => new(1,0,0);

    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _window = new Window
        {
            Width = 200,
            Height = 40,
            SystemDecorations = SystemDecorations.None,
            CanResize = false,
            Topmost = true,
            Background = Brushes.Black,
            Opacity = 0.7,
        };
        var text = new TextBlock { Foreground = Brushes.White, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        _window.Content = text;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => text.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _timer.Start();
        _window.Show();
    }

    public void Stop()
    {
        _timer?.Stop();
        _window?.Close();
        _timer = null;
        _window = null;
    }
}

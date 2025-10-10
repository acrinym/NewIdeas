using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class GlideOpenEffect : IWindowEffect
{
    public string Name => "GlideOpen";
    public string Description => "Slide in from the left edge and fade in on open.";

    public void Attach(Window window)
    {
        window.Opened += Window_Opened;
    }

    public void Detach(Window window)
    {
        window.Opened -= Window_Opened;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void Window_Opened(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;

        var end = window.Position;
        var start = new PixelPoint(end.X - 140, end.Y);

        // Initialize starting state
        window.Position = start;
        window.Opacity = 0.0;

        var duration = TimeSpan.FromMilliseconds(280);
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };

        timer.Tick += (_, _) =>
        {
            var t = DateTime.UtcNow - startTime;
            var p = t.TotalMilliseconds / duration.TotalMilliseconds;
            if (p >= 1.0)
            {
                window.Position = end;
                window.Opacity = 1.0;
                timer.Stop();
                return;
            }

            // Ease-out cubic
            var ease = 1 - Math.Pow(1 - p, 3);
            var x = (int)(start.X + (end.X - start.X) * ease);
            var y = (int)(start.Y + (end.Y - start.Y) * ease);
            window.Position = new PixelPoint(x, y);
            window.Opacity = 0.2 + 0.8 * ease;
        };

        timer.Start();
    }
}
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class DodgeFocusEffect : IWindowEffect
{
    public string Name => "DodgeFocus";
    public string Description => "Window nudges away on blur, returns on focus";

    public void Attach(Window window)
    {
        window.Deactivated += OnBlur;
        window.Activated += OnFocus;
    }

    public void Detach(Window window)
    {
        window.Deactivated -= OnBlur;
        window.Activated -= OnFocus;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void OnBlur(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        AnimatePosition(window, offsetX: 16, offsetY: 12, durationMs: 140);
    }

    private void OnFocus(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        AnimatePosition(window, offsetX: 0, offsetY: 0, durationMs: 160);
    }

    private static void AnimatePosition(Window window, int offsetX, int offsetY, int durationMs)
    {
        var start = window.Position;
        var end = new PixelPoint(start.X + offsetX, start.Y + offsetY);
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var t = DateTime.UtcNow - startTime;
            var p = t.TotalMilliseconds / duration.TotalMilliseconds;
            if (p >= 1.0)
            {
                window.Position = end;
                timer.Stop();
                return;
            }
            var ease = 1 - Math.Pow(1 - p, 3);
            var x = (int)(start.X + (end.X - start.X) * ease);
            var y = (int)(start.Y + (end.Y - start.Y) * ease);
            window.Position = new PixelPoint(x, y);
        };
        timer.Start();
    }
}
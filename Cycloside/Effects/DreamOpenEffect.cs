using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class DreamOpenEffect : IWindowEffect
{
    public string Name => "DreamOpen";
    public string Description => "Soft blur and fade-in on open";

    public void Attach(Window window)
    {
        window.Opened += OnOpened;
    }

    public void Detach(Window window)
    {
        window.Opened -= OnOpened;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        window.Opacity = 0.0;
        var blur = new BlurEffect { Radius = 10 };
        window.Effect = blur;

        var duration = TimeSpan.FromMilliseconds(300);
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var t = DateTime.UtcNow - startTime;
            var p = Math.Clamp(t.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            var ease = 1 - Math.Pow(1 - p, 3);
            window.Opacity = 0.2 + 0.8 * ease;
            blur.Radius = 10 * (1 - ease);
            if (p >= 1.0)
            {
                window.Opacity = 1.0;
                window.Effect = null;
                timer.Stop();
            }
        };
        timer.Start();
    }
}
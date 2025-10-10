using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class BeamUpMinimizeEffect : IWindowEffect
{
    public string Name => "BeamUpMinimize";
    public string Description => "Move upward, shrink and fade, then minimize.";

    private readonly HashSet<Window> _animating = new();
    private readonly Dictionary<Window, PixelPoint> _origPositions = new();

    public void Attach(Window window)
    {
        window.PropertyChanged += Window_PropertyChanged;
    }

    public void Detach(Window window)
    {
        window.PropertyChanged -= Window_PropertyChanged;
        _animating.Remove(window);
        _origPositions.Remove(window);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void Window_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Window window) return;
        if (e.Property != Window.WindowStateProperty) return;

        var newState = (WindowState)e.NewValue!;
        if (newState != WindowState.Minimized) return;
        if (_animating.Contains(window)) return;

        _animating.Add(window);
        try
        {
            var originalPos = window.Position;
            _origPositions[window] = originalPos;
            window.WindowState = WindowState.Normal;

            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(220);
            var startOpacity = window.Opacity;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) =>
            {
                var t = DateTime.UtcNow - startTime;
                var p = t.TotalMilliseconds / duration.TotalMilliseconds;
                if (p >= 1.0)
                {
                    timer.Stop();
                    window.Opacity = startOpacity;
                    window.Position = originalPos;
                    window.WindowState = WindowState.Minimized;
                    _animating.Remove(window);
                    return;
                }
                var ease = 1 - Math.Pow(1 - p, 3);
                var offsetY = (int)(-120 * ease);
                window.Position = new PixelPoint(originalPos.X, originalPos.Y + offsetY);
                window.Opacity = 1.0 - ease;
            };
            timer.Start();
        }
        catch
        {
            window.WindowState = WindowState.Minimized;
            _animating.Remove(window);
        }
    }
}
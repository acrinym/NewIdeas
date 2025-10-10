using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class MagicLampMinimizeEffect : IWindowEffect
{
    public string Name => "MagicLampMinimize";
    public string Description => "Squash toward titlebar and fade, then minimize.";

    private readonly HashSet<Window> _animating = new();
    private readonly Dictionary<Window, double> _origHeights = new();

    public void Attach(Window window)
    {
        window.PropertyChanged += Window_PropertyChanged;
    }

    public void Detach(Window window)
    {
        window.PropertyChanged -= Window_PropertyChanged;
        _animating.Remove(window);
        _origHeights.Remove(window);
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
            var originalHeight = window.Height;
            _origHeights[window] = originalHeight;
            window.WindowState = WindowState.Normal;

            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(220);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) =>
            {
                var t = DateTime.UtcNow - startTime;
                var p = t.TotalMilliseconds / duration.TotalMilliseconds;
                if (p >= 1.0)
                {
                    window.Opacity = 0.0;
                    timer.Stop();
                    window.WindowState = WindowState.Minimized;
                    window.Height = originalHeight; // restore height for when restored
                    window.Opacity = 1.0; // reset opacity
                    _animating.Remove(window);
                    return;
                }
                var ease = 1 - Math.Pow(1 - p, 3);
                var targetHeight = Math.Max(30, originalHeight * (1 - 0.85 * ease));
                window.Height = targetHeight;
                window.Opacity = 1.0 - ease;
            };
            timer.Start();
        }
        catch
        {
            // Fallback: just minimize
            window.WindowState = WindowState.Minimized;
            _animating.Remove(window);
        }
    }
}
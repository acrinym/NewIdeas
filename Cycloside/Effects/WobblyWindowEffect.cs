using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace Cycloside.Effects;

public class WobblyWindowEffect : IWindowEffect
{
    public string Name => "Wobbly";
    public string Description => "Adds a springy wobble when moving the window";

    private readonly Dictionary<Window, PixelPoint> _target = new();
    private readonly Dictionary<Window, DispatcherTimer> _timers = new();

    public void Attach(Window window)
    {
        _target[window] = window.Position;
        window.PositionChanged += OnPosChanged;
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) => Animate(window);
        _timers[window] = timer;
        timer.Start();
    }

    private void OnPosChanged(object? sender, PixelPointEventArgs e)
    {
        if (sender is Window win)
            _target[win] = e.Point;
    }

    private void Animate(Window window)
    {
        if (!_target.TryGetValue(window, out var target))
            return;

        var current = window.Position;
        var dx = (target.X - current.X) * 0.25;
        var dy = (target.Y - current.Y) * 0.25;
        if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1)
            return;

        window.Position = new PixelPoint(current.X + (int)dx, current.Y + (int)dy);
    }

    public void Detach(Window window)
    {
        window.PositionChanged -= OnPosChanged;
        if (_timers.TryGetValue(window, out var timer))
        {
            timer.Stop();
            _timers.Remove(window);
        }
        _target.Remove(window);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }
}

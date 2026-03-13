using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class GlideDownOpenEffect : IWindowEffect
{
    private readonly Dictionary<ISceneTarget, EventHandler> _handlers = new();

    public string Name => "GlideDownOpen";
    public string Description => "Slide in from below and fade in on open.";

    public void Attach(ISceneTarget target)
    {
        if (target is not WindowSceneAdapter adapter) return;
        var handler = new EventHandler((s, e) => OnOpened(target));
        _handlers[target] = handler;
        adapter.Window.Opened += handler;
    }

    public void Detach(ISceneTarget target)
    {
        if (target is not WindowSceneAdapter adapter || !_handlers.TryGetValue(target, out var handler))
            return;
        adapter.Window.Opened -= handler;
        _handlers.Remove(target);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void OnOpened(ISceneTarget target)
    {
        var end = target.Position;
        var start = new PixelPoint(end.X, end.Y + 120);
        target.Position = start;
        target.Opacity = 0.0;

        var duration = TimeSpan.FromMilliseconds(280);
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var t = DateTime.UtcNow - startTime;
            var p = t.TotalMilliseconds / duration.TotalMilliseconds;
            if (p >= 1.0)
            {
                target.Position = end;
                target.Opacity = 1.0;
                timer.Stop();
                (timer as IDisposable)?.Dispose();
                return;
            }
            var ease = 1 - Math.Pow(1 - p, 3);
            var x = (int)(start.X + (end.X - start.X) * ease);
            var y = (int)(start.Y + (end.Y - start.Y) * ease);
            target.Position = new PixelPoint(x, y);
            target.Opacity = 0.2 + 0.8 * ease;
        };
        timer.Start();
    }
}
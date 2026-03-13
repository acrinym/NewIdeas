using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class DreamOpenEffect : IWindowEffect
{
    private readonly Dictionary<ISceneTarget, EventHandler> _handlers = new();

    public string Name => "DreamOpen";
    public string Description => "Soft blur and fade-in on open";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        var handler = new EventHandler((s, e) => OnOpened(target));
        _handlers[target] = handler;
        window.Opened += handler;
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null || !_handlers.TryGetValue(target, out var handler))
            return;
        window.Opened -= handler;
        _handlers.Remove(target);
    }

    private void OnOpened(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;

        target.Opacity = 0.0;
        var blur = new BlurEffect { Radius = 10 };
        window.Effect = blur;

        var duration = TimeSpan.FromMilliseconds(EffectConstants.DreamOpenDurationMs);
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EffectConstants.TickIntervalMs) };
        timer.Tick += (_, _) =>
        {
            var t = DateTime.UtcNow - startTime;
            var p = Math.Clamp(t.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            var ease = 1 - Math.Pow(1 - p, 3);
            target.Opacity = 0.2 + 0.8 * ease;
            blur.Radius = 10 * (1 - ease);
            if (p >= 1.0)
            {
                target.Opacity = 1.0;
                window.Effect = null;
                timer.Stop();
                (timer as IDisposable)?.Dispose();
            }
        };
        timer.Start();
    }
}
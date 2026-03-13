using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class WobblyWindowEffect : IWindowEffect
{
    private readonly Dictionary<Window, ISceneTarget> _windowToTarget = new();
    private readonly Dictionary<ISceneTarget, PixelPoint> _targetPos = new();
    private readonly Dictionary<ISceneTarget, DispatcherTimer> _timers = new();
    private readonly Dictionary<ISceneTarget, EventHandler<PixelPointEventArgs>> _posHandlers = new();

    public string Name => "Wobbly";
    public string Description => "Adds a springy wobble when moving the window";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        _windowToTarget[window] = target;
        _targetPos[target] = target.Position;
        var posHandler = new EventHandler<PixelPointEventArgs>((s, e) => OnPosChanged(target, e));
        _posHandlers[target] = posHandler;
        window.PositionChanged += posHandler;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EffectConstants.TickIntervalMs) };
        timer.Tick += (_, _) => Animate(target);
        _timers[target] = timer;
        timer.Start();
    }

    private void OnPosChanged(ISceneTarget target, PixelPointEventArgs e)
    {
        _targetPos[target] = e.Point;
    }

    private void Animate(ISceneTarget target)
    {
        if (!_targetPos.TryGetValue(target, out var targetPoint))
            return;

        var current = target.Position;
        var dx = (targetPoint.X - current.X) * 0.25;
        var dy = (targetPoint.Y - current.Y) * 0.25;
        if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1)
            return;

        target.Position = new PixelPoint(current.X + (int)dx, current.Y + (int)dy);
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        if (!_posHandlers.TryGetValue(target, out var posHandler))
            return;
        window.PositionChanged -= posHandler;
        _posHandlers.Remove(target);
        _windowToTarget.Remove(window);
        if (_timers.TryGetValue(target, out var timer))
        {
            timer.Stop();
            (timer as IDisposable)?.Dispose();
            _timers.Remove(target);
        }
        _targetPos.Remove(target);
    }
}

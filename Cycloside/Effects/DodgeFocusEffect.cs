using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class DodgeFocusEffect : IWindowEffect
{
    private readonly Dictionary<ISceneTarget, EventHandler> _blurHandlers = new();
    private readonly Dictionary<ISceneTarget, EventHandler> _focusHandlers = new();

    public string Name => "DodgeFocus";
    public string Description => "Window nudges away on blur, returns on focus";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        var blurHandler = new EventHandler((s, e) => OnBlur(target));
        var focusHandler = new EventHandler((s, e) => OnFocus(target));
        _blurHandlers[target] = blurHandler;
        _focusHandlers[target] = focusHandler;
        window.Deactivated += blurHandler;
        window.Activated += focusHandler;
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null || !_blurHandlers.TryGetValue(target, out var blurHandler) || !_focusHandlers.TryGetValue(target, out var focusHandler))
            return;
        window.Deactivated -= blurHandler;
        window.Activated -= focusHandler;
        _blurHandlers.Remove(target);
        _focusHandlers.Remove(target);
    }

    private void OnBlur(ISceneTarget target)
    {
        AnimatePosition(target, offsetX: 16, offsetY: 12, durationMs: 140);
    }

    private void OnFocus(ISceneTarget target)
    {
        AnimatePosition(target, offsetX: 0, offsetY: 0, durationMs: 160);
    }

    private static void AnimatePosition(ISceneTarget target, int offsetX, int offsetY, int durationMs)
    {
        var start = target.Position;
        var end = new PixelPoint(start.X + offsetX, start.Y + offsetY);
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EffectConstants.TickIntervalMs) };
        timer.Tick += (_, _) =>
        {
            var t = DateTime.UtcNow - startTime;
            var p = t.TotalMilliseconds / duration.TotalMilliseconds;
            if (p >= 1.0)
            {
                target.Position = end;
                timer.Stop();
                (timer as IDisposable)?.Dispose();
                return;
            }
            var ease = 1 - Math.Pow(1 - p, 3);
            var x = (int)(start.X + (end.X - start.X) * ease);
            var y = (int)(start.Y + (end.Y - start.Y) * ease);
            target.Position = new PixelPoint(x, y);
        };
        timer.Start();
    }
}
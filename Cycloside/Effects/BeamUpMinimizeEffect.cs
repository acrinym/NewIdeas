using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Scene;
using Cycloside.Services;

namespace Cycloside.Effects;

public class BeamUpMinimizeEffect : IWindowEffect
{
    private readonly HashSet<Window> _animating = new();
    private readonly Dictionary<Window, PixelPoint> _origPositions = new();
    private readonly Dictionary<Window, ISceneTarget> _windowToTarget = new();

    public string Name => "BeamUpMinimize";
    public string Description => "Move upward, shrink and fade, then minimize.";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        _windowToTarget[window] = target;
        window.PropertyChanged += Window_PropertyChanged;
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        window.PropertyChanged -= Window_PropertyChanged;
        _animating.Remove(window);
        _origPositions.Remove(window);
        _windowToTarget.Remove(window);
    }

    private void Window_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Window window) return;
        if (!_windowToTarget.TryGetValue(window, out var target)) return;
        if (e.Property != Window.WindowStateProperty) return;

        var newState = (WindowState)e.NewValue!;
        if (newState != WindowState.Minimized) return;
        if (_animating.Contains(window)) return;

        _animating.Add(window);
        try
        {
            var originalPos = target.Position;
            _origPositions[window] = originalPos;
            window.WindowState = WindowState.Normal;

            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(EffectConstants.MinimizeEffectDurationMs);
            var startOpacity = target.Opacity;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EffectConstants.TickIntervalMs) };
            timer.Tick += (_, _) =>
            {
                var t = DateTime.UtcNow - startTime;
                var p = t.TotalMilliseconds / duration.TotalMilliseconds;
                if (p >= 1.0)
                {
                    timer.Stop();
                    (timer as IDisposable)?.Dispose();
                    target.Opacity = startOpacity;
                    target.Position = originalPos;
                    window.WindowState = WindowState.Minimized;
                    _animating.Remove(window);
                    return;
                }
                var ease = 1 - Math.Pow(1 - p, 3);
                var offsetY = (int)(-EffectConstants.SlideDistancePx * ease);
                target.Position = new PixelPoint(originalPos.X, originalPos.Y + offsetY);
                target.Opacity = 1.0 - ease;
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            Logger.Log($"BeamUpMinimizeEffect: {ex.Message}");
            window.WindowState = WindowState.Minimized;
            _animating.Remove(window);
        }
    }
}
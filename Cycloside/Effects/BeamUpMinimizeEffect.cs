using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Cycloside;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class BeamUpMinimizeEffect : IWindowEffect
{
    public string Name => "BeamUpMinimize";
    public string Description => "Move upward, shrink and fade, then minimize.";

    private readonly HashSet<Window> _animating = new();
    private readonly Dictionary<Window, PixelPoint> _origPositions = new();
    private readonly HashSet<Window> _ignoreStateChanges = new();
    private readonly Dictionary<Window, DispatcherTimer> _timers = new();

    public void Attach(Window window)
    {
        window.PropertyChanged += Window_PropertyChanged;
    }

    public void Detach(Window window)
    {
        window.PropertyChanged -= Window_PropertyChanged;
        
        // Stop and dispose any running timer for this window
        if (_timers.TryGetValue(window, out var timer))
        {
            timer.Stop();
            _timers.Remove(window);
        }
        
        _animating.Remove(window);
        _origPositions.Remove(window);
        _ignoreStateChanges.Remove(window);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void Window_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Window window) return;
        if (e.Property != Window.WindowStateProperty) return;

        // Ignore state changes that we triggered ourselves
        if (_ignoreStateChanges.Contains(window)) return;

        var newState = (WindowState)e.NewValue!;
        if (newState != WindowState.Minimized) return;
        if (_animating.Contains(window)) return;

        _animating.Add(window);
        try
        {
            var originalPos = window.Position;
            _origPositions[window] = originalPos;
            
            // Set flag to ignore the state change we're about to trigger
            _ignoreStateChanges.Add(window);
            window.WindowState = WindowState.Normal;
            _ignoreStateChanges.Remove(window);

            // Read parameters
            var parms = SettingsManager.Settings.WindowEffectParameters;
            int durationMs = 220;
            int offsetYAbs = 120; // pixels to move upward
            if (parms.TryGetValue(Name, out var mp))
            {
                if (mp.TryGetValue("DurationMs", out var dStr) && int.TryParse(dStr, out var dVal))
                    durationMs = Math.Clamp(dVal, 60, 2000);
                if (mp.TryGetValue("OffsetY", out var oStr) && int.TryParse(oStr, out var oVal))
                    offsetYAbs = Math.Clamp(oVal, 20, 600);
            }

            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(durationMs);
            var startOpacity = window.Opacity;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timers[window] = timer; // Track the timer for this window
            
            timer.Tick += (_, _) =>
            {
                var t = DateTime.UtcNow - startTime;
                var p = t.TotalMilliseconds / duration.TotalMilliseconds;
                if (p >= 1.0)
                {
                    timer.Stop();
                    _timers.Remove(window); // Remove timer reference
                    window.Opacity = startOpacity;
                    window.Position = originalPos;
                    
                    // Set flag to ignore the final minimize state change
                    _ignoreStateChanges.Add(window);
                    window.WindowState = WindowState.Minimized;
                    _ignoreStateChanges.Remove(window);
                    
                    _animating.Remove(window);
                    return;
                }
                var ease = 1 - Math.Pow(1 - p, 3);
                var offsetY = (int)(-offsetYAbs * ease);
                window.Position = new PixelPoint(originalPos.X, originalPos.Y + offsetY);
                window.Opacity = 1.0 - ease;
            };
            timer.Start();
        }
        catch
        {
            // Cleanup timer if it was created
            if (_timers.TryGetValue(window, out var timer))
            {
                timer.Stop();
                _timers.Remove(window);
            }
            
            _ignoreStateChanges.Add(window);
            window.WindowState = WindowState.Minimized;
            _ignoreStateChanges.Remove(window);
            _animating.Remove(window);
        }
    }
}
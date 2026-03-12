using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Cycloside;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class MagicLampMinimizeEffect : IWindowEffect
{
    public string Name => "MagicLampMinimize";
    public string Description => "Squash toward titlebar and fade, then minimize.";

    private readonly HashSet<Window> _animating = new();
    private readonly Dictionary<Window, double> _origHeights = new();
    private readonly HashSet<Window> _ignoreStateChanges = new();
    private readonly Dictionary<Window, DispatcherTimer> _timers = new();

    public void Attach(Window window)
    {
        var message = $"MagicLampMinimize: Attaching to window {window.Title} (Current state: {window.WindowState})";
        System.Diagnostics.Debug.WriteLine(message);
        Console.WriteLine(message);
        window.PropertyChanged += Window_PropertyChanged;
        
        // Also log when the window is first attached
        var attachMessage = $"MagicLampMinimize: Window {window.Title} attached successfully. IsVisible: {window.IsVisible}, IsActive: {window.IsActive}";
        System.Diagnostics.Debug.WriteLine(attachMessage);
        Console.WriteLine(attachMessage);
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
        _origHeights.Remove(window);
        _ignoreStateChanges.Remove(window);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void Window_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Window window) 
        {
            var message = $"MagicLampMinimize: PropertyChanged sender is not Window: {sender?.GetType().Name}";
            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
            return;
        }
        
        var propMessage = $"MagicLampMinimize: Property changed on {window.Title}: {e.Property.Name} = {e.NewValue} (was {e.OldValue})";
        System.Diagnostics.Debug.WriteLine(propMessage);
        Console.WriteLine(propMessage);
        
        if (e.Property != Window.WindowStateProperty) return;

        var oldState = (WindowState?)e.OldValue;
        var newState = (WindowState)e.NewValue!;
        var stateMessage = $"MagicLampMinimize: Window {window.Title} state changed from {oldState} to {newState}";
        System.Diagnostics.Debug.WriteLine(stateMessage);
        Console.WriteLine(stateMessage);
        
        // Ignore state changes that we triggered ourselves
        if (_ignoreStateChanges.Contains(window))
        {
            var selfMessage = $"MagicLampMinimize: Ignoring self-triggered state change to {newState}";
            System.Diagnostics.Debug.WriteLine(selfMessage);
            Console.WriteLine(selfMessage);
            return;
        }
        
        if (newState != WindowState.Minimized) 
        {
            var ignoreMessage = $"MagicLampMinimize: Ignoring state change to {newState} (not Minimized)";
            System.Diagnostics.Debug.WriteLine(ignoreMessage);
            Console.WriteLine(ignoreMessage);
            return;
        }
        
        if (_animating.Contains(window)) 
        {
            var animatingMessage = $"MagicLampMinimize: Window {window.Title} is already animating, skipping";
            System.Diagnostics.Debug.WriteLine(animatingMessage);
            Console.WriteLine(animatingMessage);
            return;
        }

        var startMessage = $"MagicLampMinimize: *** STARTING ANIMATION *** for {window.Title}";
        System.Diagnostics.Debug.WriteLine(startMessage);
        Console.WriteLine(startMessage);

        _animating.Add(window);
        try
        {
            var originalHeight = window.Height;
            _origHeights[window] = originalHeight;
            
            // Set flag to ignore the state change we're about to trigger
            _ignoreStateChanges.Add(window);
            window.WindowState = WindowState.Normal;
            _ignoreStateChanges.Remove(window);

            // Read parameters
            var parms = SettingsManager.Settings.WindowEffectParameters;
            int durationMs = 220;
            double squashFactor = 0.85; // proportion of height reduction at peak
            int minHeight = 30;
            if (parms.TryGetValue(Name, out var mp))
            {
                if (mp.TryGetValue("DurationMs", out var dStr) && int.TryParse(dStr, out var dVal))
                    durationMs = Math.Clamp(dVal, 60, 2000);
                if (mp.TryGetValue("SquashFactor", out var sStr) && double.TryParse(sStr, out var sVal))
                    squashFactor = Math.Clamp(sVal, 0.1, 0.95);
                if (mp.TryGetValue("MinHeight", out var mStr) && int.TryParse(mStr, out var mVal))
                    minHeight = Math.Clamp(mVal, 20, 200);
            }

            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(durationMs);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timers[window] = timer; // Track the timer for this window
            
            timer.Tick += (_, _) =>
            {
                var t = DateTime.UtcNow - startTime;
                var p = t.TotalMilliseconds / duration.TotalMilliseconds;
                if (p >= 1.0)
                {
                    window.Opacity = 0.0;
                    timer.Stop();
                    _timers.Remove(window); // Remove timer reference
                    
                    // Set flag to ignore the final minimize state change
                    _ignoreStateChanges.Add(window);
                    window.WindowState = WindowState.Minimized;
                    _ignoreStateChanges.Remove(window);
                    
                    window.Height = originalHeight; // restore height for when restored
                    window.Opacity = 1.0; // reset opacity
                    _animating.Remove(window);
                    return;
                }
                var ease = 1 - Math.Pow(1 - p, 3);
                var targetHeight = Math.Max(minHeight, originalHeight * (1 - squashFactor * ease));
                window.Height = targetHeight;
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
            
            // Fallback: just minimize
            _ignoreStateChanges.Add(window);
            window.WindowState = WindowState.Minimized;
            _ignoreStateChanges.Remove(window);
            _animating.Remove(window);
        }
    }
}
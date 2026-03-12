using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Cycloside.Effects;

public class FadeEffect : IWindowEffect
{
    public string Name => "Fade";
    public string Description => "Smooth fade in/out animation for window open/close";

    private readonly HashSet<Window> _animating = new();
    private readonly TimeSpan _fadeInDuration;
    private readonly TimeSpan _fadeOutDuration;
    private readonly IEasing _fadeInEasing;
    private readonly IEasing _fadeOutEasing;

    public FadeEffect(
        TimeSpan? fadeInDuration = null, 
        TimeSpan? fadeOutDuration = null,
        IEasing? fadeInEasing = null,
        IEasing? fadeOutEasing = null)
    {
        _fadeInDuration = fadeInDuration ?? TimeSpan.FromMilliseconds(300);
        _fadeOutDuration = fadeOutDuration ?? TimeSpan.FromMilliseconds(200);
        _fadeInEasing = fadeInEasing ?? new QuadraticEaseOut();
        _fadeOutEasing = fadeOutEasing ?? new QuadraticEaseIn();
    }

    public void Attach(Window window)
    {
        window.Closing += OnClosing;
        window.Opened += OnOpened;
    }

    public void Detach(Window window)
    {
        window.Closing -= OnClosing;
        window.Opened -= OnOpened;
        _animating.Remove(window);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;

        // Store original opacity
        var originalOpacity = window.Opacity;

        // Set initial state (transparent)
        window.Opacity = 0.0;

        // Create fade-in animation
        var fadeInAnimation = new Animation
        {
            Duration = _fadeInDuration,
            Easing = (Easing)_fadeInEasing,
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters =
                    {
                        new Setter(Window.OpacityProperty, originalOpacity)
                    }
                }
            }
        };

        await fadeInAnimation.RunAsync(window);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not Window window) return;
        if (_animating.Contains(window)) return;
        e.Cancel = true;
        _animating.Add(window);

        // Create fade-out animation
        var fadeOutAnimation = new Animation
        {
            Duration = _fadeOutDuration,
            Easing = (Easing)_fadeOutEasing,
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters =
                    {
                        new Setter(Window.OpacityProperty, 0.0)
                    }
                }
            }
        };

        await fadeOutAnimation.RunAsync(window);
        
        // Remove event handler to avoid re-entry and close
        window.Closing -= OnClosing;
        _animating.Remove(window);
        window.Close();
    }
}
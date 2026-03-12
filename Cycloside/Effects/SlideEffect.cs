using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Media;

namespace Cycloside.Effects;

public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}

public class SlideEffect : IWindowEffect
{
    private readonly HashSet<Window> _animating = new();
    public string Name => "Slide";
    public string Description => "Slide animation for window open/close from specified direction";

    private readonly SlideDirection _openDirection;
    private readonly SlideDirection _closeDirection;
    private readonly TimeSpan _duration;
    private readonly IEasing _easing;

    public SlideEffect(
        SlideDirection openDirection = SlideDirection.Up,
        SlideDirection closeDirection = SlideDirection.Down,
        TimeSpan? duration = null,
        IEasing? easing = null)
    {
        _openDirection = openDirection;
        _closeDirection = closeDirection;
        _duration = duration ?? TimeSpan.FromMilliseconds(350);
        _easing = easing ?? new CubicEaseOut();
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

        // Store original values
        var originalOpacity = window.Opacity;
        var originalTransform = window.RenderTransform;

        // Calculate slide offset based on direction
        var (offsetX, offsetY) = GetSlideOffset(_openDirection, window);

        // Set initial state
        window.Opacity = 0.0;
        window.RenderTransform = new TranslateTransform(offsetX, offsetY);

        // Create slide-in animation
        var slideInAnimation = new Animation
        {
            Duration = _duration,
            Easing = (Easing)_easing,
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters =
                    {
                        new Setter(Window.OpacityProperty, originalOpacity),
                        new Setter(Window.RenderTransformProperty, originalTransform ?? new TranslateTransform(0, 0))
                    }
                }
            }
        };

        await slideInAnimation.RunAsync(window);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not Window window) return;
        if (_animating.Contains(window)) return;
        e.Cancel = true;
        _animating.Add(window);

        // Calculate slide offset based on direction
        var (offsetX, offsetY) = GetSlideOffset(_closeDirection, window);

        // Create slide-out animation
        var slideOutAnimation = new Animation
        {
            Duration = _duration,
            Easing = (Easing)new CubicEaseIn(),
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters =
                    {
                        new Setter(Window.OpacityProperty, 0.0),
                        new Setter(Window.RenderTransformProperty, new TranslateTransform(offsetX, offsetY))
                    }
                }
            }
        };

        await slideOutAnimation.RunAsync(window);
        
        // Remove event handler to avoid re-entry and close
        window.Closing -= OnClosing;
        _animating.Remove(window);
        window.Close();
    }

    private (double offsetX, double offsetY) GetSlideOffset(SlideDirection direction, Window window)
    {
        var screenBounds = window.Screens.Primary?.Bounds ?? new PixelRect(0, 0, 1920, 1080);
        
        return direction switch
        {
            SlideDirection.Left => (-window.Width - 50, 0),
            SlideDirection.Right => (screenBounds.Width + 50, 0),
            SlideDirection.Up => (0, -window.Height - 50),
            SlideDirection.Down => (0, screenBounds.Height + 50),
            _ => (0, 0)
        };
    }
}
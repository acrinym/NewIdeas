using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Media;

namespace Cycloside.Effects;

public class ScaleEffect : IWindowEffect
{
    public string Name => "Scale";
    public string Description => "Scale animation for window open/close with smooth zoom effect";

    public void Attach(Window window)
    {
        window.Closing += OnClosing;
        window.Opened += OnOpened;
    }

    public void Detach(Window window)
    {
        window.Closing -= OnClosing;
        window.Opened -= OnOpened;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;

        // Store original values
        var originalOpacity = window.Opacity;
        var originalScaleX = window.RenderTransform is ScaleTransform st ? st.ScaleX : 1.0;
        var originalScaleY = window.RenderTransform is ScaleTransform st2 ? st2.ScaleY : 1.0;

        // Set initial state (small and transparent)
        window.Opacity = 0.0;
        window.RenderTransform = new ScaleTransform { ScaleX = 0.3, ScaleY = 0.3 };
        window.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        // Create opening animation
        var openAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new BackEaseOut(),
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters =
                    {
                        new Setter(Window.OpacityProperty, originalOpacity),
                        new Setter(Window.RenderTransformProperty, new ScaleTransform 
                        { 
                            ScaleX = originalScaleX, 
                            ScaleY = originalScaleY 
                        })
                    }
                }
            }
        };

        await openAnimation.RunAsync(window);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not Window window) return;
        e.Cancel = true;

        // Set transform origin to center
        window.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        // Create closing animation
        var closeAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(250),
            Easing = new CubicEaseIn(),
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters =
                    {
                        new Setter(Window.OpacityProperty, 0.0),
                        new Setter(Window.RenderTransformProperty, new ScaleTransform 
                        { 
                            ScaleX = 0.1, 
                            ScaleY = 0.1 
                        })
                    }
                }
            }
        };

        await closeAnimation.RunAsync(window);
        
        // Remove event handler to avoid re-entry and close
        window.Closing -= OnClosing;
        window.Close();
    }
}
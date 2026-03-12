using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;

namespace Cycloside.Widgets.Animations;

/// <summary>
/// Extended animation utilities for widgets
/// </summary>
public static class WidgetAnimationsExtended
{
    /// <summary>
    /// Animates a widget's entrance with a slide-in effect from the specified direction
    /// </summary>
    public static async Task SlideInAsync(Control control, SlideDirection direction = SlideDirection.Bottom, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(400);
        
        var (startX, startY) = GetSlideStartPosition(direction, control);
        
        control.RenderTransform = new TranslateTransform(startX, startY);
        control.Opacity = 0;
        
        var slideAnimation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = 
                    { 
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(startX, startY)),
                        new Setter(Visual.OpacityProperty, 0.0)
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = 
                    { 
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)),
                        new Setter(Visual.OpacityProperty, 1.0)
                    },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await slideAnimation.RunAsync(control);
    }
    
    /// <summary>
    /// Animates a widget with a bounce effect
    /// </summary>
    public static async Task BounceAsync(Control control, double intensity = 1.2, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(600);
        
        var bounceAnimation = new Animation
        {
            Duration = duration,
            Easing = new BounceEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)) },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(intensity, intensity)) },
                    Cue = new Cue(0.5d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)) },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await bounceAnimation.RunAsync(control);
    }
    
    /// <summary>
    /// Animates a widget with a pulse effect
    /// </summary>
    public static async Task PulseAsync(Control control, int pulseCount = 3, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(1000);
        
        for (int i = 0; i < pulseCount; i++)
        {
            var pulseAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / pulseCount),
                Easing = new SineEaseInOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 0.5) },
                        Cue = new Cue(0.5d)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) },
                        Cue = new Cue(1d)
                    }
                }
            };
            
            await pulseAnimation.RunAsync(control);
        }
    }
    
    /// <summary>
    /// Animates a widget with a shake effect
    /// </summary>
    public static async Task ShakeAsync(Control control, double intensity = 10, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(500);
        
        var shakeAnimation = new Animation
        {
            Duration = duration,
            Easing = new LinearEasing(),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-intensity, 0)) },
                    Cue = new Cue(0.1d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(intensity, 0)) },
                    Cue = new Cue(0.2d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-intensity * 0.8, 0)) },
                    Cue = new Cue(0.3d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(intensity * 0.8, 0)) },
                    Cue = new Cue(0.4d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-intensity * 0.6, 0)) },
                    Cue = new Cue(0.5d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(intensity * 0.6, 0)) },
                    Cue = new Cue(0.6d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-intensity * 0.4, 0)) },
                    Cue = new Cue(0.7d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(intensity * 0.4, 0)) },
                    Cue = new Cue(0.8d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-intensity * 0.2, 0)) },
                    Cue = new Cue(0.9d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await shakeAnimation.RunAsync(control);
    }
    
    private static (double x, double y) GetSlideStartPosition(SlideDirection direction, Control control)
    {
        return direction switch
        {
            SlideDirection.Top => (0, -100),
            SlideDirection.Bottom => (0, 100),
            SlideDirection.Left => (-100, 0),
            SlideDirection.Right => (100, 0),
            _ => (0, 100)
        };
    }
}

/// <summary>
/// Slide direction for slide animations
/// </summary>
public enum SlideDirection
{
    Top,
    Bottom,
    Left,
    Right
}
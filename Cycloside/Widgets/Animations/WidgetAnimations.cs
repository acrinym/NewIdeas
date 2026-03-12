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
/// Provides animation utilities for widgets
/// </summary>
public static class WidgetAnimations
{
    /// <summary>
    /// Animates a widget's entrance with a fade-in effect
    /// </summary>
    public static async Task FadeInAsync(Control control, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(300);
        
        control.Opacity = 0;
        
        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await animation.RunAsync(control);
    }
    
    /// <summary>
    /// Animates a widget's exit with a fade-out effect
    /// </summary>
    public static async Task FadeOutAsync(Control control, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(300);
        
        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(Visual.OpacityProperty, control.Opacity) },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await animation.RunAsync(control);
    }
    
    /// <summary>
    /// Animates a widget's entrance with a scale-up effect
    /// </summary>
    public static async Task ScaleInAsync(Control control, TimeSpan duration = default)
    {
        if (duration == default) duration = TimeSpan.FromMilliseconds(400);
        
        control.RenderTransform = new ScaleTransform(0.8, 0.8);
        control.Opacity = 0;
        
        var scaleAnimation = new Animation
        {
            Duration = duration,
            Easing = new BackEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = 
                    { 
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(0.8, 0.8)),
                        new Setter(Visual.OpacityProperty, 0.0)
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = 
                    { 
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)),
                        new Setter(Visual.OpacityProperty, 1.0)
                    },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await scaleAnimation.RunAsync(control);
    }
    
    /// <summary>
    /// Animates a control with a pulse effect (scale up and down)
    /// </summary>
    public static async Task PulseAsync(Control control, int durationMs = 300)
    {
        var duration = TimeSpan.FromMilliseconds(durationMs);
        
        var pulseAnimation = new Animation
        {
            Duration = duration,
            Easing = new SineEaseInOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)) },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.1, 1.1)) },
                    Cue = new Cue(0.5d)
                },
                new KeyFrame
                {
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)) },
                    Cue = new Cue(1d)
                }
            }
        };
        
        await pulseAnimation.RunAsync(control);
    }
}
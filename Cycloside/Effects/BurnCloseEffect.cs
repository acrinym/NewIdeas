using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Cycloside.Effects;

public class BurnCloseEffect : IWindowEffect
{
    public string Name => "BurnClose";
    public string Description => "Close animation: slight shrink and warm fade";

    public void Attach(Window window)
    {
        window.Closing += OnClosing;
    }

    public void Detach(Window window)
    {
        window.Closing -= OnClosing;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not Window window) return;
        e.Cancel = true;

        var w = window.Width;
        var h = window.Height;
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(220),
            Easing = new QuadraticEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(1d), Setters =
                    {
                        new Setter(Window.OpacityProperty, 0.0),
                        new Setter(Window.WidthProperty, Math.Max(0, w - 60)),
                        new Setter(Window.HeightProperty, Math.Max(0, h - 40))
                    }
                }
            }
        };
        await anim.RunAsync(window);
        window.Closing -= OnClosing; // avoid re-entry
        window.Close();
    }
}
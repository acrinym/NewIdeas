using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Cycloside.Effects;

public class ExplodeCloseEffectV2 : IWindowEffect
{
    public string Name => "ExplodeCloseV2";
    public string Description => "Close animation: quick grow + fade (explode style)";

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
            Duration = TimeSpan.FromMilliseconds(180),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(1d), Setters =
                    {
                        new Setter(Window.OpacityProperty, 0.0),
                        new Setter(Window.WidthProperty, w + 20),
                        new Setter(Window.HeightProperty, h + 14)
                    }
                }
            }
        };
        await anim.RunAsync(window);
        window.Closing -= OnClosing;
        window.Close();
    }
}
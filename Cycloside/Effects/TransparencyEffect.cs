using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using System;

namespace Cycloside.Effects;

public class TransparencyEffect : IWindowEffect
{
    public string Name => "Transparency";
    public string Description => "Adjusts opacity on focus and blur";

    public void Attach(Window window)
    {
        window.GotFocus += OnFocus;
        window.LostFocus += OnBlur;
    }

    private void OnFocus(object? sender, EventArgs e)
    {
        if (sender is Window win)
            Animate(win, 1.0);
    }

    private void OnBlur(object? sender, EventArgs e)
    {
        if (sender is Window win)
            Animate(win, 0.8);
    }

    private static void Animate(Window win, double value)
    {
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = new QuadraticEaseOut(),
            Children = { new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Window.OpacityProperty, value) } } }
        };
        anim.RunAsync(win);
    }

    public void Detach(Window window)
    {
        window.GotFocus -= OnFocus;
        window.LostFocus -= OnBlur;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }
}

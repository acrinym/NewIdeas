using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Avalonia.Input;
using Avalonia.Styling;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class TransparencyEffect : IWindowEffect
{
    private readonly Dictionary<ISceneTarget, EventHandler<GotFocusEventArgs>> _focusHandlers = new();
    private readonly Dictionary<ISceneTarget, EventHandler<Avalonia.Interactivity.RoutedEventArgs>> _blurHandlers = new();

    public string Name => "Transparency";
    public string Description => "Adjusts opacity on focus and blur";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        var focusHandler = new EventHandler<GotFocusEventArgs>((s, e) => Animate(target, 1.0));
        var blurHandler = new EventHandler<Avalonia.Interactivity.RoutedEventArgs>((s, e) => Animate(target, 0.8));
        _focusHandlers[target] = focusHandler;
        _blurHandlers[target] = blurHandler;
        window.GotFocus += focusHandler;
        window.LostFocus += blurHandler;
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null || !_focusHandlers.TryGetValue(target, out var focusHandler) || !_blurHandlers.TryGetValue(target, out var blurHandler))
            return;
        window.GotFocus -= focusHandler;
        window.LostFocus -= blurHandler;
        _focusHandlers.Remove(target);
        _blurHandlers.Remove(target);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private static void Animate(ISceneTarget target, double value)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = new QuadraticEaseOut(),
            Children = { new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Window.OpacityProperty, value) } } }
        };
        anim.RunAsync(window);
    }
}

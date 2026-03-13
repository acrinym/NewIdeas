using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class ExplodeCloseEffectV2 : IWindowEffect
{
    private readonly Dictionary<ISceneTarget, EventHandler<WindowClosingEventArgs>> _handlers = new();

    public string Name => "ExplodeCloseV2";
    public string Description => "Close animation: quick grow + fade (explode style)";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        var handler = new EventHandler<WindowClosingEventArgs>((s, e) => OnClosing(target, e));
        _handlers[target] = handler;
        window.Closing += handler;
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null || !_handlers.TryGetValue(target, out var handler))
            return;
        window.Closing -= handler;
        _handlers.Remove(target);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private async void OnClosing(ISceneTarget target, WindowClosingEventArgs e)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
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
        if (_handlers.TryGetValue(target, out var closingHandler))
            window.Closing -= closingHandler;
        window.Close();
    }
}
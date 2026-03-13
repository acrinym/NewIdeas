using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class RollUpEffect : IWindowEffect
{
    private readonly Dictionary<Window, double> _heights = new();

    public string Name => "RollUp";
    public string Description => "Collapses the window to its titlebar";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        window.PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not Window win)
            return;

        if (!_heights.ContainsKey(win))
            _heights[win] = win.Height;

        var target = Math.Abs(win.Height - win.MinHeight) < 1 ? _heights[win] : win.MinHeight;
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = new QuadraticEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(Window.HeightProperty, target) }
                }
            }
        };
        anim.RunAsync(win);
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        window.PointerPressed -= OnPointerPressed;
        _heights.Remove(window);
    }
}

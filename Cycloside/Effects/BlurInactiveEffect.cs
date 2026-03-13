using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class BlurInactiveEffect : IWindowEffect
{
    private readonly Dictionary<ISceneTarget, EventHandler> _activatedHandlers = new();
    private readonly Dictionary<ISceneTarget, EventHandler> _deactivatedHandlers = new();

    public string Name => "BlurInactive";
    public string Description => "Applies blur when the window is inactive";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        var activatedHandler = new EventHandler(OnActivated);
        var deactivatedHandler = new EventHandler(OnDeactivated);
        _activatedHandlers[target] = activatedHandler;
        _deactivatedHandlers[target] = deactivatedHandler;
        window.Activated += activatedHandler;
        window.Deactivated += deactivatedHandler;
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null || !_activatedHandlers.TryGetValue(target, out var activatedHandler) || !_deactivatedHandlers.TryGetValue(target, out var deactivatedHandler))
            return;
        window.Activated -= activatedHandler;
        window.Deactivated -= deactivatedHandler;
        window.Effect = null;
        _activatedHandlers.Remove(target);
        _deactivatedHandlers.Remove(target);
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (sender is Window win && win.Effect is BlurEffect)
            win.Effect = null;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (sender is Window win)
            win.Effect = new BlurEffect { Radius = 7 };
    }
}
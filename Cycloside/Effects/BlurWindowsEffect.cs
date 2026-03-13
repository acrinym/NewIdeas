using Avalonia.Controls;
using Avalonia.Media;
using Cycloside.Scene;

namespace Cycloside.Effects;

public class BlurWindowsEffect : IWindowEffect
{
    public string Name => "BlurWindows";
    public string Description => "Always-on blur applied to the window";

    public void Attach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        window.Effect = new BlurEffect { Radius = 6 };
    }

    public void Detach(ISceneTarget target)
    {
        var window = EffectTargetHelper.GetWindow(target);
        if (window == null) return;
        if (window.Effect is BlurEffect)
            window.Effect = null;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }
}
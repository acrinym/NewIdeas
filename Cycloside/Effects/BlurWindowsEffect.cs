using Avalonia.Controls;
using Avalonia.Media;

namespace Cycloside.Effects;

public class BlurWindowsEffect : IWindowEffect
{
    public string Name => "BlurWindows";
    public string Description => "Always-on blur applied to the window";

    public void Attach(Window window)
    {
        window.Effect = new BlurEffect { Radius = 6 };
    }

    public void Detach(Window window)
    {
        if (window.Effect is BlurEffect)
            window.Effect = null;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }
}
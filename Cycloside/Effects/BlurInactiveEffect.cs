using Avalonia.Controls;
using Avalonia.Media;

namespace Cycloside.Effects;

public class BlurInactiveEffect : IWindowEffect
{
    public string Name => "BlurInactive";
    public string Description => "Applies blur when the window is inactive";

    public void Attach(Window window)
    {
        window.Activated += OnActivated;
        window.Deactivated += OnDeactivated;
    }

    public void Detach(Window window)
    {
        window.Activated -= OnActivated;
        window.Deactivated -= OnDeactivated;
        window.Effect = null;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void OnActivated(object? sender, System.EventArgs e)
    {
        if (sender is Window win)
        {
            if (win.Effect is BlurEffect)
                win.Effect = null;
        }
    }

    private void OnDeactivated(object? sender, System.EventArgs e)
    {
        if (sender is Window win)
        {
            win.Effect = new BlurEffect { Radius = 7 };
        }
    }
}
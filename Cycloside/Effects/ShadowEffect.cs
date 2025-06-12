using Avalonia.Controls;
using Avalonia.Media;

namespace Cycloside.Effects;

public class ShadowEffect : IWindowEffect
{
    public string Name => "Shadow";
    public string Description => "Adds a simple drop shadow";

    public void Attach(Window window)
    {
        if (window.Effect is not DropShadowEffect)
        {
            window.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                Color = Colors.Black,
                Opacity = 0.5
            };
        }
    }

    public void Detach(Window window)
    {
        if (window.Effect is DropShadowEffect)
            window.Effect = null;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }
}

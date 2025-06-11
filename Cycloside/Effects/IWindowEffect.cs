using Avalonia.Controls;

namespace Cycloside.Effects;

public interface IWindowEffect
{
    string Name { get; }
    string Description { get; }

    void Attach(Window window);
    void Detach(Window window);
    void ApplyEvent(WindowEventType type, object? args);
}

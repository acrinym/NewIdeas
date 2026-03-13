using Avalonia.Controls;
using Cycloside.Scene;

namespace Cycloside.Effects;

public interface IWindowEffect
{
    string Name { get; }
    string Description { get; }

    void Attach(ISceneTarget target);
    void Detach(ISceneTarget target);
}

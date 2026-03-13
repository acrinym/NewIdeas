using Avalonia;
using Avalonia.Threading;

namespace Cycloside.Scene
{
    /// <summary>
    /// Abstraction for effect targets. Enables effects on Window, SceneNode, or future surfaces.
    /// </summary>
    public interface ISceneTarget
    {
        PixelRect Bounds { get; }
        PixelPoint Position { get; set; }
        double Opacity { get; set; }
        bool IsVisible { get; }
        IDispatcher? Dispatcher { get; }
    }
}

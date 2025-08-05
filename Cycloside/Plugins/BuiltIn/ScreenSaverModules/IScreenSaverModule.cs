using Avalonia;
using Avalonia.Media;

namespace Cycloside.Plugins.BuiltIn.ScreenSaverModules
{
    internal interface IScreenSaverModule
    {
        string Name { get; }
        void Update();
        void Render(DrawingContext context, Rect bounds);
    }
}

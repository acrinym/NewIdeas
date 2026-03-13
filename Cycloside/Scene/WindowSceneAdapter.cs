using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Cycloside.Scene
{
    /// <summary>
    /// Adapts Avalonia Window to ISceneTarget for effect compatibility.
    /// </summary>
    public class WindowSceneAdapter : ISceneTarget
    {
        private readonly Window _window;

        private WindowSceneAdapter(Window window)
        {
            _window = window;
        }

        /// <summary>
        /// Underlying Window. Used by effects that need to subscribe to window events (e.g. Opened).
        /// </summary>
        public Window Window => _window;

        public static WindowSceneAdapter CreateFrom(Window window)
        {
            return new WindowSceneAdapter(window);
        }

        private const int MinBoundsSize = 1;
        public PixelRect Bounds => new PixelRect(_window.Position, new PixelSize(Math.Max(MinBoundsSize, (int)_window.ClientSize.Width), Math.Max(MinBoundsSize, (int)_window.ClientSize.Height)));
        public PixelPoint Position { get => _window.Position; set => _window.Position = value; }
        public double Opacity { get => _window.Opacity; set => _window.Opacity = value; }
        public bool IsVisible => _window.IsVisible;
        public IDispatcher? Dispatcher => Avalonia.Threading.Dispatcher.UIThread;
    }
}

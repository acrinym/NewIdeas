using System;
using Avalonia;
using Avalonia.Media;

namespace Cycloside.Plugins.BuiltIn.ScreenSaverModules
{
    internal class BouncingBallAnimation : IScreenSaverModule
    {
        private double _x;
        private double _y;
        private double _vx;
        private double _vy;
        private readonly Random _random = new();
        public string Name => "BouncingBall";

        public BouncingBallAnimation()
        {
            _x = _random.NextDouble();
            _y = _random.NextDouble();
            _vx = (_random.NextDouble() - 0.5) * 0.02;
            _vy = (_random.NextDouble() - 0.5) * 0.02;
        }

        public void Update()
        {
            _x += _vx;
            _y += _vy;
            if (_x < 0 || _x > 1) _vx = -_vx;
            if (_y < 0 || _y > 1) _vy = -_vy;
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            double radius = Math.Min(bounds.Width, bounds.Height) * 0.05;
            var cx = bounds.X + _x * bounds.Width;
            var cy = bounds.Y + _y * bounds.Height;
            var brush = Brushes.CornflowerBlue;
            context.DrawEllipse(brush, null, new Point(cx, cy), radius, radius);
        }
    }
}

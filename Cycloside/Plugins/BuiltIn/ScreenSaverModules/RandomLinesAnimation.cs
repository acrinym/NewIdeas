using System;
using Avalonia;
using Avalonia.Media;

namespace Cycloside.Plugins.BuiltIn.ScreenSaverModules
{
    internal class RandomLinesAnimation : IScreenSaverModule
    {
        private readonly Random _random = new();
        public string Name => "RandomLines";

        public void Update()
        {
            // Stateless animation; nothing to update
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            for (int i = 0; i < 50; i++)
            {
                var p1 = new Point(bounds.X + _random.NextDouble() * bounds.Width,
                                   bounds.Y + _random.NextDouble() * bounds.Height);
                var p2 = new Point(bounds.X + _random.NextDouble() * bounds.Width,
                                   bounds.Y + _random.NextDouble() * bounds.Height);
                var color = Color.FromArgb(255,
                    (byte)_random.Next(256),
                    (byte)_random.Next(256),
                    (byte)_random.Next(256));
                var pen = new Pen(new SolidColorBrush(color), 1);
                context.DrawLine(pen, p1, p2);
            }
        }
    }
}

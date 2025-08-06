using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

// Ported from XScreenSaver's "deco" module.
// Draws recursively subdivided rectangles with random colors.
// This is a simplified C# implementation for the Cycloside screensaver host.

namespace Cycloside.Plugins.BuiltIn.ScreenSaverModules
{
    internal class DecoAnimation : IScreenSaverModule
    {
        private readonly Random _random = new();
        private readonly List<Rect> _rects = new();
        private readonly IBrush[] _palette =
        {
            Brushes.Red,
            Brushes.Yellow,
            Brushes.Blue,
            Brushes.White,
            Brushes.Black
        };

        public string Name => "Deco";


        public void Update()
        {
            _rects.Clear();
            Subdivide(new Rect(0, 0, 1, 1), 0);
        }

        private void Subdivide(Rect r, int depth)
        {
            if (depth > 4 || r.Width < 0.05 || r.Height < 0.05)
            {
                _rects.Add(r);
                return;
            }

            bool vertical = _random.NextDouble() < 0.5;
            double split = 0.2 + _random.NextDouble() * 0.6;

            if (vertical)
            {
                double w = r.Width * split;
                var left = new Rect(r.X, r.Y, w, r.Height);
                var right = new Rect(r.X + w, r.Y, r.Width - w, r.Height);
                Subdivide(left, depth + 1);
                Subdivide(right, depth + 1);
            }
            else
            {
                double h = r.Height * split;
                var top = new Rect(r.X, r.Y, r.Width, h);
                var bottom = new Rect(r.X, r.Y + h, r.Width, r.Height - h);
                Subdivide(top, depth + 1);
                Subdivide(bottom, depth + 1);
            }
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            foreach (var r in _rects)
            {
                var rect = new Rect(
                    bounds.X + r.X * bounds.Width,
                    bounds.Y + r.Y * bounds.Height,
                    r.Width * bounds.Width,
                    r.Height * bounds.Height);
                var brush = _palette[_random.Next(_palette.Length)];
                context.FillRectangle(brush, rect);
            }
        }
    }
}

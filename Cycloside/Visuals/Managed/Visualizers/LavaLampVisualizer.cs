using System;
using Avalonia;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class LavaLampVisualizer : IManagedVisualizer
{
    private sealed class Blob { public Point P; public Point V; public double R; }
    private readonly Random _rng = new();
    private Blob[] _blobs = Array.Empty<Blob>();
    private double _amp;

    public string Name => "Lava Lamp";
    public string Description => "Metaball-like blobs with gradients";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        double sum = 0; int count = Math.Min(96, data.Spectrum.Length / 2);
        for (int i = 0; i < count; i++) sum += data.Spectrum[i];
        _amp = Math.Clamp(sum / (count * 255.0), 0, 1);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w <= 0 || h <= 0) return;

        // Background
        var bg = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0,0,RelativeUnit.Relative),
            EndPoint = new RelativePoint(0,1,RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(18,6,6), 0),
                new GradientStop(Color.FromRgb(6,2,2), 1)
            }
        };
        ctx.FillRectangle(bg, new Rect(0,0,w,h));

        // Init blobs
        if (_blobs.Length == 0)
        {
            int n = 8;
            _blobs = new Blob[n];
            for (int i = 0; i < n; i++)
            {
                _blobs[i] = new Blob
                {
                    P = new Point(_rng.NextDouble()*w, _rng.NextDouble()*h),
                    V = new Point((_rng.NextDouble()-0.5)*40, (_rng.NextDouble()-0.5)*40),
                    R = Math.Min(w,h) * (0.06 + _rng.NextDouble()*0.12)
                };
            }
        }

        // Update and draw
        foreach (var b in _blobs)
        {
            var speed = 1 + _amp * 2;
            b.P = new Point((b.P.X + b.V.X * 0.033 * speed + w) % w,
                            (b.P.Y + b.V.Y * 0.033 * speed + h) % h);

            var grad = new RadialGradientBrush
            {
                GradientOrigin = new RelativePoint(0.5,0.5,RelativeUnit.Relative),
                Center = new RelativePoint(0.5,0.5,RelativeUnit.Relative),
                RadiusX = new RelativeScalar(1, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromArgb(220, 255, 60, 0), 0),
                    new GradientStop(Color.FromArgb(160, 255, 120, 0), 0.4),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 1)
                }
            };

            var geo = new EllipseGeometry(new Rect(b.P.X - b.R, b.P.Y - b.R, 2*b.R, 2*b.R));
            ctx.DrawGeometry(grad, null, geo);
        }
    }
}

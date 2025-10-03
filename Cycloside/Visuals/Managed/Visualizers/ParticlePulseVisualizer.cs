using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class ParticlePulseVisualizer : IManagedVisualizer
{
    private sealed class P { public Point Pos; public Point Vel; public double Life; public double MaxLife; }
    private readonly List<P> _ps = new();
    private readonly Random _rng = new();
    private double _amp;

    public string Name => "Particle Pulse";
    public string Description => "Center bursts of particles on beats";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        double sum = 0; int count = Math.Min(48, data.Spectrum.Length / 2);
        for (int i = 0; i < count; i++) sum += data.Spectrum[i];
        _amp = Math.Clamp(sum / (count * 255.0), 0, 1);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w <= 0 || h <= 0) return;
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0, 0, w, h));

        // Spawn proportional to amp
        int spawn = (int)(2 + _amp * 40);
        for (int i = 0; i < spawn; i++)
        {
            var ang = _rng.NextDouble() * Math.PI * 2;
            var speed = 30 + _rng.NextDouble() * 220 * (0.3 + _amp);
            _ps.Add(new P
            {
                Pos = new Point(w / 2, h / 2),
                Vel = new Point(Math.Cos(ang) * speed, Math.Sin(ang) * speed),
                Life = 0,
                MaxLife = 0.8 + _rng.NextDouble() * 1.5
            });
        }

        // integrate
        double dt = 0.033; // approx
        for (int i = _ps.Count - 1; i >= 0; i--)
        {
            var p = _ps[i];
            p.Life += dt;
            if (p.Life >= p.MaxLife) { _ps.RemoveAt(i); continue; }
            var drag = 0.98;
            p.Vel = new Point(p.Vel.X * drag, p.Vel.Y * drag);
            p.Pos = new Point(p.Pos.X + p.Vel.X * dt, p.Pos.Y + p.Vel.Y * dt);
        }

        // draw
        foreach (var p in _ps)
        {
            double t = p.Life / p.MaxLife; // 0..1
            byte a = (byte)(220 * (1 - t));
            var ac = ManagedVisStyle.Accent().Color;
            var color = Color.FromArgb(a, ac.R, ac.G, ac.B);
            var brush = new SolidColorBrush(color);
            double r = 2 + 6 * (1 - t);
            var geo = new EllipseGeometry(new Rect(p.Pos.X - r, p.Pos.Y - r, 2 * r, 2 * r));
            ctx.DrawGeometry(brush, null, geo);
        }
    }
}

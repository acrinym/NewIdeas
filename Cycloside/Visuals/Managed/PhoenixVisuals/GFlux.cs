using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Audio-reactive particle flow field. Particles emit from center/bands and swirl.
/// </summary>
public sealed class GFlux : IVisualizerPlugin
{
    public string Id => "gflux";
    public string DisplayName => "G-Flux";

    private int _w, _h;
    private Particle[] _ps = Array.Empty<Particle>();
    private int _count = 1500;
    private readonly Random _rng = new();
    private float _time;

    public void Initialize(int width, int height) { _w = width; _h = height; Allocate(); }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        _time += 0.016f;
        if (_ps.Length != _count) Allocate();

        // Clear the previous frame to avoid persistent streaks across the viewport
        canvas.Clear(0xFF000000);

        // Audio-driven parameters
        float speed = 40f + f.Mid * 160f;      // movement speed
        float swirl = 0.8f + f.Treble * 2.2f;  // flow curl
        float emit = 10f + f.Energy * 200f;    // emission rate

        // Emit a few new particles near center each frame; more on beat
        int emitN = (int)emit + (f.Beat ? 120 : 0);
        for (int e = 0; e < emitN; e++) Spawn(f);

        // Update + draw
        for (int i = 0; i < _ps.Length; i++)
        {
            ref var p = ref _ps[i];
            if (p.life <= 0) continue;

            // Save old position for streak
            float ox = p.x, oy = p.y;

            // Flow field: curl based on sin/cos of scaled coordinates and time
            float nx = (p.x / _w - 0.5f) * 2f;
            float ny = (p.y / _h - 0.5f) * 2f;
            float a = MathF.Sin(nx * 3.1f + _time * 0.7f) + MathF.Cos(ny * 3.7f - _time * 0.6f);
            float b = MathF.Cos(nx * 2.9f - _time * 0.8f) - MathF.Sin(ny * 2.3f + _time * 0.5f);
            float vx = a * swirl;
            float vy = b * swirl;

            // Add bias from FFT bands to create outward pulses
            vx += (nx) * (0.2f + f.Bass * 0.5f);
            vy += (ny) * (0.2f + f.Bass * 0.5f);

            p.vx = p.vx * 0.92f + vx * 0.08f;
            p.vy = p.vy * 0.92f + vy * 0.08f;

            p.x += p.vx * speed * 0.016f;
            p.y += p.vy * speed * 0.016f;
            p.life--;

            // Wrap around edges for continuous motion
            bool wrapped = false;
            if (p.x < -10) { p.x = _w + 10; wrapped = true; } else if (p.x > _w + 10) { p.x = -10; wrapped = true; }
            if (p.y < -10) { p.y = _h + 10; wrapped = true; } else if (p.y > _h + 10) { p.y = -10; wrapped = true; }

            // Color from treble and cycle
            byte r = (byte)Math.Clamp((int)((0.4f + f.Treble * 0.6f) * 255), 0, 255);
            byte g = (byte)Math.Clamp((int)((0.5f + f.Mid * 0.5f) * 255), 0, 255);
            byte bch = (byte)Math.Clamp((int)((0.8f - f.Bass * 0.6f) * 255), 0, 255);
            uint col = 0xFF000000u | (uint)(r << 16) | (uint)(g << 8) | bch;

            // Draw a streak from old to new position; if we wrapped (large jump), draw a point instead
            float dx = MathF.Abs(p.x - ox), dy = MathF.Abs(p.y - oy);
            if (!wrapped && dx < _w * 0.5f && dy < _h * 0.5f)
                canvas.DrawLine(ox, oy, p.x, p.y, col, 1.2f);
            else
                canvas.FillCircle(p.x, p.y, 1.5f, col);
        }
    }

    private void Allocate()
    {
        _ps = new Particle[_count];
        for (int i = 0; i < _count; i++) _ps[i].life = 0;
    }

    private void Spawn(IAudioFeatures f)
    {
        // Find a dead slot
        for (int i = 0; i < _ps.Length; i++)
        {
            if (_ps[i].life > 0) continue;
            ref var p = ref _ps[i];
            // Emit near center with slight random offset; bias radius by energy
            float cx = _w * 0.5f, cy = _h * 0.5f;
            float rad = (float)_rng.NextDouble() * (10f + f.Energy * 60f);
            float ang = (float)_rng.NextDouble() * MathF.PI * 2f;
            p.x = cx + MathF.Cos(ang) * rad;
            p.y = cy + MathF.Sin(ang) * rad;
            p.vx = (float)(_rng.NextDouble() * 2 - 1) * 0.1f;
            p.vy = (float)(_rng.NextDouble() * 2 - 1) * 0.1f;
            p.life = 120 + _rng.Next(240);
            return;
        }
    }

    private struct Particle
    {
        public float x, y, vx, vy; public int life;
    }
}

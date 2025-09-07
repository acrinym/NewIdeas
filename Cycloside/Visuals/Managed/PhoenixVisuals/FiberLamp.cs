using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Fiber optic lamp effect: many flexible fibers arcing from a base, swaying with audio.
/// </summary>
public sealed class FiberLamp : IVisualizerPlugin
{
    public string Id => "fiber_lamp";
    public string DisplayName => "Fiber Lamp";

    private int _w, _h;
    private Fiber[] _fibers = Array.Empty<Fiber>();
    private readonly Random _rng = new();
    private float _t;

    public void Initialize(int width, int height)
    {
        _w = width; _h = height; Allocate(140);
    }
    public void Resize(int width, int height) { _w = width; _h = height; if (_fibers.Length == 0) Allocate(140); }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _t += 0.016f;

        // Base position at bottom center
        float bx = _w * 0.5f;
        float by = _h * 0.92f;

        // Audio controls
        float amp = 0.2f + f.Energy * 0.8f;          // sway amplitude
        float len = MathF.Min(_h * 0.6f, _h * (0.45f + f.Bass * 0.35f));
        float speed = 0.6f + f.Mid * 2.0f;           // sway speed

        // Preallocate polyline buffer
        const int Segs = 22;
        var pts = new (float x, float y)[Segs + 1];

        for (int i = 0; i < _fibers.Length; i++)
        {
            ref var fb = ref _fibers[i];
            // individual sway phases
            float sway = MathF.Sin((_t + fb.phase) * speed) * amp;
            // fiber base angle around semi-circle
            float baseAng = fb.baseAngle + sway * 0.6f;
            // per-fiber length variation
            float L = len * (0.65f + fb.lengthJitter * 0.35f);

            // Build a simple cubic-like arc via points; add local noise
            for (int s = 0; s <= Segs; s++)
            {
                float t = s / (float)Segs; // 0..1 along fiber
                // radial outwards from base angle, then bend down slightly by gravity
                float ang = baseAng + (t * 0.6f - 0.3f) * sway * 0.8f;
                float r = L * t;
                float x = bx + MathF.Cos(ang) * r;
                float y = by - MathF.Sin(ang) * r + t * t * 18f; // gravity term
                // tiny per-fiber, per-segment jitter
                float j = (float)(_rng.NextDouble() - 0.5) * (1.0f - t) * 0.6f;
                x += j;
                y += j * 0.4f;
                pts[s] = (x, y);
            }

            // color from treble + fiber index
            float hue = (i / (float)_fibers.Length) * 360f + f.Treble * 140f;
            uint col = HsvToRgb(hue % 360f, 0.8f, 0.9f);
            // thinner near base, thicker near tips
            float lw = 0.6f + amp * 1.2f;
            canvas.DrawLines(pts, lw, col);

            // bright tip
            var tip = pts[^1];
            canvas.FillCircle(tip.x, tip.y, 2.2f + f.Treble * 2.0f, 0xFFFFFFFF);
        }
    }

    private void Allocate(int count)
    {
        _fibers = new Fiber[count];
        for (int i = 0; i < count; i++)
        {
            // spread around ~160 degrees fan
            float spread = MathF.PI * 0.9f;
            float baseAng = (i / (float)(count - 1)) * spread + (MathF.PI - spread) * 0.5f;
            _fibers[i] = new Fiber
            {
                baseAngle = baseAng,
                phase = (float)_rng.NextDouble() * MathF.PI * 2f,
                lengthJitter = (float)_rng.NextDouble(),
            };
        }
    }

    private struct Fiber { public float baseAngle, phase, lengthJitter; }

    private static uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s, x = c * (1f - Math.Abs((h / 60f) % 2f - 1f)), m = v - c;
        float r, g, b;
        if (h < 60f) { r = c; g = x; b = 0f; }
        else if (h < 120f) { r = x; g = c; b = 0f; }
        else if (h < 180f) { r = 0f; g = c; b = x; }
        else if (h < 240f) { r = 0f; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }
        byte R = (byte)((r + m) * 255f); byte G = (byte)((g + m) * 255f); byte B = (byte)((b + m) * 255f);
        return (uint)(0xFF << 24 | R << 16 | G << 8 | B);
    }
}


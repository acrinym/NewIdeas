using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Audio-reactive raver hoop with trails and color cycling.
/// </summary>
public sealed class RaverHoop : IVisualizerPlugin
{
    public string Id => "raver_hoop";
    public string DisplayName => "Raver Hoop";

    private int _w, _h;
    private float _angle;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        float cx = _w * 0.5f, cy = _h * 0.5f; float maxR = MathF.Min(_w, _h) * 0.45f;
        float radius = maxR * (0.5f + f.Bass * 0.5f);
        _angle += 0.02f + f.Mid * 0.2f + (f.Beat ? 0.1f : 0f);

        int trails = 20;
        for (int t = 0; t < trails; t++)
        {
            float a = _angle - t * (0.08f + f.Treble * 0.04f);
            float x = cx + MathF.Cos(a) * radius;
            float y = cy + MathF.Sin(a) * radius;
            float hue = (a * 180f / MathF.PI) % 360f;
            uint col = HsvToRgb((hue + t * 6f + f.Treble * 60f) % 360f, 1f, MathF.Max(0.2f, 1f - t / (float)trails));
            // draw a small arc segment approximated by short line
            float a2 = a + 0.12f;
            float x2 = cx + MathF.Cos(a2) * radius;
            float y2 = cy + MathF.Sin(a2) * radius;
            canvas.DrawLine(x, y, x2, y2, col, 4f);
        }
    }

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


using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Classic meshing gears; audio drives rotation speeds and color.
/// </summary>
public sealed class Gears : IVisualizerPlugin
{
    public string Id => "gears";
    public string DisplayName => "Gears";

    private int _w, _h;
    private float _a1, _a2, _a3;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        // Update angles based on bands
        _a1 += 0.02f + f.Bass * 0.25f;
        _a2 -= 0.018f + f.Mid * 0.2f; // opposite direction
        _a3 += 0.015f + f.Treble * 0.3f;

        float cx = _w * 0.5f, cy = _h * 0.5f; float R = MathF.Min(_w, _h) * 0.18f;
        DrawGear(canvas, cx - R * 1.5f, cy, R * 1.00f, 12, _a1, HsvToRgb(20 + f.Bass * 60, 1, 0.9f));
        DrawGear(canvas, cx + R * 1.5f, cy, R * 0.85f, 14, _a2, HsvToRgb(180 + f.Mid * 60, 1, 0.9f));
        DrawGear(canvas, cx, cy - R * 1.7f, R * 0.70f, 10, _a3, HsvToRgb(300 + f.Treble * 60, 1, 0.9f));
    }

    private static void DrawGear(ISimpleCanvas c, float cx, float cy, float r, int teeth, float angle, uint color)
    {
        int segs = teeth * 8; // fine sampling
        var pts = new (float x, float y)[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            float a = t * MathF.PI * 2f + angle;
            // Radial modulation for teeth (simple square-ish wave)
            float tooth = MathF.Sin(a * teeth * 0.5f);
            float rad = r * (1.0f + 0.18f * MathF.Sign(tooth));
            pts[i] = (cx + MathF.Cos(a) * rad, cy + MathF.Sin(a) * rad);
        }
        c.DrawLines(pts, 3.0f, color);
        // inner hub
        c.DrawCircle(cx, cy, r * 0.25f, 0xFFFFFFFF, false);
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


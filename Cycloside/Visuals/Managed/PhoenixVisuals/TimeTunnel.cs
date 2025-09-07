using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Radial tunnel with audio-driven speed/warp/rotation.
/// </summary>
public sealed class TimeTunnel : IVisualizerPlugin
{
    public string Id => "time_tunnel";
    public string DisplayName => "Time Tunnel";

    private int _w, _h;
    private float _z, _rot;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);

        // Advance camera based on bass energy; rotate via mids
        float speed = 0.08f + f.Bass * 0.8f + (f.Beat ? 0.2f : 0f);
        _z += speed;
        _rot += 0.01f + f.Mid * 0.2f;

        // Tunnel parameters
        int rings = 28;            // number of rings visible
        int spokes = 36;           // lines per ring
        float cx = _w * 0.5f;
        float cy = _h * 0.5f;
        float maxR = MathF.Min(_w, _h) * 0.48f;

        // Draw concentric rings with perspective
        var ring = new (float x, float y)[spokes + 1];
        for (int r = 0; r < rings; r++)
        {
            float z = r + (_z % 1f);
            float scale = 1f / (0.5f + z * 0.15f);        // farther rings are smaller
            float radius = maxR * scale;
            float alpha = (float)Math.Clamp(1.0 - (double)z / rings, 0.0, 1.0) * (0.4f + f.Energy * 0.6f);
            uint col = PackColor(0.2f + f.Treble * 0.6f, 0.8f, alpha);

            // Build a ring polygon
            for (int s = 0; s <= spokes; s++)
            {
                float a = (s / (float)spokes) * MathF.PI * 2f + _rot;
                ring[s] = (cx + MathF.Cos(a) * radius, cy + MathF.Sin(a) * radius);
            }
            canvas.DrawLines(ring, 1.2f, col);
        }

        // Draw radial spokes with hue sweep based on treble
        int radial = 24;
        for (int i = 0; i < radial; i++)
        {
            float a = (i / (float)radial) * MathF.PI * 2f + _rot * 0.7f;
            float x1 = cx + MathF.Cos(a) * maxR * 0.05f;
            float y1 = cy + MathF.Sin(a) * maxR * 0.05f;
            float x2 = cx + MathF.Cos(a) * maxR;
            float y2 = cy + MathF.Sin(a) * maxR;
            float hue = (i / (float)radial) * 360f;
            uint col = HsvToRgb(hue, 0.9f, 0.7f + f.Treble * 0.3f);
            canvas.DrawLine(x1, y1, x2, y2, col, 1.5f);
        }
    }

    private static uint PackColor(float g, float b, float a)
    {
        byte A = (byte)Math.Clamp((int)(a * 255), 0, 255);
        byte G = (byte)Math.Clamp((int)(g * 255), 0, 255);
        byte B = (byte)Math.Clamp((int)(b * 255), 0, 255);
        return (uint)(A << 24 | 0x00 << 16 | G << 8 | B);
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

using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Quasicrystal interference pattern approximated on CPU; audio drives scale/phase/color.
/// </summary>
public sealed class QuasiCrystal : IVisualizerPlugin
{
    public string Id => "quasicrystal";
    public string DisplayName => "QuasiCrystal";

    private int _w, _h;
    private float _phase;
    private const int Waves = 5;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        // Solid backdrop (dark)
        canvas.Clear(0xFF000000);

        // Audio-driven parameters
        _phase += 0.03f + f.Treble * 0.2f;
        float scale = 2.0f + f.Mid * 10.0f;
        float amp = 0.5f + f.Bass * 0.8f;

        int rows = 120; // downsampled grid rows
        int cols = 160; // downsampled grid cols
        float dx = _w / (float)(cols - 1);
        float dy = _h / (float)(rows - 1);

        // Precompute wave directions
        Span<(float x,float y)> dir = stackalloc (float,float)[Waves];
        for (int k = 0; k < Waves; k++)
        {
            float a = k * (MathF.PI * 2f) / Waves + _phase * 0.2f;
            dir[k] = (MathF.Cos(a), MathF.Sin(a));
        }

        // Plot rows as polylines with color from magnitude
        var line = new (float x, float y)[cols];
        for (int r = 0; r < rows; r++)
        {
            float y = r * dy;
            for (int c = 0; c < cols; c++)
            {
                float x = c * dx;
                // Map to centered coordinates
                float nx = (x / _w - 0.5f) * scale;
                float ny = (y / _h - 0.5f) * scale;
                float s = 0;
                for (int k = 0; k < Waves; k++)
                {
                    var d = dir[k];
                    s += MathF.Cos(nx * d.x * 3.0f + ny * d.y * 3.0f + _phase);
                }
                s = (s / Waves) * amp;
                float yy = y + s * 18.0f; // displace vertically
                line[c] = (x, yy);
            }
            float hue = (r / (float)rows) * 360f + f.Treble * 90f;
            uint col = HsvToRgb(hue % 360f, 0.9f, 0.8f);
            canvas.DrawLines(line, 1.0f, col);
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


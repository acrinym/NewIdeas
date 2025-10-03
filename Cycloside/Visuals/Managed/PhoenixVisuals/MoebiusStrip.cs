using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Parametric Möbius strip with audio-driven rotation and tint.
/// </summary>
public sealed class MoebiusStrip : IVisualizerPlugin
{
    public string Id => "moebius_strip";
    public string DisplayName => "Möbius Strip";

    private int _w, _h;
    private float _ax, _ay, _az; // rotation angles

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        // Clear background
        canvas.Clear(0xFF000000);

        // Audio-driven rotations
        _ax += 0.01f + f.Bass * 0.1f;
        _ay += 0.013f + f.Mid * 0.1f;
        _az += 0.008f + f.Treble * 0.1f;

        // Möbius parameters
        float R = 1.2f;             // radius of central circle
        float W = 0.4f + f.Energy * 0.3f; // half-width of strip
        int vBands = 18;            // cross bands
        int uSegs = 160;           // segments around ring

        // Build ring polylines for each cross band v
        var line = new (float x, float y)[uSegs + 1];
        for (int bi = 0; bi < vBands; bi++)
        {
            // v goes from -W..W; spread across bands
            float v = ((bi / (float)(vBands - 1)) - 0.5f) * 2f * W;
            for (int si = 0; si <= uSegs; si++)
            {
                float u = (si / (float)uSegs) * (MathF.PI * 2f);
                // Möbius strip param eqs
                float cu2 = MathF.Cos(u * 0.5f);
                float su2 = MathF.Sin(u * 0.5f);
                float cu = MathF.Cos(u);
                float su = MathF.Sin(u);
                float x = (R + v * cu2) * cu;
                float y = (R + v * cu2) * su;
                float z = v * su2;
                // Rotate and project
                Project(Rotate(x, y, z, _ax, _ay, _az), out var px, out var py);
                line[si] = (px, py);
            }
            // Color varies along band index and treble
            float hue = (bi / (float)vBands) * 300f + f.Treble * 100f;
            uint col = HsvToRgb(hue % 360f, 0.8f, 0.9f);
            canvas.DrawLines(line, 1.5f, col);
        }
    }

    private static (float x, float y, float z) Rotate(float x, float y, float z, float ax, float ay, float az)
    {
        float cx = MathF.Cos(ax), sx = MathF.Sin(ax);
        float cy = MathF.Cos(ay), sy = MathF.Sin(ay);
        float cz = MathF.Cos(az), sz = MathF.Sin(az);
        // Z rotation
        float xz = x * cz - y * sz; float yz = x * sz + y * cz; float zz = z;
        // Y rotation
        float xy = xz * cy + zz * sy; float zy = -xz * sy + zz * cy; float yy = yz;
        // X rotation
        float yx = yy * cx - zy * sx; float zx = yy * sx + zy * cx; float xx = xy;
        return (xx, yx, zx);
    }

    private void Project((float x, float y, float z) p, out float sx, out float sy)
    {
        float f = 3.0f; // focal length
        float s = f / (f + p.z);
        sx = _w * 0.5f + p.x * s * _w * 0.22f;
        sy = _h * 0.5f + p.y * s * _h * 0.22f;
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


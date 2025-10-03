using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class RainbowMerkabaSuperscope : IVisualizerPlugin
{
    public string Id => "rainbow_merkaba";
    public string DisplayName => "Rainbow Merkaba";

    private int _w, _h;
    private int _n = 720;
    private float _rot;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _rot += 0.02f + features.Energy * 0.2f;
        Span<(float x, float y)> pts = stackalloc (float, float)[_n];
        for (int k = 0; k < _n; k++)
        {
            float i = k / (float)_n;
            int edge = (int)MathF.Floor(i * 12);
            float t = i * 12 - edge;
            (float x1, float y1, float z1, float x2, float y2, float z2) = edge switch
            {
                0 => (1, 1, 1, -1, 1, 1),
                1 => (-1, 1, 1, -1, -1, 1),
                2 => (-1, -1, 1, 1, -1, 1),
                3 => (1, -1, 1, 1, 1, 1),
                4 => (1, 1, -1, -1, 1, -1),
                5 => (-1, 1, -1, -1, -1, -1),
                6 => (-1, -1, -1, 1, -1, -1),
                7 => (1, -1, -1, 1, 1, -1),
                8 => (1, 1, 1, 1, 1, -1),
                9 => (-1, 1, 1, -1, 1, -1),
                10 => (-1, -1, 1, -1, -1, -1),
                _ => (1, -1, 1, 1, -1, -1)
            };
            float x = (x2 - x1) * t + x1; float y = (y2 - y1) * t + y1; float z = (z2 - z1) * t + z1;
            float cz = MathF.Cos(_rot * 0.6f), sz = MathF.Sin(_rot * 0.6f);
            float cy = MathF.Cos(_rot * 0.3f), sy = MathF.Sin(_rot * 0.3f);
            float cx = MathF.Cos(_rot), sx = MathF.Sin(_rot);
            float xz = x * cz - y * sz; float yz = x * sz + y * cz; float zz = z;
            float xy = xz * cy + zz * sy; float zy = -xz * sy + zz * cy; float yy = yz;
            float yx = yy * cx - zy * sx; float zx = yy * sx + zy * cx; float xx = xy;
            float pers = 2f / (2 + zx);
            float px = xx * pers; float py = yx * pers;
            float sxp = px * _w * 0.45f + _w * 0.5f; float syp = py * _h * 0.45f + _h * 0.5f;
            pts[k] = (sxp, syp);
        }
        canvas.DrawLines(pts, 1.0f, 0xFFFFFFFF);
    }

    public void Dispose() { }
}


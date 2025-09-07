using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class RainbowSphereGridSuperscope : IVisualizerPlugin
{
    public string Id => "rainbow_sphere_grid";
    public string DisplayName => "Rainbow Sphere Grid";

    private int _w, _h;
    private int _n = 700;
    private float _phase;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _phase += 0.02f;
        Span<(float x, float y)> pts = stackalloc (float, float)[_n];
        for (int k = 0; k < _n; k++)
        {
            float i = k / (float)(_n - 1);
            float theta = MathF.Acos(1 - 2 * i);
            float phi = i * MathF.PI * 6;
            float xs = MathF.Sin(theta) * MathF.Cos(phi + _phase);
            float ys = MathF.Sin(theta) * MathF.Sin(phi + _phase);
            float zs = MathF.Cos(theta);
            float g = 0.1f * (MathF.Sin(phi * 6 + _phase) + MathF.Sin(theta * 6 + _phase));
            xs += g * xs; ys += g * ys;
            float pers = 1 / (1 + zs);
            float x = xs * pers; float y = ys * pers;
            float px = x * _w * 0.45f + _w * 0.5f; float py = y * _h * 0.45f + _h * 0.5f;
            pts[k] = (px, py);
        }
        canvas.DrawLines(pts, 1.0f, 0xFFFFFFFF);
    }

    public void Dispose() { }
}


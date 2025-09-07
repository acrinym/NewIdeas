using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class ScopeDishSuperscope : IVisualizerPlugin
{
    public string Id => "scope_dish";
    public string DisplayName => "3D Scope Dish";

    private int _w, _h;
    private int _n = 200;
    private float _r;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _r += 0.01f;
        Span<(float x, float y)> pts = stackalloc (float, float)[_n];
        for (int k = 0; k < _n; k++)
        {
            float i = k / (float)(_n - 1);
            float iz = 1.3f + MathF.Sin(_r + i * MathF.PI * 2) * (features.Volume + 0.5f) * 0.88f;
            float ix = MathF.Cos(_r + i * MathF.PI * 2) * (features.Volume + 0.5f) * 0.88f;
            float iy = -0.3f + MathF.Abs(MathF.Cos(features.Volume * MathF.PI));
            float x = ix / iz; float y = iy / iz;
            float px = x * _w * 0.45f + _w * 0.5f;
            float py = y * _h * 0.45f + _h * 0.5f;
            pts[k] = (px, py);
        }
        canvas.DrawLines(pts, 2.0f, 0xFF00FFAA);
    }

    public void Dispose() { }
}


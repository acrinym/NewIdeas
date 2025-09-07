using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class RotatingBowSuperscope : IVisualizerPlugin
{
    public string Id => "rotating_bow";
    public string DisplayName => "Rotating Bow";

    private int _w, _h;
    private float _t;
    private int _n = 80;

    public void Initialize(int width, int height) { _w = width; _h = height; _t = 0; }
    public void Resize(int width, int height) { _w = width; _h = height; }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _t += 0.01f;
        Span<(float x, float y)> pts = stackalloc (float, float)[_n];
        for (int k = 0; k < _n; k++)
        {
            float i = k / (float)(_n - 1);
            float r = i * (float)Math.PI * 2;
            float d = MathF.Sin(r * 3) + features.Volume * 0.5f;
            float x = MathF.Cos(_t + r) * d;
            float y = MathF.Sin(_t - r) * d;
            float px = x * _w * 0.4f + _w * 0.5f;
            float py = y * _h * 0.4f + _h * 0.5f;
            pts[k] = (px, py);
        }
        canvas.DrawLines(pts, 2.0f, 0xFFFFAA00);
    }

    public void Dispose() { }
}


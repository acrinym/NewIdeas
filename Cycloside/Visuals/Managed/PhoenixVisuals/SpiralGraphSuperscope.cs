using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class SpiralGraphSuperscope : IVisualizerPlugin
{
    public string Id => "spiral_graph";
    public string DisplayName => "Spiral Graph";

    private int _w, _h;
    private float _t;
    private int _n = 100;

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
            float r = i * (float)Math.PI * 128 + _t;
            float x = MathF.Cos(r / 64) * 0.7f + MathF.Sin(r) * 0.3f;
            float y = MathF.Sin(r / 64) * 0.7f + MathF.Cos(r) * 0.3f;
            float px = x * _w * 0.45f + _w * 0.5f;
            float py = y * _h * 0.45f + _h * 0.5f;
            pts[k] = (px, py);
        }
        canvas.DrawLines(pts, 2.0f, 0xFF66CCFF);
    }

    public void Dispose() { }
}


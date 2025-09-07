using System;
using System.Collections.Generic;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Spiral superscope visualization (ported from PhoenixVisualizer.Visuals.SpiralSuperscope)
/// </summary>
public sealed class SpiralSuperscope : IVisualizerPlugin
{
    public string Id => "spiral_superscope";
    public string DisplayName => "Spiral Superscope";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 800;

    public void Initialize(int width, int height)
    {
        _width = width; _height = height; _time = 0;
    }

    public void Resize(int width, int height) { _width = width; _height = height; }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _time -= 0.05f;
        float volume = features.Volume;
        bool beat = features.Beat;

        var points = new List<(float x, float y)>(_numPoints);
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            float d = t + volume * 0.2f;
            float r = _time + t * (float)Math.PI * 4;
            float x = (float)Math.Cos(r) * d;
            float y = (float)Math.Sin(r) * d;
            x = x * _width * 0.3f + _width * 0.5f;
            y = y * _height * 0.3f + _height * 0.5f;
            points.Add((x, y));
        }
        uint color = beat ? 0xFFFFFF00 : 0xFF00FFFF;
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray().AsSpan(), 1.0f, color);
    }

    public void Dispose() { }
}

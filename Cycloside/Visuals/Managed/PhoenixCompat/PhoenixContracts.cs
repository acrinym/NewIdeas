using System;
using System.Collections.Generic;

namespace Cycloside.Visuals.Managed.PhoenixCompat;

/// <summary>
/// Minimal audio features contract adapted from PhoenixVisualizer.PluginHost.
/// This is provided to keep the Phoenix AVS engine portable inside Cycloside.
/// </summary>
public interface IAudioFeatures
{
    float[] Fft { get; }
    float[] Waveform { get; }
    float Rms { get; }
    double Bpm { get; }
    bool Beat { get; }
    float Bass { get; }
    float Mid { get; }
    float Treble { get; }
    float Energy { get; }
    float Volume { get; }
    float Peak { get; }
    double TimeSeconds { get; }
    float[] FrequencyBands { get; }
    float[] SmoothedFft { get; }
}

/// <summary>
/// A lightweight canvas contract similar to Phoenix's ISkiaCanvas.
/// Backed by Avalonia DrawingContext via our adapter.
/// </summary>
public interface ISimpleCanvas
{
    int Width { get; }
    int Height { get; }

    void Clear(uint color);
    void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f);
    void DrawLines(Span<(float x, float y)> points, float thickness, uint color);
    void DrawRect(float x, float y, float width, float height, uint color, bool filled = false);
    void FillRect(float x, float y, float width, float height, uint color);
    void DrawCircle(float x, float y, float radius, uint color, bool filled = false);
    void FillCircle(float x, float y, float radius, uint color);
    void DrawText(string text, float x, float y, uint color, float size = 12.0f);
    void DrawPoint(float x, float y, uint color, float size = 1.0f);
    void Fade(uint color, float alpha);
    void DrawPolygon(Span<(float x, float y)> points, uint color, bool filled = false);
    void DrawArc(float x, float y, float radius, float startAngle, float sweepAngle, uint color, float thickness = 1.0f);
    void SetLineWidth(float width);
    float GetLineWidth();
}

/// <summary>
/// Portable visualizer contract adapted from PhoenixVisualizer.PluginHost.IVisualizerPlugin.
/// Visuals ported from Phoenix implement this to render onto ISimpleCanvas using IAudioFeatures.
/// </summary>
public interface IVisualizerPlugin : IDisposable
{
    string Id { get; }
    string DisplayName { get; }
    void Initialize(int width, int height);
    void Resize(int width, int height);
    void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas);
}

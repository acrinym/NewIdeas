using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class SpectrumVisualizer : IVisualizerPlugin
{
    public string Id => "spectrum";
    public string DisplayName => "Spectrum Bars";

    private int _width;
    private int _height;

    public void Initialize(int width, int height) => Resize(width, height);
    public void Resize(int width, int height) { _width = width; _height = height; }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        var fft = features.Fft;
        if (fft == null || fft.Length == 0) return;
        int bins = 64;
        int len = fft.Length;
        int step = Math.Max(1, len / bins);
        float barWidth = _width / (float)bins;
        float maxHeight = _height * 0.9f;
        Span<(float x, float y)> seg = stackalloc (float, float)[2];
        for (int i = 0; i < bins; i++)
        {
            int start = i * step;
            int end = Math.Min(start + step, len);
            float sum = 0f; for (int j = start; j < end; j++) sum += MathF.Abs(fft[j]);
            float avg = sum / Math.Max(1, end - start);
            float height = Math.Clamp(avg * 10f, 0f, 1f) * maxHeight;
            float x = i * barWidth + barWidth / 2f;
            seg[0] = (x, _height); seg[1] = (x, _height - height);
            uint color = HsvToArgb((1f - i / (float)(bins - 1)) * 270f, 1f, 1f);
            canvas.DrawLines(seg, Math.Max(1f, barWidth * 0.8f), color);
        }
    }

    public void Dispose() { }

    private static uint HsvToArgb(float h, float s, float v)
    {
        float c = v * s; float x = c * (1 - MathF.Abs((h / 60f % 2) - 1)); float m = v - c;
        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        byte R = (byte)Math.Clamp((r + m) * 255f, 0, 255);
        byte G = (byte)Math.Clamp((g + m) * 255f, 0, 255);
        byte B = (byte)Math.Clamp((b + m) * 255f, 0, 255);
        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | B;
    }
}

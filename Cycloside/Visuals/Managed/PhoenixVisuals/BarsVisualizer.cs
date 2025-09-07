using System;
using System.Linq;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class BarsVisualizer : IVisualizerPlugin
{
    public string Id => "bars";
    public string DisplayName => "Simple Bars";

    private int _w, _h;
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF101010);
        if (f.Fft is null || f.Fft.Length == 0) return;

        float fftSum = 0f, fftMax = 0f; int fftNonZero = 0;
        for (int i = 0; i < f.Fft.Length; i++)
        {
            float absVal = MathF.Abs(f.Fft[i]);
            fftSum += absVal; if (absVal > fftMax) fftMax = absVal; if (absVal > 0.001f) fftNonZero++;
        }
        if (fftSum < 0.001f || fftMax < 0.001f || fftNonZero < 10)
        {
            var time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            for (int i = 0; i < f.Fft.Length; i++) f.Fft[i] = MathF.Sin(time * 2f + i * 0.1f) * 0.3f;
        }

        int n = Math.Min(64, f.Fft.Length);
        float barW = Math.Max(1f, (float)_w / n);
        Span<(float x, float y)> seg = stackalloc (float, float)[2];
        for (int i = 0; i < n; i++)
        {
            float v = f.Fft[i];
            float mag = MathF.Min(1f, (float)Math.Log(1 + 8 * Math.Max(0, v)));
            float h = mag * (_h - 10);
            float x = i * barW;
            seg[0] = (x + barW * 0.5f, _h - 5);
            seg[1] = (x + barW * 0.5f, _h - 5 - h);
            canvas.DrawLines(seg, Math.Max(1f, barW * 0.6f), 0xFF40C4FF);
        }
    }

    public void Dispose() { }
}


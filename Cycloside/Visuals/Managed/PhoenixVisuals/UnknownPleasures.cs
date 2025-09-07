using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Joy Division-like stacked wave lines driven by audio waveform and FFT energy.
/// </summary>
public sealed class UnknownPleasures : IVisualizerPlugin
{
    public string Id => "unknown_pleasures";
    public string DisplayName => "Unknown Pleasures";

    private int _w, _h;
    private float _phase;
    private readonly Random _rng = new();

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _phase += 0.7f + f.Bass * 0.6f;

        // Layout parameters
        int rows = 36;                 // number of stacked lines
        int cols = 160;                // samples per row
        float marginX = _w * 0.08f;
        float marginY = _h * 0.08f;
        float usableW = _w - marginX * 2f;
        float usableH = _h - marginY * 2f;
        float rowStep = usableH / (rows + 1);

        // Base amplitude from energy; increase on beat
        float amp = MathF.Min(1f, f.Energy * 2f) * 0.35f + (f.Beat ? 0.15f : 0f);

        // Pre-build an index mapping into the waveform array
        var wave = f.Waveform;
        int wlen = wave?.Length > 0 ? wave.Length : 1;

        // Draw rows back to front so overlaps look nice
        var ptsBuf = new (float x, float y)[cols];
        for (int r = 0; r < rows; r++)
        {
            float y0 = marginY + (r + 1) * rowStep;
            // Row-specific noise and falloff
            float fall = 1f - r / (float)rows;
            float rowAmp = amp * (0.3f + 0.7f * fall);
            float rowNoise = (float)(_rng.NextDouble() * 0.15 - 0.075);

            // Allocate a single span of points for the polyline
            float prev = 0f;
            for (int c = 0; c < cols; c++)
            {
                float t = cols > 1 ? c / (float)(cols - 1) : 0f; // 0..1
                float x = marginX + t * usableW;

                // Map t across waveform; apply simple easing to emphasize center
                int wi = (int)(t * (wlen - 1));
                float sample = wave != null ? (wave[wi] * 2f - 1f) : 0f;

                // Subtle per-row oscillation to avoid identical rows
                float osc = MathF.Sin(_phase * 0.02f + r * 0.3f + t * 6.283f) * 0.12f;

                // Accumulate and smooth for a more organic shape
                float yOff = (sample * 0.6f + osc + rowNoise) * rowAmp * (1.0f + MathF.Sin(t * MathF.PI) * 0.6f);
                float smoothed = prev * 0.7f + yOff * 0.3f; prev = smoothed;

                // Bend the center upwards like the album art
                float centerBend = MathF.Exp(-MathF.Pow((t - 0.5f) * 2.4f, 2)) * 0.35f * rowAmp;
                float y = y0 - (smoothed + centerBend) * usableH * 0.25f;

                ptsBuf[c] = (x, y);
            }

            // Width varies slightly with FFT peak; color cycles with treble/row for variety
            float lw = 1.0f + f.Peak * 1.5f;
            float hue = (r / (float)rows) * 300f + f.Treble * 120f; // 0..300 + treble accent
            uint col = HsvToArgb(hue % 360f, 0.7f, 0.95f);
            canvas.DrawLines(ptsBuf, lw, col);
        }
    }

    private static uint HsvToArgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - MathF.Abs((h / 60f % 2) - 1));
        float m = v - c;
        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        byte R = (byte)Math.Clamp((int)((r + m) * 255f), 0, 255);
        byte G = (byte)Math.Clamp((int)((g + m) * 255f), 0, 255);
        byte B = (byte)Math.Clamp((int)((b + m) * 255f), 0, 255);
        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | B;
    }
}

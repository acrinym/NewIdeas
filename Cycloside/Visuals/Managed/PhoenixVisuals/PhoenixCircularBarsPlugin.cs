using System;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

public sealed class PhoenixCircularBarsPlugin : IVisualizerPlugin
{
    public string Id => "phoenix_circular_bars";
    public string DisplayName => "Phoenix Circular Bars";

    private int _w, _h;
    private float _rotation, _pulsePhase, _colorShift, _bouncePhase;

    private const int BAR_COUNT = 32;
    private const float INNER_RADIUS = 0.15f;
    private const float OUTER_RADIUS = 0.85f;
    private const float BAR_WIDTH = 0.08f;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas)
    {
        canvas.Clear(0xFF0A0A0A);
        float dt = 1f / 60f;
        _rotation += dt * (2f + features.Mid * 3f);
        _pulsePhase += dt * (3f + features.Treble * 4f);
        _colorShift += dt * (1f + features.Rms * 2f);
        _bouncePhase += dt * (4f + features.Bass * 6f);

        float cx = _w * 0.5f; float cy = _h * 0.5f; float maxR = Math.Min(_w, _h) * 0.4f;
        DrawGlowRings(canvas, cx, cy, maxR, features);

        for (int i = 0; i < BAR_COUNT; i++)
        {
            float angle = (i / (float)BAR_COUNT) * MathF.PI * 2f + _rotation;
            int fftIndex = Math.Clamp((i * features.Fft.Length) / Math.Max(1, BAR_COUNT), 0, Math.Max(0, features.Fft.Length - 1));
            float fftValue = features.Fft.Length > 0 ? features.Fft[fftIndex] : 0f;
            float barLength = INNER_RADIUS + (OUTER_RADIUS - INNER_RADIUS) * Math.Clamp(fftValue, 0, 1);
            float barHeight = barLength * maxR;
            float bounce = MathF.Sin(_bouncePhase + i * 0.3f) * 0.1f;
            float pulse = MathF.Sin(_pulsePhase + i * 0.2f) * 0.15f;
            barHeight *= (1f + bounce + pulse);
            barHeight = Math.Max(barHeight, maxR * 0.02f);

            float startR = INNER_RADIUS * maxR; float endR = startR + barHeight;
            float sx = cx + startR * MathF.Cos(angle); float sy = cy + startR * MathF.Sin(angle);
            float ex = cx + endR * MathF.Cos(angle); float ey = cy + endR * MathF.Sin(angle);
            float halfW = BAR_WIDTH * 0.5f; float w1 = startR * halfW; float w2 = endR * halfW;
            var corners = new (float x, float y)[]
            {
                (sx + w1 * MathF.Cos(angle + MathF.PI/2), sy + w1 * MathF.Sin(angle + MathF.PI/2)),
                (sx + w1 * MathF.Cos(angle - MathF.PI/2), sy + w1 * MathF.Sin(angle - MathF.PI/2)),
                (ex + w2 * MathF.Cos(angle - MathF.PI/2), ey + w2 * MathF.Sin(angle - MathF.PI/2)),
                (ex + w2 * MathF.Cos(angle + MathF.PI/2), ey + w2 * MathF.Sin(angle + MathF.PI/2))
            };
            uint barColor = HsvToRgb((_colorShift * 60f + i * 12f) % 360f, 0.9f, 1f);
            DrawBar(canvas, corners, barColor);
        }

        if (features.Beat) DrawCenterSparkle(canvas, cx, cy, maxR * 0.1f);
        DrawFloatingParticles(canvas, features);
    }

    private static void DrawGlowRings(ISimpleCanvas canvas, float cx, float cy, float maxR, IAudioFeatures features)
    {
        uint inner = 0x2200FFFF; canvas.FillCircle(cx, cy, maxR * INNER_RADIUS * 1.2f, inner);
        float outerGlowRadius = maxR * OUTER_RADIUS * (1f + features.Bass * 0.3f);
        uint outer = 0x1500FF88; canvas.FillCircle(cx, cy, outerGlowRadius, outer);
    }

    private static void DrawBar(ISimpleCanvas canvas, (float x, float y)[] corners, uint color)
    {
        float cx = (corners[0].x + corners[1].x + corners[2].x + corners[3].x) / 4f;
        float cy = (corners[0].y + corners[1].y + corners[2].y + corners[3].y) / 4f;
        float maxDist = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            float dx = corners[i].x - cx; float dy = corners[i].y - cy; float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > maxDist) maxDist = dist;
        }
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
            {
                float ox = (i - 2) * maxDist * 0.2f; float oy = (j - 2) * maxDist * 0.2f;
                float fx = cx + ox; float fy = cy + oy;
                if (PointInPoly(fx, fy, corners)) canvas.FillCircle(fx, fy, maxDist * 0.15f, color);
            }
        canvas.SetLineWidth(1f);
        var outline = new (float x, float y)[corners.Length + 1];
        for (int k = 0; k < corners.Length; k++) outline[k] = corners[k];
        outline[^1] = corners[0];
        canvas.DrawLines(outline, 1f, 0xFFFFFFFF);
    }

    private static bool PointInPoly(float x, float y, (float x, float y)[] c)
    {
        bool inside = false; for (int i = 0, j = c.Length - 1; i < c.Length; j = i++)
        {
            if (((c[i].y > y) != (c[j].y > y)) && (x < (c[j].x - c[i].x) * (y - c[i].y) / (c[j].y - c[i].y) + c[i].x)) inside = !inside;
        }
        return inside;
    }

    private static uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s, x = c * (1f - Math.Abs((h / 60f) % 2f - 1f)), m = v - c; float r, g, b;
        if (h < 60f) { r = c; g = x; b = 0f; }
        else if (h < 120f) { r = x; g = c; b = 0f; }
        else if (h < 180f) { r = 0f; g = c; b = x; }
        else if (h < 240f) { r = 0f; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }
        byte R = (byte)((r + m) * 255f); byte G = (byte)((g + m) * 255f); byte B = (byte)((b + m) * 255f);
        return (uint)(0xFF << 24 | R << 16 | G << 8 | B);
    }

    private static void DrawCenterSparkle(ISimpleCanvas canvas, float cx, float cy, float size)
    {
        uint sparkle = 0xFFFFFFFF; canvas.FillCircle(cx, cy, size, sparkle);
        uint ray = 0x88FFFFFF; canvas.SetLineWidth(2f);
        for (int i = 0; i < 8; i++)
        {
            float a = (i / 8f) * MathF.PI * 2f; float ex = cx + size * 2f * MathF.Cos(a); float ey = cy + size * 2f * MathF.Sin(a);
            canvas.DrawLine(cx, cy, ex, ey, ray, 2f);
        }
    }

    private static void DrawFloatingParticles(ISimpleCanvas canvas, IAudioFeatures features)
    {
        int n = 12; uint col = 0x44FFFFFF; float t = (float)features.TimeSeconds;
        for (int i = 0; i < n; i++)
        {
            float x = (float)(features.TimeSeconds % 1.0) + 0; // not used in center calc here
            float px = (float)(features.TimeSeconds);
            float py = (float)(features.TimeSeconds);
            // simple drifting points
            float xf = (float)(features.TimeSeconds * 0.5 + i * 0.7);
            float yf = (float)(features.TimeSeconds * 0.3 + i * 0.9);
            float xx = (float)(Math.Abs(Math.Sin(xf)));
            float yy = (float)(Math.Abs(Math.Cos(yf)));
            float xPos = xx * (features.Fft.Length > 0 ? features.Fft[0] : 1) * 10f; // keep minimal dependency
            float yPos = yy * (features.Fft.Length > 0 ? features.Fft[1 % features.Fft.Length] : 1) * 10f;
            // draw tiny circle near top-left area for subtle sparkle
            canvas.FillCircle(10 + xPos, 10 + yPos, 2f, col);
        }
    }
}


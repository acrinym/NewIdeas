using System;
using System.Collections.Generic;
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.PhoenixVisuals;

/// <summary>
/// Recursive evolving squares inspired by xscreensaver's 'boxed' style. Audio drives split speed and color.
/// </summary>
public sealed class EverEvolvingSquares : IVisualizerPlugin
{
    public string Id => "ever_evolving_squares";
    public string DisplayName => "Ever‑Evolving Squares";

    private int _w, _h;
    private readonly List<Cell> _cells = new();
    private readonly Random _rng = new();

    public void Initialize(int width, int height)
    {
        _w = width; _h = height; _cells.Clear();
        _cells.Add(new Cell { X = 0, Y = 0, W = width, H = height, Age = 0, Life = 120 });
    }
    public void Resize(int width, int height) { _w = width; _h = height; if (_cells.Count == 0) Initialize(width, height); }
    public void Dispose() { }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas c)
    {
        // Clear and draw current set of squares; evolve over time with audio influence
        c.Clear(0xFF000000);
        float speed = 1f + f.Energy * 4f + (f.Beat ? 3f : 0f);

        // Update ages and split old cells
        for (int i = 0; i < _cells.Count; i++)
        {
            var tmp = _cells[i];
            tmp.Age += speed;
            _cells[i] = tmp;
        }

        for (int i = _cells.Count - 1; i >= 0; i--)
        {
            var cell = _cells[i];
            // Subdivide if lifetime reached and size large enough
            if (cell.Age >= cell.Life && cell.W > 40 && cell.H > 40)
            {
                _cells.RemoveAt(i);
                Subdivide(cell, f);
            }
        }

        // Draw
        foreach (var cell in _cells)
        {
            // Color from band; slightly animate hue by treble
            float hue = ((cell.Seed * 37) % 360) + f.Treble * 90f;
            uint col = HsvToRgb(hue % 360f, 0.8f, 0.9f);
            // Border thickness by mid energy
            float lw = 1.0f + f.Mid * 4f;
            // Slight insets to create nested feel
            float inset = (float)(Math.Sin((cell.Age / cell.Life) * Math.PI) * 0.12) * Math.Min(cell.W, cell.H);
            c.DrawRect(cell.X + inset, cell.Y + inset, cell.W - inset * 2, cell.H - inset * 2, col, filled: false);
        }
    }

    private void Subdivide(Cell cell, IAudioFeatures f)
    {
        // Choose split orientation based on aspect and random; bias by bass
        bool vertical = (cell.W > cell.H) ^ (_rng.NextDouble() < (0.4 + f.Bass * 0.3f));
        if (vertical)
        {
            float ratio = 0.3f + (float)_rng.NextDouble() * 0.4f; // 30%..70%
            int w1 = (int)(cell.W * ratio);
            var a = new Cell { X = cell.X, Y = cell.Y, W = w1, H = cell.H, Life = NextLife(), Seed = _rng.Next(360) };
            var b = new Cell { X = cell.X + w1, Y = cell.Y, W = cell.W - w1, H = cell.H, Life = NextLife(), Seed = _rng.Next(360) };
            _cells.Add(a); _cells.Add(b);
        }
        else
        {
            float ratio = 0.3f + (float)_rng.NextDouble() * 0.4f;
            int h1 = (int)(cell.H * ratio);
            var a = new Cell { X = cell.X, Y = cell.Y, W = cell.W, H = h1, Life = NextLife(), Seed = _rng.Next(360) };
            var b = new Cell { X = cell.X, Y = cell.Y + h1, W = cell.W, H = cell.H - h1, Life = NextLife(), Seed = _rng.Next(360) };
            _cells.Add(a); _cells.Add(b);
        }
    }

    private int NextLife() => 90 + _rng.Next(180); // frames until next split (approx)

    private struct Cell
    {
        public float X, Y, W, H; public float Age; public int Life; public int Seed;
    }

    private static uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s, x = c * (1f - Math.Abs((h / 60f) % 2f - 1f)), m = v - c;
        float r, g, b;
        if (h < 60f) { r = c; g = x; b = 0f; }
        else if (h < 120f) { r = x; g = c; b = 0f; }
        else if (h < 180f) { r = 0f; g = c; b = x; }
        else if (h < 240f) { r = 0f; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }
        byte R = (byte)((r + m) * 255f); byte G = (byte)((g + m) * 255f); byte B = (byte)((b + m) * 255f);
        return (uint)(0xFF << 24 | R << 16 | G << 8 | B);
    }
}

using System;
using Avalonia;
using Avalonia.Media;

namespace Cycloside.Visuals.Managed.PhoenixCompat;

/// <summary>
/// Wraps an Avalonia DrawingContext to implement ISimpleCanvas for Phoenix AVS engine.
/// </summary>
public sealed class AvaloniaCanvasAdapter : ISimpleCanvas
{
    private readonly DrawingContext _ctx;
    private readonly int _w;
    private readonly int _h;
    private float _lineWidth = 1.0f;

    public AvaloniaCanvasAdapter(DrawingContext ctx, Size size)
    {
        _ctx = ctx;
        _w = (int)Math.Max(1, size.Width);
        _h = (int)Math.Max(1, size.Height);
    }

    public int Width => _w;
    public int Height => _h;

    public void Clear(uint color)
    {
        var c = FromRgb(color);
        _ctx.FillRectangle(new SolidColorBrush(c), new Rect(0, 0, _w, _h));
    }

    public void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f)
    {
        var pen = new Pen(new SolidColorBrush(FromRgb(color)), thickness);
        _ctx.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
    }

    public void DrawLines(Span<(float x, float y)> points, float thickness, uint color)
    {
        if (points.Length < 2) return;
        var pen = new Pen(new SolidColorBrush(FromRgb(color)), thickness);
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(new Point(points[0].x, points[0].y), false);
            for (int i = 1; i < points.Length; i++) g.LineTo(new Point(points[i].x, points[i].y));
            g.EndFigure(false);
        }
        _ctx.DrawGeometry(null, pen, geo);
    }

    public void DrawRect(float x, float y, float width, float height, uint color, bool filled = false)
    {
        var rect = new Rect(x, y, width, height);
        if (filled)
            _ctx.FillRectangle(new SolidColorBrush(FromRgb(color)), rect);
        else
            _ctx.DrawRectangle(null, new Pen(new SolidColorBrush(FromRgb(color)), _lineWidth), rect);
    }

    public void FillRect(float x, float y, float width, float height, uint color)
        => DrawRect(x, y, width, height, color, filled: true);

    public void DrawCircle(float x, float y, float radius, uint color, bool filled = false)
    {
        if (filled)
            _ctx.DrawEllipse(new SolidColorBrush(FromRgb(color)), null, new Point(x, y), radius, radius);
        else
            _ctx.DrawEllipse(null, new Pen(new SolidColorBrush(FromRgb(color)), _lineWidth), new Point(x, y), radius, radius);
    }

    public void FillCircle(float x, float y, float radius, uint color)
        => DrawCircle(x, y, radius, color, filled: true);

    public void DrawText(string text, float x, float y, uint color, float size = 12.0f)
    {
        // Fallback text rendering: draw a small marker line proportional to size.
        // This avoids dependency on specific text APIs and keeps engine portable.
        var pen = new Pen(new SolidColorBrush(FromRgb(color)), Math.Max(1, size * 0.1));
        _ctx.DrawLine(pen, new Point(x, y), new Point(x + Math.Max(4, size), y));
    }

    public void DrawPoint(float x, float y, uint color, float size = 1.0f)
    {
        FillCircle(x, y, Math.Max(1.0f, size * 0.5f), color);
    }

    public void Fade(uint color, float alpha)
    {
        var c = FromRgb(color);
        var a = (byte)Math.Clamp((int)(alpha * 255), 0, 255);
        _ctx.FillRectangle(new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B)), new Rect(0, 0, _w, _h));
    }

    public void DrawPolygon(Span<(float x, float y)> points, uint color, bool filled = false)
    {
        if (points.Length < 3) return;
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(new Point(points[0].x, points[0].y), filled);
            for (int i = 1; i < points.Length; i++) g.LineTo(new Point(points[i].x, points[i].y));
            g.EndFigure(true);
        }
        if (filled)
            _ctx.DrawGeometry(new SolidColorBrush(FromRgb(color)), null, geo);
        else
            _ctx.DrawGeometry(null, new Pen(new SolidColorBrush(FromRgb(color)), _lineWidth), geo);
    }

    public void DrawArc(float x, float y, float radius, float startAngle, float sweepAngle, uint color, float thickness = 1.0f)
    {
        // Approximate arc with polyline segments for simplicity
        int segments = Math.Max(8, (int)(Math.Abs(sweepAngle) / 10));
        Span<(float x, float y)> pts = stackalloc (float x, float y)[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            var a = (startAngle + sweepAngle * (i / (float)segments)) * Math.PI / 180.0;
            pts[i] = ((float)(x + Math.Cos(a) * radius), (float)(y + Math.Sin(a) * radius));
        }
        DrawLines(pts, thickness, color);
    }

    public void SetLineWidth(float width) => _lineWidth = width;
    public float GetLineWidth() => _lineWidth;

    private static Color FromRgb(uint rgb)
    {
        var r = (byte)((rgb >> 16) & 0xFF);
        var g = (byte)((rgb >> 8) & 0xFF);
        var b = (byte)(rgb & 0xFF);
        return Color.FromRgb(r, g, b);
    }
}

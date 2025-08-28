using System;
using Avalonia;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class OscilloscopeVisualizer : IManagedVisualizer
{
    private readonly byte[] _wave = new byte[1152];
    private readonly Pen _wavePen = new(new SolidColorBrush(Color.FromArgb(230, 0, 255, 180)), 2);
    private readonly Pen _midPen = new(Brushes.Gray, 1, DashStyle.Dash);

    public string Name => "Oscilloscope";
    public string Description => "Stereo waveform";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        var len = Math.Min(_wave.Length, data.Waveform.Length);
        Array.Copy(data.Waveform, _wave, len);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width;
        var h = size.Height;
        if (w <= 0 || h <= 0) return;

        // Background
        ctx.FillRectangle(new SolidColorBrush(Color.FromRgb(5, 5, 8)), new Rect(0, 0, w, h));
        // Midline
        var mid = h / 2;
        ctx.DrawLine(_midPen, new Point(0, mid), new Point(w, mid));

        // Draw left channel using first 576 samples
        int samples = 576;
        var geo = new StreamGeometry();
        using (var gctx = geo.Open())
        {
            double step = w / (samples - 1);
            for (int i = 0; i < samples; i++)
            {
                var v = (_wave[i] / 255.0) * 2 - 1; // -1..1
                var y = mid + v * (h * 0.45);
                var x = i * step;
                if (i == 0) gctx.BeginFigure(new Point(x, y), false);
                else gctx.LineTo(new Point(x, y));
            }
        }
        ctx.DrawGeometry(null, _wavePen, geo);

        // Right channel (duplicate in our data): shift color slightly
        var rPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 120, 255)), 1.5);
        var geoR = new StreamGeometry();
        using (var gctx = geoR.Open())
        {
            double step = w / (samples - 1);
            for (int i = 0; i < samples; i++)
            {
                var v = (_wave[i + samples] / 255.0) * 2 - 1;
                var y = mid + v * (h * 0.35);
                var x = i * step;
                if (i == 0) gctx.BeginFigure(new Point(x, y), false);
                else gctx.LineTo(new Point(x, y));
            }
        }
        ctx.DrawGeometry(null, rPen, geoR);
    }
}


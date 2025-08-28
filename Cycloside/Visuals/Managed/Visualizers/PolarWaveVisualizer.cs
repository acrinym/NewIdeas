using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class PolarWaveVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly byte[] _wave = new byte[1152];
    private readonly Pen _pen = new(new SolidColorBrush(Color.FromRgb(255, 120, 0)), 2);
    private readonly Pen _pen2 = new(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), 1);
    private double _thickness = 2;

    public string Name => "Polar Wave";
    public string Description => "Radial waveform shape";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        Array.Copy(data.Waveform, 0, _wave, 0, Math.Min(_wave.Length, data.Waveform.Length));
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w<=0||h<=0) return;
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0,0,w,h));
        var c = new Point(w/2, h/2); double r = Math.Min(w,h)*0.35;

        // main
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            for (int i=0;i<576;i++)
            {
                double t = (double)i/576.0 * Math.PI*2;
                double v = (_wave[i]/255.0)*2 - 1; // -1..1
                double rr = r + v * r*0.5;
                var p = new Point(c.X + Math.Cos(t)*rr, c.Y + Math.Sin(t)*rr);
                if (i==0) g.BeginFigure(p, false);
                else g.LineTo(p);
            }
        }
        ctx.DrawGeometry(null, new Pen(ManagedVisStyle.Accent(), _thickness), geo);

        // secondary ring
        var ring = new StreamGeometry();
        using (var g = ring.Open())
        {
            for (int i=0;i<576;i++)
            {
                double t = (double)i/576.0 * Math.PI*2;
                double v = (_wave[i+576]/255.0)*2 - 1;
                double rr = r*0.6 + v * r*0.3;
                var p = new Point(c.X + Math.Cos(t)*rr, c.Y + Math.Sin(t)*rr);
                if (i==0) g.BeginFigure(p, false);
                else g.LineTo(p);
            }
        }
        ctx.DrawGeometry(null, new Pen(ManagedVisStyle.Secondary(), Math.Max(1, _thickness*0.6)), ring);
    }

    public string ConfigKey => "ManagedVis.PolarWave.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var thick = new Slider { Minimum = 1, Maximum = 6, Width = 160, Value = _thickness };
        thick.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _thickness = thick.Value; StateManager.Set(ConfigKey+"Thickness", _thickness.ToString("0.0")); } };
        panel.Children.Add(new TextBlock { Text = "Thickness:" }); panel.Children.Add(thick);
        return panel;
    }
    public void LoadOptions()
    {
        if (double.TryParse(StateManager.Get(ConfigKey+"Thickness"), out var t)) _thickness = Math.Clamp(t, 1, 6);
    }
}

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class CircularSpectrumVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly byte[] _spectrum = new byte[1152];
    private readonly Pen _ringPen = new(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1);
    private readonly SolidColorBrush _barBrush = new(Color.FromArgb(220, 50, 205, 255));

    public string Name => "Circular Spectrum";
    public string Description => "Radial bars around a ring";
    private int _bars = 64;

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        var len = Math.Min(_spectrum.Length, data.Spectrum.Length);
        Array.Copy(data.Spectrum, _spectrum, len);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w <= 0 || h <= 0) return;
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0, 0, w, h));

        var center = new Point(w / 2, h / 2);
        var radius = Math.Min(w, h) * 0.35;
        ctx.DrawEllipse(null, _ringPen, center, radius, radius);

        int bars = Math.Clamp(_bars, 16, 128);
        for (int i = 0; i < bars; i++)
        {
            int bin = (int)(Math.Pow((double)i / (bars - 1), 1.6) * 575);
            double v = Math.Min(1.0, (_spectrum[bin] / 255.0) * ManagedVisStyle.Sensitivity);
            double len = v * radius * 0.9;
            double ang = i * (2 * Math.PI / bars) - Math.PI / 2;
            var inner = new Point(center.X + radius * Math.Cos(ang), center.Y + radius * Math.Sin(ang));
            var outer = new Point(center.X + (radius + len) * Math.Cos(ang), center.Y + (radius + len) * Math.Sin(ang));
            var pen = new Pen(ManagedVisStyle.Accent(), Math.Max(1, Math.Min(6, len / 8)));
            ctx.DrawLine(pen, inner, outer);
        }
    }

    public string ConfigKey => "ManagedVis.Circular.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bars = new Slider { Minimum = 16, Maximum = 128, Width = 160, Value = _bars };
        bars.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _bars = (int)bars.Value; StateManager.Set(ConfigKey + "Bars", _bars.ToString()); } };
        panel.Children.Add(new TextBlock { Text = "Bars:" }); panel.Children.Add(bars);
        return panel;
    }
    public void LoadOptions()
    {
        if (int.TryParse(StateManager.Get(ConfigKey + "Bars"), out var b)) _bars = Math.Clamp(b, 16, 128);
    }
}

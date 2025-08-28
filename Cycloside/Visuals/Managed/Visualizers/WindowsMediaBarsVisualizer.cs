using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

// Inspired by classic WMP "Bars and Waves" (bars variant)
public sealed class WindowsMediaBarsVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly byte[] _spec = new byte[1152];
    private double[] _smooth = new double[64];
    private double[] _peaks = new double[64];
    private readonly SolidColorBrush _bar = new(Color.FromRgb(0, 200, 255));
    private readonly SolidColorBrush _trail = new(Color.FromArgb(120, 0, 200, 255));
    private readonly SolidColorBrush _bg = new(Color.FromRgb(8,8,12));
    private readonly Pen _grid = new(new SolidColorBrush(Color.FromArgb(100, 255,255,255)), 1, DashStyle.Dash);
    private int _bars = 64;
    private double _smoothFactor = 0.7;
    private double _peakDecay = 0.01;

    public string Name => "WMP Bars";
    public string Description => "Windows Media Player style bars";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        var len = Math.Min(_spec.Length, data.Spectrum.Length);
        Array.Copy(data.Spectrum, _spec, len);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w <= 0 || h <= 0) return;
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0,0,w,h));

        // grid lines
        for (int i=1;i<5;i++)
        {
            var y = h * i / 5.0; ctx.DrawLine(_grid, new Point(0,y), new Point(w,y));
        }

        int n = _bars; if (n<8) n=8; if (n>128) n=128; double barW = w / n;
        for (int i=0;i<n;i++)
        {
            int bin = (int)(Math.Pow((double)i/(n-1), 1.5) * 575);
            double v = _spec[bin] / 255.0;
            _smooth[i] = _smooth[i] * _smoothFactor + v * (1 - _smoothFactor);
            double bh = _smooth[i] * h;

            // peak with decay
            _peaks[i] = Math.Max(_peaks[i]-h*_peakDecay, bh);

            double x = i*barW + barW*0.15;
            var rect = new Rect(x, h-bh, barW*0.7, bh);
            // trail
            var trailRect = new Rect(x, h-_peaks[i]-3, barW*0.7, Math.Min(3, _peaks[i]));
            ctx.FillRectangle(new SolidColorBrush(Color.FromArgb(120, ManagedVisStyle.Accent().Color.R, ManagedVisStyle.Accent().Color.G, ManagedVisStyle.Accent().Color.B)), rect);
            ctx.FillRectangle(ManagedVisStyle.Accent(), new Rect(x, h-bh, barW*0.7, Math.Max(1, bh*0.7)));
            ctx.FillRectangle(ManagedVisStyle.Peak(), trailRect);
        }
    }

    public string ConfigKey => "ManagedVis.WmpBars.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bars = new Slider { Minimum = 8, Maximum = 128, Width = 160, Value = _bars };
        bars.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _bars = (int)bars.Value; StateManager.Set(ConfigKey+"Bars", _bars.ToString()); ResizeArrays(); } };
        var smooth = new Slider { Minimum = 0.1, Maximum = 0.95, Width = 140, Value = _smoothFactor };
        smooth.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _smoothFactor = smooth.Value; StateManager.Set(ConfigKey+"Smooth", _smoothFactor.ToString("0.00")); } };
        var peak = new Slider { Minimum = 0.002, Maximum = 0.05, Width = 140, Value = _peakDecay };
        peak.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _peakDecay = peak.Value; StateManager.Set(ConfigKey+"PeakDecay", _peakDecay.ToString("0.000")); } };
        panel.Children.Add(new TextBlock { Text = "Bars:" }); panel.Children.Add(bars);
        panel.Children.Add(new TextBlock { Text = "Smooth:" }); panel.Children.Add(smooth);
        panel.Children.Add(new TextBlock { Text = "Peak:" }); panel.Children.Add(peak);
        return panel;
    }

    public void LoadOptions()
    {
        if (int.TryParse(StateManager.Get(ConfigKey+"Bars"), out var b)) _bars = Math.Clamp(b, 8, 128);
        if (double.TryParse(StateManager.Get(ConfigKey+"Smooth"), out var s)) _smoothFactor = Math.Clamp(s, 0.1, 0.95);
        if (double.TryParse(StateManager.Get(ConfigKey+"PeakDecay"), out var p)) _peakDecay = Math.Clamp(p, 0.002, 0.05);
        ResizeArrays();
    }

    private void ResizeArrays()
    {
        if (_smooth.Length != _bars) _smooth = new double[_bars];
        if (_peaks.Length != _bars) _peaks = new double[_bars];
    }
}

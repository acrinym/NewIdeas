using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class SpectrumBarsVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly byte[] _spectrum = new byte[1152];
    private readonly double[] _peaksReadonly = new double[64];
    private DateTime _last = DateTime.UtcNow;

    private int _bars = 64;
    private double _smoothFactor = 0.7;
    private double _peakDecay = 0.01;
    private double[] _smooth = new double[64];
    private double[] _peaks = new double[64];

    public string Name => "Spectrum Bars";
    public string Description => "Simple FFT bar visualizer";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        // Keep last stereo spectrum block (1152 bytes)
        var len = Math.Min(_spectrum.Length, data.Spectrum.Length);
        Array.Copy(data.Spectrum, _spectrum, len);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width;
        var h = size.Height;
        if (w <= 0 || h <= 0) return;

        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0, 0, w, h));

        // Grid lines
        var grid = ManagedVisStyle.Grid();
        for (int i = 1; i < 6; i++)
        {
            var y = h * i / 6.0;
            ctx.DrawLine(grid, new Point(0, y), new Point(w, y));
        }

        // Use 64 bars from left channel (first 576 bytes)
        int bars = _bars;
        if (bars < 8) bars = 8; if (bars > 128) bars = 128;
        double barWidth = w / bars;
        double decay = 0.02 + 0.001 * Math.Max(0, 60 - elapsed.TotalMilliseconds % 60);

        for (int i = 0; i < bars; i++)
        {
            int bin = (int)(Math.Pow((double)i / (bars - 1), 2.0) * 575); // skew to lower freqs
            var v = _spectrum[bin] / 255.0; // 0..1
            _smooth[i] = _smooth[i] * _smoothFactor + v * (1 - _smoothFactor);
            var barHeight = _smooth[i] * h;

            // Peak hold with slow decay
            var pd = _peakDecay;
            _peaks[i] = Math.Max(_peaks[i] - pd * h, barHeight);

            var x = i * barWidth + barWidth * 0.1;
            var y = h - barHeight;
            var rect = new Rect(x, y, barWidth * 0.8, barHeight);
            ctx.FillRectangle(ManagedVisStyle.Accent(), rect);

            // Peak indicator
            var py = h - _peaks[i];
            ctx.FillRectangle(ManagedVisStyle.Peak(), new Rect(x, py - 2, barWidth * 0.8, 2));
        }
    }

    public string ConfigKey => "ManagedVis.SpectrumBars.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bars = new Slider { Minimum = 8, Maximum = 128, Width = 160, Value = _bars };
        bars.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _bars = (int)bars.Value; StateManager.Set(ConfigKey+"Bars", _bars.ToString()); } };
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
        if (_peaks.Length != _bars) _peaks = new double[_bars];
        if (_smooth.Length != _bars) _smooth = new double[_bars];
    }
}

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class SpectrogramVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly byte[] _spec = new byte[1152];
    private byte[][]? _columns; // ring buffer of columns
    private int _head; // current write position
    private int _bands = 64;

    public string Name => "Spectrogram";
    public string Description => "Scrolling frequency heatmap";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        var len = Math.Min(_spec.Length, data.Spectrum.Length);
        Array.Copy(data.Spectrum, _spec, len);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = (int)Math.Max(64, size.Width);
        var h = (int)Math.Max(64, size.Height);
        if (_columns == null || _columns.Length != w)
        {
            _columns = new byte[w][];
            for (int i = 0; i < w; i++) _columns[i] = new byte[_bands];
            _head = 0;
        }

        // push new column using left channel
        var col = _columns[_head];
        for (int i = 0; i < _bands; i++)
        {
            int bin = (int)(Math.Pow((double)i / (_bands - 1), 2.0) * 575);
            col[i] = _spec[bin];
        }
        _head = (_head + 1) % _columns.Length;

        // background
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0, 0, w, h));

        // draw from oldest to newest left->right
        for (int x = 0; x < _columns.Length; x++)
        {
            int idx = (_head + x) % _columns.Length; // oldest first
            var cdata = _columns[idx];
            for (int i = 0; i < _bands; i++)
            {
                double v = Math.Min(1.0, (cdata[i] / 255.0) * ManagedVisStyle.Sensitivity);
                var color = ColorLerp(ManagedVisStyle.Background().Color, ManagedVisStyle.Accent().Color, v);
                var brush = new SolidColorBrush(color);
                // low bands at bottom
                double y0 = h - (i + 1) * (h / (double)_bands);
                double y1 = h - i * (h / (double)_bands);
                ctx.FillRectangle(brush, new Rect(x, y0, 1, y1 - y0 + 1));
            }
        }
    }

    private static Color ColorLerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        byte L(byte x, byte y) => (byte)(x + (y - x) * t);
        return Color.FromRgb(L(a.R, b.R), L(a.G, b.G), L(a.B, b.B));
    }

    public string ConfigKey => "ManagedVis.Spectrogram.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var bands = new Slider { Minimum = 16, Maximum = 128, Width = 160, Value = _bands };
        bands.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _bands = (int)bands.Value; StateManager.Set(ConfigKey + "Bands", _bands.ToString()); _columns = null; } };
        panel.Children.Add(new TextBlock { Text = "Bands:" }); panel.Children.Add(bands);
        return panel;
    }
    public void LoadOptions()
    {
        if (int.TryParse(StateManager.Get(ConfigKey + "Bands"), out var b)) _bands = Math.Clamp(b, 16, 128);
    }
}

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class MatrixRainVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly Random _rng = new();
    private double[]? _y;
    private double[]? _speed;
    private int _cols;
    private int _desiredCols = 64;
    private double _amplitude;

    public string Name => "Matrix Rain";
    public string Description => "Falling code rain driven by bass";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        // Use low spectrum bins to estimate bass amplitude 0..1
        double sum = 0; int count = Math.Min(48, data.Spectrum.Length / 2);
        for (int i = 0; i < count; i++) sum += data.Spectrum[i];
        _amplitude = Math.Clamp(sum / (count * 255.0), 0, 1);
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w <= 0 || h <= 0) return;
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0, 0, w, h));

        // Initialize columns lazily
        if (_cols == 0 || _y == null || _speed == null)
        {
            _cols = Math.Clamp(_desiredCols, 16, 160);
            _y = new double[_cols];
            _speed = new double[_cols];
            for (int i = 0; i < _cols; i++) { _y[i] = _rng.NextDouble() * h; _speed[i] = 40 + _rng.NextDouble() * 120; }
        }

        // Parameters driven by amplitude
        var colWidth = w / _cols; var seg = Math.Max(6, h / 40);
        var maxLen = (int)Math.Clamp(6 + _amplitude * ManagedVisStyle.Sensitivity * 40, 8, 60);
        var accent = ManagedVisStyle.Accent().Color;
        var headBrush = new SolidColorBrush(Color.FromRgb(accent.R, accent.G, accent.B));

        for (int x = 0; x < _cols; x++)
        {
            double y = _y![x];
            // draw a vertical tail
            for (int i = 0; i < maxLen; i++)
            {
                var yy = (y - i * seg + h) % h;
                byte a = (byte)Math.Clamp(50 + 205 * (1.0 - i / (double)maxLen), 0, 255);
                var baseColor = accent;
                var brush = i == 0 ? headBrush : new SolidColorBrush(Color.FromArgb(a, baseColor.R, baseColor.G, baseColor.B));
                ctx.FillRectangle(brush, new Rect(x * colWidth + 1, yy, colWidth - 2, seg - 1));
            }
            // advance
            var speed = _speed![x] * (0.5 + _amplitude);
            _y[x] = (y + speed * 0.033) % h;
        }
    }

    public string ConfigKey => "ManagedVis.Matrix.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var cols = new Slider { Minimum = 16, Maximum = 160, Width = 160, Value = _desiredCols };
        cols.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _desiredCols = (int)cols.Value; StateManager.Set(ConfigKey + "Cols", _desiredCols.ToString()); _y = null; _speed = null; _cols = 0; } };
        panel.Children.Add(new TextBlock { Text = "Columns:" }); panel.Children.Add(cols);
        return panel;
    }
    public void LoadOptions()
    {
        if (int.TryParse(StateManager.Get(ConfigKey + "Cols"), out var c)) _desiredCols = Math.Clamp(c, 16, 160);
    }
}

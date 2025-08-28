using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Layout;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;

namespace Cycloside.Visuals.Managed.Visualizers;

public sealed class StarfieldVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private struct Star { public double X,Y,Z; }
    private Star[] _stars = Array.Empty<Star>();
    private readonly Random _rng = new();
    private double _speed = 1;
    private int _countSetting = 400;

    public string Name => "Starfield";
    public string Description => "3D starfield, speed on beats";

    public void Init() { }
    public void Dispose() { }

    public void UpdateAudioData(AudioData data)
    {
        double sum = 0; int n = Math.Min(64, data.Spectrum.Length/2);
        for (int i=0;i<n;i++) sum += data.Spectrum[i];
        _speed = 0.5 + Math.Clamp(sum/(n*255.0), 0, 1) * 3.0;
    }

    public void Render(DrawingContext ctx, Size size, TimeSpan elapsed)
    {
        var w = size.Width; var h = size.Height; if (w<=0||h<=0) return;
        ctx.FillRectangle(ManagedVisStyle.Background(), new Rect(0,0,w,h));

        if (_stars.Length == 0)
        {
            int count = Math.Clamp(_countSetting, 100, 2000);
            _stars = new Star[count];
            for (int i=0;i<count;i++) _stars[i] = NewStar();
        }

        var center = new Point(w/2, h/2);
        double fov = Math.Min(w,h)*0.8;
        double dt = 0.033;
        foreach (ref var s in _stars.AsSpan())
        {
            s.Z -= dt * _speed;
            if (s.Z <= 0.1) s = NewStar();

            double sx = (s.X / s.Z) * fov + center.X;
            double sy = (s.Y / s.Z) * fov + center.Y;
            double r = Math.Clamp(2.5/(s.Z*s.Z), 0.5, 3.5);
            var geo = new EllipseGeometry(new Rect(sx-r, sy-r, r*2, r*2));
            var a = (byte)Math.Clamp(40 + (220 * (1 - s.Z/5.0)), 40, 255);
            var ac = ManagedVisStyle.Accent().Color;
            ctx.DrawGeometry(new SolidColorBrush(Color.FromArgb(a, ac.R, ac.G, ac.B)), null, geo);
        }
    }

    private Star NewStar()
    {
        return new Star { X = _rng.NextDouble()*2-1, Y = _rng.NextDouble()*2-1, Z = 0.8 + _rng.NextDouble()*4.2 };
    }

    public string ConfigKey => "ManagedVis.Starfield.";
    public Control BuildOptionsView()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var count = new Slider { Minimum = 100, Maximum = 2000, Width = 200, Value = _countSetting };
        count.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { _countSetting = (int)count.Value; StateManager.Set(ConfigKey+"Count", _countSetting.ToString()); _stars = Array.Empty<Star>(); } };
        panel.Children.Add(new TextBlock { Text = "Stars:" }); panel.Children.Add(count);
        return panel;
    }
    public void LoadOptions()
    {
        if (int.TryParse(StateManager.Get(ConfigKey+"Count"), out var c)) _countSetting = Math.Clamp(c, 100, 2000);
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class JezzballPlugin : IPlugin
{
    private Window? _window;

    public string Name => "Jezzball";
    public string Description => "Simple Jezzball clone";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _window = new Window
        {
            Title = "Jezzball",
            Width = 500,
            Height = 400,
            Content = new JezzballControl()
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(JezzballPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }
}

internal class JezzballControl : Control
{
    private readonly List<Ball> _balls = new();
    private readonly List<Rect> _areas = new();
    private readonly List<Rect> _filled = new();
    private readonly DispatcherTimer _timer;
    private const double BallRadius = 8;

    public JezzballControl()
    {
        Width = 480;
        Height = 360;
        _areas.Add(new Rect(0,0,Width,Height));
        var rand = new Random();
        for (int i = 0; i < 2; i++)
        {
            _balls.Add(new Ball
            {
                X = rand.NextDouble() * (Width - BallRadius*2) + BallRadius,
                Y = rand.NextDouble() * (Height - BallRadius*2) + BallRadius,
                DX = rand.NextDouble() > 0.5 ? 2 : -2,
                DY = rand.NextDouble() > 0.5 ? 2 : -2
            });
        }
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_,_) => Tick();
        _timer.Start();
        AddHandler(PointerPressedEvent, PointerPressed, RoutingStrategies.Tunnel);
    }

    private new void PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(this);
        var area = _areas.FirstOrDefault(r => r.Contains(p));
        if (area == default) return;

        bool vertical = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        if (vertical)
        {
            if (_balls.Any(b => Math.Abs(b.X - p.X) <= BallRadius && b.Y >= area.Top && b.Y <= area.Bottom))
                return;
            var left = new Rect(area.Left, area.Top, p.X - area.Left, area.Height);
            var right = new Rect(p.X, area.Top, area.Right - p.X, area.Height);
            _areas.Remove(area);
            var leftHasBall = _balls.Any(b => left.Contains(new Point(b.X,b.Y)));
            var rightHasBall = _balls.Any(b => right.Contains(new Point(b.X,b.Y)));
            if (leftHasBall) _areas.Add(left); else _filled.Add(left);
            if (rightHasBall) _areas.Add(right); else _filled.Add(right);
        }
        else
        {
            if (_balls.Any(b => Math.Abs(b.Y - p.Y) <= BallRadius && b.X >= area.Left && b.X <= area.Right))
                return;
            var top = new Rect(area.Left, area.Top, area.Width, p.Y - area.Top);
            var bottom = new Rect(area.Left, p.Y, area.Width, area.Bottom - p.Y);
            _areas.Remove(area);
            var topHasBall = _balls.Any(b => top.Contains(new Point(b.X,b.Y)));
            var bottomHasBall = _balls.Any(b => bottom.Contains(new Point(b.X,b.Y)));
            if (topHasBall) _areas.Add(top); else _filled.Add(top);
            if (bottomHasBall) _areas.Add(bottom); else _filled.Add(bottom);
        }
        InvalidateVisual();
    }

    private void Tick()
    {
        foreach (var b in _balls)
        {
            var area = _areas.First(r => r.Contains(new Point(b.X,b.Y)));
            b.X += b.DX;
            b.Y += b.DY;
            if (b.X - BallRadius <= area.Left) { b.X = area.Left + BallRadius; b.DX *= -1; }
            if (b.X + BallRadius >= area.Right) { b.X = area.Right - BallRadius; b.DX *= -1; }
            if (b.Y - BallRadius <= area.Top) { b.Y = area.Top + BallRadius; b.DY *= -1; }
            if (b.Y + BallRadius >= area.Bottom) { b.Y = area.Bottom - BallRadius; b.DY *= -1; }
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        foreach (var f in _filled)
            context.FillRectangle(Brushes.DimGray, f);
        foreach (var a in _areas)
            context.DrawRectangle(null, new Pen(Brushes.White,1), a);
        foreach (var b in _balls)
            context.DrawEllipse(Brushes.Red, null, new Rect(b.X-BallRadius, b.Y-BallRadius, BallRadius*2, BallRadius*2));
    }

    private class Ball
    {
        public double X; public double Y; public double DX; public double DY;
    }
}

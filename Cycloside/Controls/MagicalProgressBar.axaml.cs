using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cycloside.Controls
{
    public partial class MagicalProgressBar : UserControl
    {
        public static readonly StyledProperty<double> ProgressProperty =
            AvaloniaProperty.Register<MagicalProgressBar, double>(nameof(Progress), 0d);

        public static readonly StyledProperty<bool> ShowPercentageProperty =
            AvaloniaProperty.Register<MagicalProgressBar, bool>(nameof(ShowPercentage), true);

        private readonly Random _random = new();
        private readonly List<Ellipse> _sparkles = new();
        private readonly List<double> _sparklePhases = new();
        private readonly List<double> _sparkleAnchors = new();
        private readonly List<double> _sparkleRows = new();
        private Border? _fillBorder;
        private Grid? _trackHost;
        private Canvas? _sparkleCanvas;
        private DispatcherTimer? _sparkleTimer;
        private double _sparkleTick;

        public double Progress
        {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public bool ShowPercentage
        {
            get => GetValue(ShowPercentageProperty);
            set => SetValue(ShowPercentageProperty, value);
        }

        public MagicalProgressBar()
        {
            InitializeComponent();

            this.GetObservable(ProgressProperty).Subscribe(_ => UpdateVisualState());
            this.GetObservable(BoundsProperty).Subscribe(_ => UpdateVisualState());

            AttachedToVisualTree += (_, _) =>
            {
                EnsureParts();
                InitializeSparkles();
                UpdateVisualState();
                StartSparkles();
            };

            DetachedFromVisualTree += (_, _) => StopSparkles();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void EnsureParts()
        {
            _fillBorder ??= this.FindControl<Border>("PART_Fill");
            _trackHost ??= this.FindControl<Grid>("PART_TrackHost");
            _sparkleCanvas ??= this.FindControl<Canvas>("PART_SparkleCanvas");
        }

        private void InitializeSparkles()
        {
            if (_sparkles.Count > 0)
            {
                return;
            }

            EnsureParts();

            var sparkleNames = new[]
            {
                "PART_Sparkle1",
                "PART_Sparkle2",
                "PART_Sparkle3",
                "PART_Sparkle4",
                "PART_Sparkle5"
            };

            foreach (var sparkleName in sparkleNames)
            {
                var sparkle = this.FindControl<Ellipse>(sparkleName);
                if (sparkle == null)
                {
                    continue;
                }

                _sparkles.Add(sparkle);
                _sparklePhases.Add(_random.NextDouble() * Math.PI * 2);
                _sparkleAnchors.Add(0.18 + (_random.NextDouble() * 0.64));
                _sparkleRows.Add(1 + (_random.NextDouble() * 6));
            }
        }

        private void StartSparkles()
        {
            if (_sparkleTimer != null)
            {
                return;
            }

            _sparkleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(95)
            };

            _sparkleTimer.Tick += OnSparkleTick;
            _sparkleTimer.Start();
        }

        private void StopSparkles()
        {
            if (_sparkleTimer == null)
            {
                return;
            }

            _sparkleTimer.Stop();
            _sparkleTimer.Tick -= OnSparkleTick;
            _sparkleTimer = null;
        }

        private void OnSparkleTick(object? sender, EventArgs e)
        {
            _sparkleTick += 0.18;
            UpdateSparkles(GetFillWidth());
        }

        private void UpdateVisualState()
        {
            EnsureParts();

            if (_fillBorder == null)
            {
                return;
            }

            var clampedProgress = Math.Clamp(Progress, 0d, 100d);
            var totalWidth = GetTrackWidth();
            var fillWidth = Math.Round(totalWidth * (clampedProgress / 100d), 2);

            _fillBorder.IsVisible = fillWidth > 0.5;
            _fillBorder.Width = fillWidth;

            UpdateSparkles(fillWidth);
        }

        private double GetTrackWidth()
        {
            if (_trackHost != null && _trackHost.Bounds.Width > 0)
            {
                return _trackHost.Bounds.Width;
            }

            var fallbackWidth = Bounds.Width - 8;
            return fallbackWidth > 0 ? fallbackWidth : 0;
        }

        private double GetFillWidth()
        {
            if (_fillBorder != null && _fillBorder.Width > 0)
            {
                return _fillBorder.Width;
            }

            return 0;
        }

        private void UpdateSparkles(double fillWidth)
        {
            if (_sparkleCanvas == null || _sparkles.Count == 0)
            {
                return;
            }

            var active = fillWidth > 18;
            _sparkleCanvas.IsVisible = active;

            if (!active)
            {
                foreach (var sparkle in _sparkles)
                {
                    sparkle.Opacity = 0;
                }

                return;
            }

            for (var index = 0; index < _sparkles.Count; index++)
            {
                var sparkle = _sparkles[index];
                var phase = _sparklePhases[index];
                var anchor = _sparkleAnchors[index];
                var row = _sparkleRows[index];
                var drift = Math.Sin(_sparkleTick + phase) * 7;
                var bob = Math.Cos((_sparkleTick * 1.35) + phase) * 2.2;
                var left = (fillWidth * anchor) + drift;
                var maxLeft = Math.Max(4, fillWidth - sparkle.Bounds.Width - 4);

                if (left < 4)
                {
                    left = 4;
                }

                if (left > maxLeft)
                {
                    left = maxLeft;
                }

                Canvas.SetLeft(sparkle, left);
                Canvas.SetTop(sparkle, row + bob);
                sparkle.Opacity = 0.25 + ((Math.Sin((_sparkleTick * 1.6) + phase) + 1) * 0.28);
            }
        }
    }
}

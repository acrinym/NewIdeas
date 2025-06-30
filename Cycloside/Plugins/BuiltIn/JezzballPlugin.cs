using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    #region Plugin Entry Point
    public class JezzballPlugin : IPlugin
    {
        private Window? _window;
        private JezzballControl? _control;

        public string Name => "Jezzball";
        public string Description => "A playable Jezzball clone with lives, time, and win conditions.";
        public Version Version => new(1, 3, 0); // Version bump for new features
        public Cycloside.Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _control = new JezzballControl();

            var restartButton = new Button { Content = "Restart", Margin = new Thickness(5) };
            restartButton.Click += (_, _) => _control.RestartGame();

            var layout = new DockPanel();
            DockPanel.SetDock(restartButton, Dock.Top);
            layout.Children.Add(restartButton);
            layout.Children.Add(_control);

            _window = new Window
            {
                Title = "Jezzball",
                Width = 800,
                Height = 600,
                CanResize = false, // Prevent resizing to keep the play area consistent
                Content = layout
            };
            ThemeManager.ApplyFromSettings(_window, nameof(JezzballPlugin));
            _window.KeyDown += OnWindowKeyDown;
            _window.Show();
        }

        public void Stop()
        {
            if (_window != null)
            {
                _window.KeyDown -= OnWindowKeyDown;
                (_window.Content as IDisposable)?.Dispose();
                _window.Close();
            }
            _window = null;
            _control = null;
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                _control?.RestartGame();
                e.Handled = true;
            }
        }
    }
    #endregion

    #region Game Model (State and Logic)

    public enum WallOrientation { Vertical, Horizontal }
    
    public enum BallType { Normal, Slow, Fast, Splitting }

    public class BuildingWall
    {
        public Rect Area { get; }
        public WallOrientation Orientation { get; }
        public Point Origin { get; }
        public Rect WallPart1 { get; set; }
        public Rect WallPart2 { get; set; }
        public bool IsPart1Active { get; set; } = true;
        public bool IsPart2Active { get; set; } = true;

        public BuildingWall(Rect area, Point origin, WallOrientation orientation)
        {
            Area = area;
            Origin = origin;
            Orientation = orientation;
            double thickness = 4;
            WallPart1 = new Rect(origin, new Size(thickness, thickness));
            WallPart2 = new Rect(origin, new Size(thickness, thickness));
        }
        
        public bool IsDead(IReadOnlyList<Ball> balls)
        {
            var part1HitBall = !IsPart1Active && (Orientation == WallOrientation.Vertical ? WallPart1.Top > Area.Top : WallPart1.Left > Area.Left);
            var part2HitBall = !IsPart2Active && (Orientation == WallOrientation.Vertical ? WallPart2.Bottom < Area.Bottom : WallPart2.Right < Area.Right);
            return part1HitBall || part2HitBall;
        }

        public bool IsComplete => !IsPart1Active && !IsPart2Active;
    }

    public class Ball
    {
        public Point Position { get; private set; }
        public Vector Velocity { get; private set; }
        public double Radius { get; }
        public IBrush Fill { get; }
        public BallType Type { get; }

        public Ball(Point position, Vector velocity, BallType type = BallType.Normal, double radius = 8)
        {
            Position = position;
            Velocity = velocity;
            Type = type;
            Radius = radius;

            switch (Type)
            {
                case BallType.Slow:
                    Fill = Brushes.DeepSkyBlue;
                    Velocity *= 0.7; 
                    break;
                case BallType.Fast:
                    Fill = Brushes.OrangeRed;
                    Velocity *= 1.3; 
                    break;
                case BallType.Splitting:
                    Fill = Brushes.MediumPurple;
                    break;
                default:
                    Fill = Brushes.Crimson;
                    break;
            }
        }

        public Rect BoundingBox => new(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);

        public void Update(Rect bounds, double dt)
        {
            Position += Velocity * dt;

            if ((Position.X - Radius < bounds.Left && Velocity.X < 0) || (Position.X + Radius > bounds.Right && Velocity.X > 0))
            {
                Velocity = Velocity.WithX(-Velocity.X);
            }
            if ((Position.Y - Radius < bounds.Top && Velocity.Y < 0) || (Position.Y + Radius > bounds.Bottom && Velocity.Y > 0))
            {
                Velocity = Velocity.WithY(-Velocity.Y);
            }

            Position = new Point(
                Math.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
                Math.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius)
            );
        }
    }

    public class JezzballGameState
    {
        public int Level { get; private set; } = 1;
        public int Lives { get; private set; } = 3;
        public long Score { get; private set; } = 0;
        public TimeSpan TimeLeft { get; private set; }
        public double CapturedPercentage { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public bool IsGameOver => Lives <= 0;

        public IReadOnlyList<Ball> Balls => _balls;
        public IReadOnlyList<Rect> ActiveAreas => _activeAreas;
        public IReadOnlyList<Rect> FilledAreas => _filledAreas;
        public BuildingWall? CurrentWall { get; private set; }

        private readonly List<Ball> _balls = new();
        private readonly List<Rect> _activeAreas = new();
        private readonly List<Rect> _filledAreas = new();
        private double _totalPlayArea;

        private const double WallSpeed = 150.0;
        private const double CaptureRequirement = 0.75;

        public JezzballGameState()
        {
            StartNewGame();
        }
        
        public void StartNewGame()
        {
            Level = 1;
            Lives = 3;
            Score = 0;
            StartLevel();
        }

        public void StartLevel()
        {
            _activeAreas.Clear();
            _filledAreas.Clear();
            _balls.Clear();
            CurrentWall = null;
            Message = $"Level {Level}";

            var bounds = new Rect(0, 0, 800, 570);
            _totalPlayArea = bounds.Width * bounds.Height;
            _activeAreas.Add(bounds);
            TimeLeft = TimeSpan.FromSeconds(20 + Level * 5);
            
            var rand = new Random();
            for (int i = 0; i < Level; i++)
            {
                var angle = rand.NextDouble() * 2 * Math.PI;
                var speed = 100 + Level * 10;
                var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                
                BallType type = BallType.Normal;
                if (Level > 3 && rand.NextDouble() > 0.8) type = BallType.Slow;
                if (Level > 5 && rand.NextDouble() > 0.8) type = BallType.Fast;
                if (Level > 7 && rand.NextDouble() > 0.9) type = BallType.Splitting;

                _balls.Add(new Ball(bounds.Center, velocity, type));
            }
            RecalculateCapturedArea();
        }
        
        public void HandleClick()
        {
            if (Message == string.Empty) return;

            if (IsGameOver)
            {
                StartNewGame();
            }
            else
            {
                StartLevel();
            }
            Message = string.Empty;
        }
        
        public void TryStartWall(Point position, WallOrientation orientation)
        {
            if (CurrentWall != null || Message != string.Empty) return;

            var area = _activeAreas.FirstOrDefault(r => r.Contains(position));
            if (area != default)
            {
                CurrentWall = new BuildingWall(area, position, orientation);
            }
        }

        public void Update(double dt)
        {
            if (Message != string.Empty) return;

            TimeLeft -= TimeSpan.FromSeconds(dt);
            if (TimeLeft <= TimeSpan.Zero)
            {
                LoseLife("Time's Up!");
                return;
            }

            UpdateBalls(dt);
            UpdateWall(dt);
        }

        private void UpdateBalls(double dt)
        {
            foreach (var ball in _balls.ToList())
            {
                var area = _activeAreas.FirstOrDefault(r => r.Intersects(ball.BoundingBox));
                if (area == default)
                {
                    area = _activeAreas.OrderBy(a => Math.Abs(a.Center.X - ball.Position.X) + Math.Abs(a.Center.Y - ball.Position.Y)).FirstOrDefault();
                }
                
                if (area != default)
                {
                    ball.Update(area, dt);
                }
            }
        }

        private void UpdateWall(double dt)
        {
            if (CurrentWall == null) return;

            double growAmount = WallSpeed * dt;

            if (CurrentWall.IsPart1Active)
            {
                var w1 = CurrentWall.WallPart1;
                w1 = CurrentWall.Orientation == WallOrientation.Vertical
                    ? new Rect(w1.X, w1.Y - growAmount, w1.Width, w1.Height + growAmount)
                    : new Rect(w1.X - growAmount, w1.Y, w1.Width + growAmount, w1.Height);
                
                if (CurrentWall.Orientation == WallOrientation.Vertical)
                {
                    if(w1.Top <= CurrentWall.Area.Top) { w1 = w1.WithY(CurrentWall.Area.Top); CurrentWall.IsPart1Active = false; }
                }
                else
                {
                     if(w1.Left <= CurrentWall.Area.Left) { w1 = w1.WithX(CurrentWall.Area.Left); CurrentWall.IsPart1Active = false; }
                }
                CurrentWall.WallPart1 = w1;

                if (_balls.Any(b => b.BoundingBox.Intersects(w1)))
                    CurrentWall.IsPart1Active = false;
            }

            if (CurrentWall.IsPart2Active)
            {
                var w2 = CurrentWall.WallPart2;
                w2 = CurrentWall.Orientation == WallOrientation.Vertical
                    ? new Rect(w2.X, w2.Y, w2.Width, w2.Height + growAmount)
                    : new Rect(w2.X, w2.Y, w2.Width + growAmount, w2.Height);

                if (CurrentWall.Orientation == WallOrientation.Vertical)
                {
                    if (w2.Bottom >= CurrentWall.Area.Bottom) { w2 = w2.WithHeight(CurrentWall.Area.Bottom - w2.Y); CurrentWall.IsPart2Active = false; }
                }
                else
                {
                    if (w2.Right >= CurrentWall.Area.Right) { w2 = w2.WithWidth(CurrentWall.Area.Right - w2.X); CurrentWall.IsPart2Active = false; }
                }
                CurrentWall.WallPart2 = w2;
                
                if (_balls.Any(b => b.BoundingBox.Intersects(w2)))
                    CurrentWall.IsPart2Active = false;
            }
            
            if (CurrentWall.IsDead(_balls))
            {
                 LoseLife("Wall Broken!");
            }
            else if (CurrentWall.IsComplete)
            {
                 CaptureAreas();
            }
        }
        
        private void LoseLife(string reason)
        {
            Lives--;
            CurrentWall = null;
            Message = Lives <= 0 ? "Game Over! Click to restart." : reason + " Click to continue.";
        }

        private void CaptureAreas()
        {
            if (CurrentWall == null) return;
            var area = CurrentWall.Area;
            Rect newArea1, newArea2;

            if (CurrentWall.Orientation == WallOrientation.Vertical)
            {
                newArea1 = new Rect(area.Left, area.Top, CurrentWall.Origin.X - area.Left, area.Height);
                newArea2 = new Rect(CurrentWall.Origin.X, area.Top, area.Right - CurrentWall.Origin.X, area.Height);
            }
            else
            {
                newArea1 = new Rect(area.Left, area.Top, area.Width, CurrentWall.Origin.Y - area.Top);
                newArea2 = new Rect(area.Left, CurrentWall.Origin.Y, area.Width, area.Bottom - CurrentWall.Origin.Y);
            }

            _activeAreas.Remove(area);
            
            var ballsToSplit = _balls.Where(b => b.Type == BallType.Splitting && (newArea1.Contains(b.Position) || newArea2.Contains(b.Position))).ToList();

            if (!_balls.Any(b => newArea1.Intersects(b.BoundingBox))) _filledAreas.Add(newArea1); else _activeAreas.Add(newArea1);
            if (!_balls.Any(b => newArea2.Intersects(b.BoundingBox))) _filledAreas.Add(newArea2); else _activeAreas.Add(newArea2);
            
            foreach (var ball in ballsToSplit)
            {
                _balls.Remove(ball);
                var rand = new Random();
                for (int i = 0; i < 2; i++)
                {
                    var angle = rand.NextDouble() * 2 * Math.PI;
                    var speed = 100 + Level * 5;
                    var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                    _balls.Add(new Ball(ball.Position, velocity, BallType.Normal, ball.Radius * 0.8));
                }
            }

            CurrentWall = null;
            RecalculateCapturedArea();

            if (CapturedPercentage >= CaptureRequirement)
            {
                Level++;
                long timeBonus = (long)TimeLeft.TotalSeconds * 100;
                Score += 1000 + timeBonus;
                Message = $"Level Complete!\nTime Bonus: {timeBonus}";
            }
        }

        private void RecalculateCapturedArea()
        {
            double filledAreaSum = _filledAreas.Sum(r => r.Width * r.Height);
            if (filledAreaSum > 0)
            {
                Score += (long)filledAreaSum / 100;
            }
            CapturedPercentage = _totalPlayArea > 0 ? filledAreaSum / _totalPlayArea : 0;
        }
    }
    #endregion

    #region Game View (UI Control)

    internal class JezzballControl : UserControl, IDisposable
    {
        private readonly JezzballGameState _gameState = new();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private Point _mousePosition;
        private WallOrientation _orientation = WallOrientation.Vertical;

        private readonly GameCanvas _gameCanvas;
        
        private readonly IBrush _backgroundBrush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            // FIXED: Replaced obsolete Radius property with RadiusX and RadiusY
            RadiusX = 0.8,
            RadiusY = 0.8,
            GradientStops = new GradientStops { new(Colors.DarkSlateBlue, 0), new(Colors.Black, 1) }
        };
        private readonly Pen _wallPen = new(Brushes.Cyan, 4, lineCap: PenLineCap.Round);
        private readonly Pen _previewPen = new(new SolidColorBrush(Colors.Yellow, 0.7), 2, DashStyle.Dash);
        private readonly IBrush _filledBrush = new SolidColorBrush(Color.FromRgb(0, 50, 70), 0.6);

        private readonly TextBlock _levelText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _livesText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _scoreText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _timeText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _capturedText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };

        public JezzballControl()
        {
            var statusBar = new DockPanel { Background = Brushes.Black, Height = 30, Opacity = 0.8 };
            DockPanel.SetDock(_levelText, Dock.Left);
            DockPanel.SetDock(_livesText, Dock.Left);
            DockPanel.SetDock(_scoreText, Dock.Left); 
            DockPanel.SetDock(_capturedText, Dock.Right);
            DockPanel.SetDock(_timeText, Dock.Right);
            statusBar.Children.AddRange(new Control[] { _levelText, _livesText, _scoreText, _capturedText, _timeText });

            var layout = new DockPanel();
            DockPanel.SetDock(statusBar, Dock.Bottom);
            layout.Children.Add(statusBar);
            _gameCanvas = new GameCanvas(this);
            layout.Children.Add(_gameCanvas);

            Content = layout;
            ClipToBounds = true;
            Focusable = true;

            _gameCanvas.PointerPressed += OnPointerPressed;
            _gameCanvas.PointerMoved += OnPointerMoved;
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; 
            _timer.Tick += GameTick;
            _timer.Start();
            _stopwatch.Start();
        }

        public void RestartGame() => _gameState.StartNewGame();

        public void Dispose()
        {
            _timer.Stop();
            _gameCanvas.PointerPressed -= OnPointerPressed;
            _gameCanvas.PointerMoved -= OnPointerMoved;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e) => _mousePosition = e.GetPosition(_gameCanvas);

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(_gameCanvas);
            if (point.Properties.IsRightButtonPressed)
            {
                _orientation = _orientation == WallOrientation.Vertical ? WallOrientation.Horizontal : WallOrientation.Vertical;
                return;
            }
            if (point.Properties.IsLeftButtonPressed)
            {
                if (_gameState.Message != string.Empty)
                    _gameState.HandleClick();
                else
                    _gameState.TryStartWall(_mousePosition, _orientation);
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            var dt = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            _gameState.Update(dt);
            UpdateStatusText();
            _gameCanvas.InvalidateVisual();
        }

        private void UpdateStatusText()
        {
            _levelText.Text = $"Level: {_gameState.Level}";
            _livesText.Text = $"Lives: {_gameState.Lives}";
            _scoreText.Text = $"Score: {_gameState.Score}";
            _timeText.Text = $"Time: {Math.Max(0, (int)_gameState.TimeLeft.TotalSeconds)}";
            _capturedText.Text = $"Captured: {_gameState.CapturedPercentage:P0}";
        }

        internal void RenderGame(DrawingContext context)
        {
            context.FillRectangle(_backgroundBrush, _gameCanvas.Bounds);

            foreach (var area in _gameState.FilledAreas) context.FillRectangle(_filledBrush, area);
            
            var gridPen = new Pen(new SolidColorBrush(Colors.White, 0.1), 1);
            foreach(var area in _gameState.ActiveAreas)
            {
                for (double x = area.Left; x < area.Right; x += 20) context.DrawLine(gridPen, new Point(x, area.Top), new Point(x, area.Bottom));
                for (double y = area.Top; y < area.Bottom; y += 20) context.DrawLine(gridPen, new Point(area.Left, y), new Point(area.Right, y));
            }
            
            foreach (var ball in _gameState.Balls)
            {
                context.DrawEllipse(ball.Fill, null, ball.Position, ball.Radius, ball.Radius);
            }
            
            if (_gameState.CurrentWall is { } wall)
            {
                if (wall.IsPart1Active) context.DrawLine(_wallPen, wall.Origin, wall.WallPart1.TopLeft);
                if (wall.IsPart2Active) context.DrawLine(_wallPen, wall.Origin, wall.WallPart2.BottomRight);
            }
            else if (_gameState.Message == string.Empty)
            {
                var area = _gameState.ActiveAreas.FirstOrDefault(r => r.Contains(_mousePosition));
                if (area != default)
                {
                    if (_orientation == WallOrientation.Vertical)
                        context.DrawLine(_previewPen, new Point(_mousePosition.X, area.Top), new Point(_mousePosition.X, area.Bottom));
                    else
                        context.DrawLine(_previewPen, new Point(area.Left, _mousePosition.Y), new Point(area.Right, _mousePosition.Y));
                }
            }

            if (_gameState.Message != string.Empty)
            {
                context.FillRectangle(new SolidColorBrush(Colors.Black, 0.5), _gameCanvas.Bounds);
                
                var formatted = new FormattedText(
                    _gameState.Message,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                    36,
                    Brushes.WhiteSmoke);

                var textPos = new Point(
                    (_gameCanvas.Bounds.Width - formatted.Width) / 2,
                    (_gameCanvas.Bounds.Height - formatted.Height) / 2);

                context.DrawText(formatted, textPos);
            }
        }

        private class GameCanvas : Control
        {
            private readonly JezzballControl _parent;
            public GameCanvas(JezzballControl parent) => _parent = parent;
            public override void Render(DrawingContext context) => _parent.RenderGame(context);
        }
    }
    #endregion
}

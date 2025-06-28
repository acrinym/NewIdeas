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

namespace Cycloside.Plugins.BuiltIn
{
    #region Plugin Entry Point
    public class JezzballPlugin : IPlugin
    {
        private Window? _window;

        public string Name => "Jezzball";
        public string Description => "A playable Jezzball clone with lives, time, and win conditions.";
        public Version Version => new(1, 2, 0); // Version bump for major refactor
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new Window
            {
                Title = "Jezzball",
                Width = 800,
                Height = 600,
                Content = new JezzballControl() // The Control is now much simpler
            };
            _window.Show();
        }

        public void Stop()
        {
            // The control handles its own timer shutdown now via IDisposable
            (_window?.Content as IDisposable)?.Dispose();
            _window?.Close();
            _window = null;
        }
    }
    #endregion

    #region Game Model (State and Logic)

    public enum WallOrientation { Vertical, Horizontal }

    /// <summary>
    /// Represents a wall being built.
    /// </summary>
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
            WallPart1 = new Rect(origin, new Size(2, 2));
            WallPart2 = new Rect(origin, new Size(2, 2));
        }

        public bool IsDead => !IsPart1Active && !IsPart2Active;
        public bool IsComplete => !IsPart1Active && !IsPart2Active; // True if both hit walls
    }

    /// <summary>
    /// Represents a single ball.
    /// </summary>
    public class Ball
    {
        public Point Position { get; private set; }
        public Vector Velocity { get; private set; }
        public static double Radius { get; } = 8;

        public Ball(Point position, Vector velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        public Rect BoundingBox => new(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);

        public void Update(Rect bounds, double dt)
        {
            Position += Velocity * dt;

            if (Position.X - Radius < bounds.Left && Velocity.X < 0) Velocity = Velocity.WithX(-Velocity.X);
            if (Position.X + Radius > bounds.Right && Velocity.X > 0) Velocity = Velocity.WithX(-Velocity.X);
            if (Position.Y - Radius < bounds.Top && Velocity.Y < 0) Velocity = Velocity.WithY(-Velocity.Y);
            if (Position.Y + Radius > bounds.Bottom && Velocity.Y > 0) Velocity = Velocity.WithY(-Velocity.Y);

            // Clamp position to prevent escaping bounds
            Position = new Point(
                Math.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
                Math.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius)
            );
        }
    }

    /// <summary>
    /// The "Engine". Contains all game state and pure logic, with no UI knowledge.
    /// </summary>
    public class JezzballGameState
    {
        // --- State Properties ---
        public int Level { get; private set; } = 1;
        public int Lives { get; private set; } = 3;
        public TimeSpan TimeLeft { get; private set; }
        public double CapturedPercentage { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public IReadOnlyList<Ball> Balls => _balls;
        public IReadOnlyList<Rect> ActiveAreas => _activeAreas;
        public IReadOnlyList<Rect> FilledAreas => _filledAreas;
        public BuildingWall? CurrentWall { get; private set; }

        // --- Private State ---
        private readonly List<Ball> _balls = new();
        private readonly List<Rect> _activeAreas = new();
        private readonly List<Rect> _filledAreas = new();
        private double _totalPlayArea;

        // --- Constants ---
        private const double WallSpeed = 120.0; // Per second
        private const double CaptureRequirement = 0.75; // 75%

        public JezzballGameState()
        {
            StartNewGame();
        }
        
        public void StartNewGame()
        {
            Level = 1;
            Lives = 3;
            StartLevel();
        }

        public void StartLevel()
        {
            _activeAreas.Clear();
            _filledAreas.Clear();
            _balls.Clear();
            CurrentWall = null;
            Message = $"Level {Level}";

            // Assume initial bounds are 800x570 (window minus status bar)
            var bounds = new Rect(0, 0, 800, 570);
            _totalPlayArea = bounds.Width * bounds.Height;
            _activeAreas.Add(bounds);
            TimeLeft = TimeSpan.FromSeconds(30 + Level * 2);
            
            var rand = new Random();
            for (int i = 0; i < Level; i++)
            {
                var angle = rand.NextDouble() * 2 * Math.PI;
                var speed = 90 + Level * 10;
                var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                _balls.Add(new Ball(bounds.Center, velocity));
            }
            RecalculateCapturedArea();
        }
        
        public void ClearMessageAndRestartIfGameOver()
        {
            if (Message == string.Empty) return;
            
            Message = string.Empty;
            if (Lives <= 0)
            {
                StartNewGame();
            }
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
            if (Message != string.Empty) return; // Game is paused

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
            foreach (var ball in _balls)
            {
                var area = _activeAreas.FirstOrDefault(r => r.Contains(ball.Position));
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

            // Grow Part 1
            if (CurrentWall.IsPart1Active)
            {
                var w1 = CurrentWall.WallPart1;
                w1 = CurrentWall.Orientation == WallOrientation.Vertical
                    ? new Rect(w1.X, w1.Y - growAmount, w1.Width, w1.Height + growAmount)
                    : new Rect(w1.X - growAmount, w1.Y, w1.Width + growAmount, w1.Height);
                CurrentWall.WallPart1 = w1;

                if (CurrentWall.Orientation == WallOrientation.Vertical ? w1.Top <= CurrentWall.Area.Top : w1.Left <= CurrentWall.Area.Left)
                    CurrentWall.IsPart1Active = false; // Reached boundary
                else if (_balls.Any(b => b.BoundingBox.Intersects(w1)))
                    CurrentWall.IsPart1Active = false; // Hit a ball
            }
            // Grow Part 2
            if (CurrentWall.IsPart2Active)
            {
                var w2 = CurrentWall.WallPart2;
                w2 = CurrentWall.Orientation == WallOrientation.Vertical
                    ? new Rect(w2.X, w2.Y, w2.Width, w2.Height + growAmount)
                    : new Rect(w2.X, w2.Y, w2.Width + growAmount, w2.Height);
                CurrentWall.WallPart2 = w2;
                
                if (CurrentWall.Orientation == WallOrientation.Vertical ? w2.Bottom >= CurrentWall.Area.Bottom : w2.Right >= CurrentWall.Area.Right)
                    CurrentWall.IsPart2Active = false; // Reached boundary
                else if (_balls.Any(b => b.BoundingBox.Intersects(w2)))
                    CurrentWall.IsPart2Active = false; // Hit a ball
            }
            
            if (CurrentWall.IsComplete) CaptureAreas();
            else if (CurrentWall.IsDead) LoseLife("Wall Broken!");
        }
        
        private void LoseLife(string reason)
        {
            Lives--;
            CurrentWall = null;
            Message = Lives <= 0 ? "Game Over! Click to restart." : reason;

            if (Lives > 0)
            {
                // Restart level state, but keep lives
                var currentLives = Lives;
                StartLevel();
                Lives = currentLives;
            }
        }

        private void CaptureAreas()
        {
            if (CurrentWall == null) return;
            var area = CurrentWall.Area;
            Rect newArea1, newArea2;

            if (CurrentWall.Orientation == WallOrientation.Vertical)
            {
                newArea1 = new Rect(area.Left, area.Top,
                    CurrentWall.Origin.X - area.Left,
                    area.Height);
                newArea2 = new Rect(CurrentWall.Origin.X, area.Top,
                    area.Right - CurrentWall.Origin.X,
                    area.Height);
            }
            else // Horizontal
            {
                newArea1 = new Rect(area.Left, area.Top,
                    area.Width,
                    CurrentWall.Origin.Y - area.Top);
                newArea2 = new Rect(area.Left, CurrentWall.Origin.Y,
                    area.Width,
                    area.Bottom - CurrentWall.Origin.Y);
            }

            _activeAreas.Remove(area);

            if (_balls.Any(b => newArea1.Contains(b.Position))) _activeAreas.Add(newArea1); else _filledAreas.Add(newArea1);
            if (_balls.Any(b => newArea2.Contains(b.Position))) _activeAreas.Add(newArea2); else _filledAreas.Add(newArea2);
            
            CurrentWall = null;
            RecalculateCapturedArea();

            if (CapturedPercentage >= CaptureRequirement)
            {
                Level++;
                Message = "Level Complete!";
            }
        }

        private void RecalculateCapturedArea()
        {
            double filledAreaSum = _filledAreas.Sum(r => r.Width * r.Height);
            CapturedPercentage = _totalPlayArea > 0 ? filledAreaSum / _totalPlayArea : 0;
        }
    }
    #endregion

    #region Game View (UI Control)

    /// <summary>
    /// The "View". Manages input, rendering, and the game loop timer.
    /// It holds the game state but delegates all logic to it.
    /// </summary>
    internal class JezzballControl : UserControl, IDisposable
    {
        // --- State and Timing ---
        private readonly JezzballGameState _gameState = new();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private Point _mousePosition;
        private WallOrientation _orientation = WallOrientation.Vertical;

        private readonly GameCanvas _gameCanvas;

        // --- UI Controls ---
        private readonly TextBlock _levelText = new() { Margin = new Thickness(10, 0) };
        private readonly TextBlock _livesText = new() { Margin = new Thickness(10, 0) };
        private readonly TextBlock _timeText = new() { Margin = new Thickness(10, 0) };
        private readonly TextBlock _capturedText = new() { Margin = new Thickness(10, 0) };

        public JezzballControl()
        {
            // --- UI Setup ---
            var statusBar = new DockPanel { Background = Brushes.DarkSlateGray, Height = 30 };
            DockPanel.SetDock(_levelText, Dock.Left);
            DockPanel.SetDock(_livesText, Dock.Left);
            DockPanel.SetDock(_capturedText, Dock.Right);
            DockPanel.SetDock(_timeText, Dock.Right);
            statusBar.Children.AddRange(new Control[] { _levelText, _livesText, _capturedText, _timeText });

            var layout = new DockPanel();
            DockPanel.SetDock(statusBar, Dock.Bottom);
            layout.Children.Add(statusBar);
            _gameCanvas = new GameCanvas(this);
            layout.Children.Add(_gameCanvas); // The game itself fills the rest

            Content = layout;
            ClipToBounds = true;
            Focusable = true;

            // --- Event Handlers ---
            _gameCanvas.PointerPressed += OnPointerPressed;
            _gameCanvas.PointerMoved += OnPointerMoved;
            
            // --- Game Loop Start ---
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 FPS
            _timer.Tick += GameTick;
            _timer.Start();
            _stopwatch.Start();
        }

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
                    _gameState.ClearMessageAndRestartIfGameOver();
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
            _gameCanvas.InvalidateVisual(); // Trigger a re-render
        }

        private void UpdateStatusText()
        {
            _levelText.Text = $"Level: {_gameState.Level}";
            _livesText.Text = $"Lives: {_gameState.Lives}";
            _timeText.Text = $"Time: {_gameState.TimeLeft:ss}";
            _capturedText.Text = $"Captured: {_gameState.CapturedPercentage:P0}";
        }

        internal void RenderGame(DrawingContext context)
        {
            context.FillRectangle(Brushes.Black, _gameCanvas.Bounds);

            // Draw Areas
            foreach (var area in _gameState.FilledAreas) context.FillRectangle(Brushes.DarkCyan, area);
            foreach (var area in _gameState.ActiveAreas) context.DrawRectangle(new Pen(Brushes.SlateGray, 1), area);

            // Draw Balls
            foreach (var ball in _gameState.Balls)
            {
            // Center of the ball is its Position, Brush is the color, Radius is used for both X and Y.
            context.DrawEllipse(Brushes.Crimson, null, ball.Position, Ball.Radius, Ball.Radius);
            }
            
            // Draw Building Wall
            if (_gameState.CurrentWall is { } wall)
            {
                var buildingPen = new Pen(Brushes.Cyan, 2);
                if (wall.IsPart1Active) context.DrawRectangle(buildingPen, wall.WallPart1);
                if (wall.IsPart2Active) context.DrawRectangle(buildingPen, wall.WallPart2);
            }
            // Draw Preview Wall
            else if (_gameState.Message == string.Empty)
            {
                var area = _gameState.ActiveAreas.FirstOrDefault(r => r.Contains(_mousePosition));
                if (area != default)
                {
                    var previewPen = new Pen(Brushes.Yellow, 1, DashStyle.Dash);
                    if (_orientation == WallOrientation.Vertical)
                        context.DrawLine(previewPen, new Point(_mousePosition.X, area.Top), new Point(_mousePosition.X, area.Bottom));
                    else
                        context.DrawLine(previewPen, new Point(area.Left, _mousePosition.Y), new Point(area.Right, _mousePosition.Y));
                }
            }

            // Draw Message
            if (_gameState.Message != string.Empty)
            {
                var formatted = new FormattedText(
                    _gameState.Message,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily, FontStyle.Normal, FontWeight.Bold),
                    48,
                    Brushes.White);

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

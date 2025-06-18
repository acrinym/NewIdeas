using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn
{
    public class JezzballPlugin : IPlugin
    {
        private Window? _window;

        public string Name => "Jezzball";
        public string Description => "A playable Jezzball clone with lives, time, and win conditions.";
        public Version Version => new(1, 1, 0); // Version bump for major gameplay implementation
        public Widgets.IWidget? Widget => null;

        public void Start()
        {
            _window = new Window
            {
                Title = "Jezzball",
                Width = 800,
                Height = 600,
                Content = new JezzballControl()
            };
            // Assuming these are your custom manager classes
            // ThemeManager.ApplyFromSettings(_window, "Plugins");
            // WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(JezzballPlugin));
            _window.Show();
        }

        public void Stop()
        {
            if (_window?.Content is JezzballControl jc)
            {
                jc.StopTimer();
            }
            _window?.Close();
            _window = null;
        }
    }

    // --- Helper Classes for Game State ---

    internal enum WallOrientation
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// Manages the state of a wall as it's being built.
    /// Crucially, it tracks the two growing halves independently.
    /// </summary>
    internal class BuildingWall
    {
        public Rect Area { get; }
        public WallOrientation Orientation { get; }
        public Point Origin { get; }

        public Rect WallPart1 { get; set; }
        public Rect WallPart2 { get; set; }

        // State flags for each half of the wall
        public bool IsPart1Active { get; set; } = true;
        public bool IsPart2Active { get; set; } = true;
        public bool IsPart1Solid { get; set; } = false;
        public bool IsPart2Solid { get; set; } = false;

        public BuildingWall(Rect area, Point origin, WallOrientation orientation)
        {
            Area = area;
            Origin = origin;
            Orientation = orientation;
            WallPart1 = new Rect(origin, origin);
            WallPart2 = new Rect(origin, origin);
        }

        /// <summary>
        /// The wall is "dead" and should be removed if both halves have been destroyed.
        /// </summary>
        public bool IsDead => !IsPart1Active && !IsPart2Active;

        /// <summary>
        /// The wall is "complete" and should trigger an area capture if both halves are solid.
        /// </summary>
        public bool IsComplete => IsPart1Solid && IsPart2Solid;
    }

    // --- Main Game Control ---

    internal class JezzballControl : Control
    {
        // Game State
        private readonly List<Ball> _balls = new();
        private readonly List<Rect> _activeAreas = new();
        private readonly List<Rect> _filledAreas = new();
        private BuildingWall? _currentWall;
        private int _level = 1;
        private int _lives = 3;
        private TimeSpan _timeLeft;
        private double _totalPlayArea;
        private double _capturedPercentage;
        private string _message = string.Empty;

        // UI Controls for Status
        private readonly TextBlock _levelText = new TextBlock();
        private readonly TextBlock _livesText = new TextBlock();
        private readonly TextBlock _timeText = new TextBlock();
        private readonly TextBlock _capturedText = new TextBlock();
        private readonly TextBlock _messageText = new TextBlock { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

        // Input and Timing
        private readonly DispatcherTimer _timer;
        private Point _mousePosition;
        private WallOrientation _orientation = WallOrientation.Vertical;

        // Constants
        private const double BallRadius = 8;
        private const double WallSpeed = 2.0;
        private const double CaptureRequirement = 0.75; // 75%

        public JezzballControl()
        {
            // --- UI Setup ---
            var statusBar = new DockPanel
            {
                Background = Brushes.Black,
                Height = 30
            };
            DockPanel.SetDock(_levelText, Dock.Left);
            DockPanel.SetDock(_livesText, Dock.Left);
            DockPanel.SetDock(_capturedText, Dock.Right);
            DockPanel.SetDock(_timeText, Dock.Right);
            statusBar.Children.Add(_levelText);
            statusBar.Children.Add(_livesText);
            statusBar.Children.Add(_capturedText);
            statusBar.Children.Add(_timeText);
            
            var gameCanvas = new Canvas(); // Use a Canvas to layer the message over the game
            var gamePanel = new Panel { Children = { this, _messageText } };

            var mainLayout = new DockPanel();
            DockPanel.SetDock(statusBar, Dock.Bottom);
            mainLayout.Children.Add(statusBar);
            mainLayout.Children.Add(gamePanel);

            // Assign the layout to the parent window
            this.Parent.EffectiveVisualChildren.OfType<Window>().FirstOrDefault()!.Content = mainLayout;
            this.Focusable = true; // Make the game area focusable

            // --- Event Handlers ---
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;

            // --- Game Start ---
            StartLevel();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // Approx 60 FPS
            _timer.Tick += (_, _) => GameTick();
            _timer.Start();
        }

        public void StopTimer() => _timer.Stop();

        private void StartLevel()
        {
            _activeAreas.Clear();
            _filledAreas.Clear();
            _balls.Clear();
            _currentWall = null;
            _message = $"Level {_level}";
            
            var bounds = this.Bounds;
            _totalPlayArea = bounds.Width * bounds.Height;
            _activeAreas.Add(bounds);

            _timeLeft = TimeSpan.FromSeconds(30 + _level * 2); // Time increases with level

            var rand = new Random();
            for (int i = 0; i < _level; i++) // Number of balls equals the level number
            {
                var area = _activeAreas.First();
                _balls.Add(new Ball
                {
                    X = rand.NextDouble() * (area.Width - BallRadius * 2) + BallRadius,
                    Y = rand.NextDouble() * (area.Height - BallRadius * 2) + BallRadius,
                    DX = (rand.NextDouble() > 0.5 ? 1 : -1) * (1.5 + _level * 0.1), // Speed increases with level
                    DY = (rand.NextDouble() > 0.5 ? 1 : -1) * (1.5 + _level * 0.1)
                });
            }
            RecalculateCapturedArea();
        }

        // --- Input Handling ---
        private void OnPointerMoved(object? sender, PointerEventArgs e) => _mousePosition = e.GetPosition(this);
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_message != string.Empty) // Don't allow play while message is shown
            {
                _message = string.Empty;
                if (_lives <= 0)
                {
                    _level = 1;
                    _lives = 3;
                    StartLevel();
                }
                return;
            }

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsRightButtonPressed)
            {
                _orientation = _orientation == WallOrientation.Vertical ? WallOrientation.Horizontal : WallOrientation.Vertical;
                return;
            }

            if (point.Properties.IsLeftButtonPressed && _currentWall == null)
            {
                var area = _activeAreas.FirstOrDefault(r => r.Contains(_mousePosition));
                if (area != default)
                    _currentWall = new BuildingWall(area, _mousePosition, _orientation);
            }
        }

        // --- Game Logic ---
        private void GameTick()
        {
            if (_message != string.Empty)
            {
                // If a message is displayed, pause the game
                UpdateStatusText();
                InvalidateVisual();
                return;
            }

            _timeLeft = _timeLeft.Subtract(TimeSpan.FromMilliseconds(16));
            if (_timeLeft.TotalSeconds <= 0)
            {
                LoseLife("Time's Up!");
                return;
            }
            
            UpdateWall();
            UpdateBalls();
            UpdateStatusText();
            InvalidateVisual();
        }

        private void UpdateWall()
        {
            if (_currentWall == null) return;

            // --- Grow Part 1 ---
            if (_currentWall.IsPart1Active)
            {
                var w1 = _currentWall.WallPart1;
                w1 = _currentWall.Orientation == WallOrientation.Vertical
                    ? w1.WithY(w1.Y - WallSpeed).WithHeight(w1.Height + WallSpeed)
                    : w1.WithX(w1.X - WallSpeed).WithWidth(w1.Width + WallSpeed);
                _currentWall.WallPart1 = w1;

                // Check for completion or collision
                if (w1.Intersects(_currentWall.Area) && (_currentWall.Orientation == WallOrientation.Vertical ? w1.Top <= _currentWall.Area.Top : w1.Left <= _currentWall.Area.Left))
                {
                    _currentWall.IsPart1Solid = true;
                    _currentWall.IsPart1Active = false;
                }
                else if (_balls.Any(b => b.IntersectsWith(w1)))
                {
                    _currentWall.IsPart1Active = false;
                }
            }

            // --- Grow Part 2 ---
            if (_currentWall.IsPart2Active)
            {
                var w2 = _currentWall.WallPart2;
                w2 = _currentWall.Orientation == WallOrientation.Vertical
                    ? w2.WithHeight(w2.Height + WallSpeed)
                    : w2.WithWidth(w2.Width + WallSpeed);
                _currentWall.WallPart2 = w2;

                // Check for completion or collision
                if (w2.Intersects(_currentWall.Area) && (_currentWall.Orientation == WallOrientation.Vertical ? w2.Bottom >= _currentWall.Area.Bottom : w2.Right >= _currentWall.Area.Right))
                {
                    _currentWall.IsPart2Solid = true;
                    _currentWall.IsPart2Active = false;
                }
                else if (_balls.Any(b => b.IntersectsWith(w2)))
                {
                    _currentWall.IsPart2Active = false;
                }
            }
            
            // --- Check Overall Wall State ---
            if (_currentWall.IsComplete)
            {
                CaptureAreas();
            }
            else if (_currentWall.IsDead)
            {
                LoseLife("Wall Broken!");
            }
        }
        
        private void LoseLife(string reason)
        {
            _lives--;
            _message = reason;
            _currentWall = null;

            if (_lives <= 0)
            {
                _message = "Game Over! Click to restart.";
            }
            else
            {
                // Quick delay then restart the level
                Dispatcher.UIThread.Post(StartLevel, DispatcherPriority.Background);
            }
        }

        private void CaptureAreas()
        {
            if (_currentWall == null) return;
            var area = _currentWall.Area;
            Rect newArea1, newArea2;

            if (_currentWall.Orientation == WallOrientation.Vertical)
            {
                newArea1 = new Rect(area.Left, area.Top, _currentWall.Origin.X - area.Left, area.Height);
                newArea2 = new Rect(_currentWall.Origin.X, area.Top, area.Right - _currentWall.Origin.X, area.Height);
            }
            else
            {
                newArea1 = new Rect(area.Left, area.Top, area.Width, _currentWall.Origin.Y - area.Top);
                newArea2 = new Rect(area.Left, _currentWall.Origin.Y, area.Width, area.Bottom - _currentWall.Origin.Y);
            }

            _activeAreas.Remove(area);

            if (_balls.Any(b => newArea1.Contains(b.Position))) _activeAreas.Add(newArea1); else _filledAreas.Add(newArea1);
            if (_balls.Any(b => newArea2.Contains(b.Position))) _activeAreas.Add(newArea2); else _filledAreas.Add(newArea2);

            _currentWall = null;
            RecalculateCapturedArea();

            if (_capturedPercentage >= CaptureRequirement)
            {
                _level++;
                _message = "Level Complete!";
                Dispatcher.UIThread.Post(StartLevel, DispatcherPriority.Background);
            }
        }

        private void UpdateBalls()
        {
            foreach (var b in _balls)
            {
                var area = _activeAreas.FirstOrDefault(r => r.Contains(b.Position));
                if (area == default) continue;
                b.Update(area);
            }
        }

        private void RecalculateCapturedArea()
        {
            double filledAreaSum = _filledAreas.Sum(r => r.Width * r.Height);
            _capturedPercentage = _totalPlayArea > 0 ? filledAreaSum / _totalPlayArea : 0;
        }

        private void UpdateStatusText()
        {
            _levelText.Text = $"Level: {_level}";
            _livesText.Text = $"Lives: {_lives}";
            _timeText.Text = $"Time: {_timeLeft:ss}";
            _capturedText.Text = $"Captured: {_capturedPercentage:P0}";
            _messageText.Text = _message;
        }

        // --- Rendering ---
        public override void Render(DrawingContext context)
        {
            // Draw a solid background
            context.FillRectangle(Brushes.Black, this.Bounds);
            base.Render(context);
            
            foreach (var f in _filledAreas) context.FillRectangle(Brushes.DarkSlateGray, f, 4);
            foreach (var a in _activeAreas) context.DrawRectangle(new Pen(Brushes.Gray, 1), a, 4);
            foreach (var b in _balls) context.DrawEllipse(Brushes.Crimson, null, b.Position, BallRadius, BallRadius);
            
            if (_currentWall != null)
            {
                var pen = new Pen(Brushes.Cyan, 2);
                if(_currentWall.IsPart1Active || _currentWall.IsPart1Solid)
                    context.DrawLine(pen, _currentWall.WallPart1.TopLeft, _currentWall.WallPart1.BottomRight);
                if(_currentWall.IsPart2Active || _currentWall.IsPart2Solid)
                    context.DrawLine(pen, _currentWall.WallPart2.TopLeft, _currentWall.WallPart2.BottomRight);
            }
            else if (_message == string.Empty) // Only show preview when playing
            {
                var area = _activeAreas.FirstOrDefault(r => r.Contains(_mousePosition));
                if (area != default)
                {
                    var pen = new Pen(Brushes.Yellow, 1, new DashStyle(new[] { 4.0, 4.0 }, 0));
                    if (_orientation == WallOrientation.Vertical)
                        context.DrawLine(pen, new Point(_mousePosition.X, area.Top), new Point(_mousePosition.X, area.Bottom));
                    else
                        context.DrawLine(pen, new Point(area.Left, _mousePosition.Y), new Point(area.Right, _mousePosition.Y));
                }
            }
        }
    }

    // Refactored Ball class to be more self-contained
    internal class Ball
    {
        public double X, Y, DX, DY;
        public Point Position => new Point(X, Y);
        private const double BallRadius = 8;
        
        public bool IntersectsWith(Rect rect) => rect.Intersects(new Rect(X - BallRadius, Y - BallRadius, BallRadius * 2, BallRadius * 2));

        public void Update(Rect bounds)
        {
            X += DX;
            Y += DY;
            if (X - BallRadius <= bounds.Left) { X = bounds.Left + BallRadius; DX *= -1; }
            if (X + BallRadius >= bounds.Right) { X = bounds.Right - BallRadius; DX *= -1; }
            if (Y - BallRadius <= bounds.Top) { Y = bounds.Top + BallRadius; DY *= -1; }
            if (Y + BallRadius >= bounds.Bottom) { Y = bounds.Bottom - BallRadius; DY *= -1; }
        }
    }
}

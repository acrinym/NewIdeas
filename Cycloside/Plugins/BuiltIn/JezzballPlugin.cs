using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Plugins;

namespace Cycloside.Plugins.BuiltIn
{
    public class JezzballPlugin : IPlugin
    {
        private Window? _window;
        private JezzballControl? _control;

        public string Name => "Jezzball";
        public string Description => "A fresh Jezzball clone based on the Canvas project";
        public Version Version => new(2, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _control = new JezzballControl();

            _window = new Window
            {
                Title = "Jezzball",
                Width = 800,
                Height = 630,
                CanResize = true,
                MinWidth = 640,
                MinHeight = 480,
                Content = _control
            };

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
            switch (e.Key)
            {
                case Key.R:
                    _control?.RestartGame();
                    e.Handled = true;
                    break;
                case Key.Space:
                    _control?.TogglePause();
                    e.Handled = true;
                    break;
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        private void ShowHelp()
        {
            var helpWindow = new Window
            {
                Title = "Jezzball Help",
                Width = 500,
                Height = 400,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = @"Jezzball Help

Goal: Build walls to capture at least 75% of the area while avoiding balls.

Controls:
- Left Click: Place a wall
- Right Click: Change wall direction (Horizontal/Vertical)
- R: Restart game
- Space: Pause/Unpause
- F1: Show this help

Rules:
- Click to create wall segments that grow in both directions
- If a ball hits a wall while it's growing, you lose a life
- Enclosed areas without balls count toward your score
- Clear 75% of the area to advance to the next level
- Each level adds more balls
- Game ends when you run out of lives

Tips:
- Plan your walls carefully
- Watch ball trajectories
- Use the grid to help with placement
- Don't place walls too close to balls",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20)
                    }
                }
            };
            helpWindow.Show();
        }
    }

    public enum JezzballSoundEvent
    {
        Click,
        WallBuild,
        WallHit,
        WallBreak,
        BallBounce,
        LevelComplete
    }

    public class Ball
    {
        public Point Position { get; private set; }
        public Vector Velocity { get; private set; }
        public double Radius { get; } = 8;
        public int Id { get; }

        public Ball(int id, Point position, Vector velocity)
        {
            Id = id;
            Position = position;
            Velocity = velocity;
        }

        public void Update(double dt, Rect bounds)
        {
            Position += Velocity * dt;

            // Bounce off walls
            if (Position.X - Radius <= bounds.Left || Position.X + Radius >= bounds.Right)
            {
                Velocity = new Vector(-Velocity.X, Velocity.Y);
                Position = new Point(
                    Math.Max(bounds.Left + Radius, Math.Min(bounds.Right - Radius, Position.X)),
                    Position.Y
                );
            }

            if (Position.Y - Radius <= bounds.Top || Position.Y + Radius >= bounds.Bottom)
            {
                Velocity = new Vector(Velocity.X, -Velocity.Y);
                Position = new Point(
                    Position.X,
                    Math.Max(bounds.Top + Radius, Math.Min(bounds.Bottom - Radius, Position.Y))
                );
            }
        }

        public void Draw(DrawingContext context)
        {
            context.DrawEllipse(Brushes.Crimson, null, Position, Radius, Radius);
        }

        public Rect BoundingBox => new(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);
    }

    public class Wall
    {
        public Point Origin { get; }
        public string Direction { get; } // "left", "right", "up", "down"
        public double Length { get; private set; } = 0;
        public double MaxLength { get; }
        public bool Active { get; set; } = true;
        public string Id { get; }

        public Wall(string id, Point origin, string direction, double maxLength)
        {
            Id = id;
            Origin = origin;
            Direction = direction;
            MaxLength = maxLength;
        }

        public void Update(double dt)
        {
            if (Active && Length < MaxLength)
            {
                Length += 150 * dt; // Growth speed
                if (Length >= MaxLength)
                {
                    Length = MaxLength;
                    Active = false;
                }
            }
        }

        public void Draw(DrawingContext context)
        {
            if (Length <= 0) return;

            var start = Origin;
            var end = Direction switch
            {
                "left" => new Point(Origin.X - Length, Origin.Y),
                "right" => new Point(Origin.X + Length, Origin.Y),
                "up" => new Point(Origin.X, Origin.Y - Length),
                "down" => new Point(Origin.X, Origin.Y + Length),
                _ => Origin
            };

            var pen = new Pen(Brushes.Cyan, 4, lineCap: PenLineCap.Round);
            context.DrawLine(pen, start, end);
        }

        public bool Intersects(Ball ball)
        {
            if (Length <= 0) return false;

            var start = Origin;
            var end = Direction switch
            {
                "left" => new Point(Origin.X - Length, Origin.Y),
                "right" => new Point(Origin.X + Length, Origin.Y),
                "up" => new Point(Origin.X, Origin.Y - Length),
                "down" => new Point(Origin.X, Origin.Y + Length),
                _ => Origin
            };

            // Simple line-circle intersection
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) return false;

            var t = Math.Max(0, Math.Min(1,
                ((ball.Position.X - start.X) * dx + (ball.Position.Y - start.Y) * dy) / (length * length)));

            var closest = new Point(start.X + t * dx, start.Y + t * dy);
            var distance = (ball.Position - closest).Length;

            return distance <= ball.Radius + 2; // 2 for wall thickness
        }
    }

    public class GridCell
    {
        public int Id { get; }
        public Point Position { get; }
        public Size Size { get; }
        public bool Scored { get; set; }
        public bool Occupied { get; set; }

        public GridCell(int id, Point position, Size size)
        {
            Id = id;
            Position = position;
            Size = size;
        }

        public void Draw(DrawingContext context)
        {
            var brush = Scored ? Brushes.Gray : (Occupied ? Brushes.LightGray : Brushes.Transparent);
            context.DrawRectangle(brush, null, new Rect(Position, Size));
        }
    }

    public class JezzballGameState
    {
        public int Level { get; private set; } = 1;
        public int Lives { get; private set; } = 3;
        public long Score { get; private set; } = 0;
        public double AreaCleared { get; private set; } = 0;
        public bool IsGameOver => Lives <= 0;
        public bool IsPaused { get; set; } = false;
        public string RoundState { get; set; } = "running"; // "running", "won", "lost"

        private readonly List<Ball> _balls = new();
        private readonly List<Wall> _walls = new();
        private readonly List<GridCell> _grid = new();
        private readonly Random _random = new();
        private readonly Size _gameSize;
        private readonly double _gridSize = 20;

        public IReadOnlyList<Ball> Balls => _balls;
        public IReadOnlyList<Wall> Walls => _walls;
        public IReadOnlyList<GridCell> Grid => _grid;

        public JezzballGameState(Size gameSize)
        {
            _gameSize = gameSize;
            InitializeGrid();
            StartLevel();
        }

        private void InitializeGrid()
        {
            _grid.Clear();
            var gridWidth = (int)(_gameSize.Width / _gridSize);
            var gridHeight = (int)(_gameSize.Height / _gridSize);

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var id = y * gridWidth + x;
                    var position = new Point(x * _gridSize, y * _gridSize);
                    var size = new Size(_gridSize, _gridSize);
                    _grid.Add(new GridCell(id, position, size));
                }
            }
        }

        public void StartLevel()
        {
            _balls.Clear();
            _walls.Clear();
            RoundState = "running";

            // Add balls based on level
            var ballCount = Math.Min(Level + 1, 8);
            for (int i = 0; i < ballCount; i++)
            {
                var x = _random.NextDouble() * (_gameSize.Width - 100) + 50;
                var y = _random.NextDouble() * (_gameSize.Height - 100) + 50;
                var angle = _random.NextDouble() * 2 * Math.PI;
                var speed = 100 + _random.NextDouble() * 50;
                var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

                _balls.Add(new Ball(i, new Point(x, y), velocity));
            }

            // Reset grid
            foreach (var cell in _grid)
            {
                cell.Scored = false;
                cell.Occupied = false;
            }
        }

        public void Update(double dt)
        {
            if (IsPaused || RoundState != "running") return;

            // Update balls
            var bounds = new Rect(0, 0, _gameSize.Width, _gameSize.Height);
            foreach (var ball in _balls)
            {
                ball.Update(dt, bounds);
            }

            // Update walls
            foreach (var wall in _walls)
            {
                wall.Update(dt);
            }

            // Check ball-wall collisions
            foreach (var ball in _balls)
            {
                foreach (var wall in _walls)
                {
                    if (wall.Active && wall.Intersects(ball))
                    {
                        LoseLife("Ball hit growing wall!");
                        return;
                    }
                }
            }

            // Check if all walls are complete
            if (_walls.Count > 0 && _walls.All(w => !w.Active))
            {
                EvaluateGrid();
            }
        }

        private void LoseLife(string reason)
        {
            Lives--;
            JezzballSound.Play(JezzballSoundEvent.WallHit);

            if (Lives <= 0)
            {
                RoundState = "lost";
            }
            else
            {
                // Reset walls and continue
                _walls.Clear();
            }
        }

        private void EvaluateGrid()
        {
            // Simple flood fill to determine captured areas
            var visited = new bool[_grid.Count];
            var capturedCells = 0;

            for (int i = 0; i < _grid.Count; i++)
            {
                if (!visited[i] && !_grid[i].Scored)
                {
                    var area = FloodFill(i, visited);
                    if (area > 0 && !ContainsBall(area))
                    {
                        capturedCells += area;
                        MarkAreaAsScored(area, visited);
                    }
                }
            }

            AreaCleared = (double)capturedCells / _grid.Count * 100;
            Score += capturedCells * 10;

            if (AreaCleared >= 75)
            {
                RoundState = "won";
                JezzballSound.Play(JezzballSoundEvent.LevelComplete);
            }
        }

        private int FloodFill(int startIndex, bool[] visited)
        {
            var stack = new Stack<int>();
            stack.Push(startIndex);
            var area = 0;

            while (stack.Count > 0)
            {
                var index = stack.Pop();
                if (visited[index] || _grid[index].Scored) continue;

                visited[index] = true;
                area++;

                // Add neighbors
                var gridWidth = (int)(_gameSize.Width / _gridSize);
                var x = index % gridWidth;
                var y = index / gridWidth;

                if (x > 0) stack.Push(index - 1);
                if (x < gridWidth - 1) stack.Push(index + 1);
                if (y > 0) stack.Push(index - gridWidth);
                if (y < (int)(_gameSize.Height / _gridSize) - 1) stack.Push(index + gridWidth);
            }

            return area;
        }

        private bool ContainsBall(int area)
        {
            // Check if any ball is in the captured area
            foreach (var ball in _balls)
            {
                var gridX = (int)(ball.Position.X / _gridSize);
                var gridY = (int)(ball.Position.Y / _gridSize);
                var cellIndex = gridY * (int)(_gameSize.Width / _gridSize) + gridX;

                if (cellIndex >= 0 && cellIndex < _grid.Count && !_grid[cellIndex].Scored)
                {
                    return true;
                }
            }
            return false;
        }

        private void MarkAreaAsScored(int area, bool[] visited)
        {
            for (int i = 0; i < _grid.Count; i++)
            {
                if (visited[i])
                {
                    _grid[i].Scored = true;
                }
            }
        }

        public bool TryAddWall(Point position, string direction)
        {
            // Check if position is already occupied
            var gridX = (int)(position.X / _gridSize);
            var gridY = (int)(position.Y / _gridSize);
            var cellIndex = gridY * (int)(_gameSize.Width / _gridSize) + gridX;

            if (cellIndex >= 0 && cellIndex < _grid.Count && _grid[cellIndex].Scored)
            {
                return false;
            }

            // Check if any wall is still active
            if (_walls.Any(w => w.Active))
            {
                return false;
            }

            // Calculate max length based on direction
            var maxLength = direction switch
            {
                "left" => position.X,
                "right" => _gameSize.Width - position.X,
                "up" => position.Y,
                "down" => _gameSize.Height - position.Y,
                _ => 0
            };

            var id = $"{position.X}-{position.Y}-{direction}";
            var wall = new Wall(id, position, direction, maxLength);
            _walls.Add(wall);

            JezzballSound.Play(JezzballSoundEvent.WallBuild);
            return true;
        }

        public void NextLevel()
        {
            Level++;
            StartLevel();
        }

        public void RestartGame()
        {
            Level = 1;
            Lives = 3;
            Score = 0;
            AreaCleared = 0;
            StartLevel();
        }
    }

    internal class JezzballControl : UserControl, IDisposable
    {
        private readonly JezzballGameState _gameState;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private Point _mousePosition;
        private string _wallDirection = "Horizontal";
        private bool _showGrid = true;

        private readonly TextBlock _levelText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _livesText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _scoreText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _areaText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };

        public JezzballControl()
        {
            _gameState = new JezzballGameState(new Size(800, 600));

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _timer.Tick += GameTick;

            var canvas = new Canvas();
            canvas.PointerMoved += OnPointerMoved;
            canvas.PointerPressed += OnPointerPressed;

            var statusBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Colors.DarkGray),
                Children =
                {
                    _levelText,
                    _livesText,
                    _scoreText,
                    _areaText
                }
            };

            var mainPanel = new DockPanel();
            DockPanel.SetDock(statusBar, Dock.Top);
            mainPanel.Children.Add(statusBar);
            mainPanel.Children.Add(canvas);

            Content = mainPanel;

            _timer.Start();
            _stopwatch.Start();
        }

        public void RestartGame()
        {
            _gameState.RestartGame();
        }

        public void TogglePause()
        {
            _gameState.IsPaused = !_gameState.IsPaused;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            _mousePosition = e.GetPosition(sender as Control);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_gameState.RoundState != "running") return;

            var position = e.GetPosition(sender as Control);

            if (e.GetCurrentPoint(sender as Control).Properties.IsRightButtonPressed)
            {
                // Right click changes direction
                _wallDirection = _wallDirection == "Horizontal" ? "Vertical" : "Horizontal";
                JezzballSound.Play(JezzballSoundEvent.Click);
            }
            else
            {
                // Left click places wall
                if (_wallDirection == "Horizontal")
                {
                    _gameState.TryAddWall(position, "left");
                    _gameState.TryAddWall(position, "right");
                }
                else
                {
                    _gameState.TryAddWall(position, "up");
                    _gameState.TryAddWall(position, "down");
                }
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            var dt = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            _gameState.Update(dt);
            UpdateStatusText();
            InvalidateVisual();
        }

        private void UpdateStatusText()
        {
            _levelText.Text = $"Level: {_gameState.Level}";
            _livesText.Text = $"Lives: {_gameState.Lives}";
            _scoreText.Text = $"Score: {_gameState.Score:N0}";
            _areaText.Text = $"Area: {_gameState.AreaCleared:F1}%";
        }

        public override void Render(DrawingContext context)
        {
            var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
            context.FillRectangle(Brushes.Black, bounds);

            // Draw grid
            if (_showGrid)
            {
                var gridSize = 20.0;
                var pen = new Pen(Brushes.DarkGray, 1);

                for (double x = 0; x < bounds.Width; x += gridSize)
                {
                    context.DrawLine(pen, new Point(x, 0), new Point(x, bounds.Height));
                }

                for (double y = 0; y < bounds.Height; y += gridSize)
                {
                    context.DrawLine(pen, new Point(0, y), new Point(bounds.Width, y));
                }
            }

            // Draw grid cells
            foreach (var cell in _gameState.Grid)
            {
                cell.Draw(context);
            }

            // Draw walls
            foreach (var wall in _gameState.Walls)
            {
                wall.Draw(context);
            }

            // Draw balls
            foreach (var ball in _gameState.Balls)
            {
                ball.Draw(context);
            }

            // Draw game over or level complete message
            if (_gameState.RoundState != "running")
            {
                var text = _gameState.RoundState == "won" ? "Level Complete!" : "Game Over!";
                var formattedText = new FormattedText(
                    text,
                    Typeface.Default,
                    48,
                    TextAlignment.Center,
                    TextWrapping.NoWrap,
                    Size.Infinity);

                var textBounds = new Rect(
                    (bounds.Width - formattedText.Bounds.Width) / 2,
                    (bounds.Height - formattedText.Bounds.Height) / 2,
                    formattedText.Bounds.Width,
                    formattedText.Bounds.Height);

                context.DrawRectangle(new SolidColorBrush(Colors.Black, 0.8), null, bounds);
                context.DrawText(Brushes.White, textBounds.Position, formattedText);

                if (_gameState.RoundState == "won")
                {
                    _gameState.NextLevel();
                }
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
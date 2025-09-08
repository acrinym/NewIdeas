using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn
{
    public enum JezzballSoundEvent
    {
        Click, WallBuild, WallHit, WallBreak, BallBounce, LevelComplete
    }

    public class JezzballGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch? _spriteBatch;
        private Texture2D? _pixel;
        private SpriteFont? _uiFont;
        private SpriteFont? _messageFont;

        private readonly JezzballGameState _gameState;
        private string _wallDirection = "Horizontal";
        private MouseState _previousMouseState;

        public JezzballGameState GameState => _gameState;
 
        public JezzballGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            // We are not running the main loop, so suppress it.
            SuppressDraw();

            _gameState = new JezzballGameState();
        }

        protected override void Initialize()
        {
            _gameState.GameSize = new Microsoft.Xna.Framework.Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _gameState.StartLevel();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a 1x1 white texture for drawing primitives
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);

            // Load a system font at runtime using SpriteFontPlus
            LoadFonts();
        }

        private void LoadFonts()
        {
            try
            {
                // Find a suitable font file on the system
                var fontPath = FindFont("Consolas.ttf", "LiberationMono-Regular.ttf", "DejaVuSansMono.ttf", "Arial.ttf");
                if (fontPath == null)
                {
                    // Fallback or error
                    return;
                }

                var fontBytes = File.ReadAllBytes(fontPath);

                // Bake a smaller font for the UI
                var uiFontBake = TtfFontBaker.Bake(fontBytes, 16, 1024, 1024, [CharacterRange.BasicLatin]);
                _uiFont = uiFontBake.CreateSpriteFont(GraphicsDevice);

                // Bake a larger font for messages
                var messageFontBake = TtfFontBaker.Bake(fontBytes, 48, 1024, 1024, [CharacterRange.BasicLatin]);
                _messageFont = messageFontBake.CreateSpriteFont(GraphicsDevice);
            }
            catch (Exception ex)
            {
                // Log the error if font loading fails
                System.Diagnostics.Debug.WriteLine($"Font loading failed: {ex.Message}");
            }
        }

        private static string? FindFont(params string[] fontNames)
        {
            var fontPaths = new List<string>();
            if (OperatingSystem.IsWindows())
            {
                fontPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts"));
            }
            else if (OperatingSystem.IsLinux())
            {
                fontPaths.Add("/usr/share/fonts/truetype/dejavu");
                fontPaths.Add("/usr/share/fonts/truetype/liberation");
                fontPaths.Add("/usr/share/fonts/truetype");
                fontPaths.Add("/usr/share/fonts");
            }
            else if (OperatingSystem.IsMacOS())
            {
                fontPaths.Add("/System/Library/Fonts");
                fontPaths.Add("/Library/Fonts");
            }

            foreach (var fontName in fontNames)
            {
                foreach (var fontPath in fontPaths)
                {
                    var path = Path.Combine(fontPath, fontName);
                    if (File.Exists(path)) return path;
                }
            }
            return null;
        }

        protected override void Update(GameTime gameTime)
        {
            var currentMouseState = Mouse.GetState();

            // Handle round state transitions (e.g., clicking to continue)
            if (_gameState.RoundState is "won" or "lost")
            {
                if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    if (_gameState.RoundState == "won") _gameState.NextLevel();
                    else _gameState.RestartGame();
                }
            }
            else
            {
                // Normal game input
                if (currentMouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
                {
                    _wallDirection = _wallDirection == "Horizontal" ? "Vertical" : "Horizontal";
                    JezzballSound.Play(JezzballSoundEvent.Click);
                }
                else if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    var position = currentMouseState.Position;
                    _gameState.TryAddWall(position, _wallDirection);
                }
            }

            _gameState.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            _previousMouseState = currentMouseState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch?.Begin();

            // Draw grid, walls, balls, etc.
            if (_spriteBatch != null && _pixel != null)
            {
                _gameState.Draw(_spriteBatch, _pixel);
            }

            // Draw UI text if the font was loaded
            if (_uiFont != null && _spriteBatch != null)
            {
                var statusText = $"Level: {_gameState.Level} | Lives: {_gameState.Lives} | Score: {_gameState.Score:N0} | Area: {_gameState.AreaCleared:F1}%";
                _spriteBatch.DrawString(_uiFont, statusText, new Vector2(10, 10), Color.White);
            }

            // Draw game over or level complete message
            if (_messageFont != null && _gameState.RoundState is "won" or "lost" && _spriteBatch != null && _pixel != null && _uiFont != null)
            {
                var message = _gameState.RoundState == "won" ? "Level Complete!" : "Game Over!";
                var subMessage = "Click to continue";

                var messageSize = _messageFont.MeasureString(message);
                var subMessageSize = _uiFont.MeasureString(subMessage);

                var messagePosition = new Vector2(
                    (GraphicsDevice.Viewport.Width - messageSize.X) / 2,
                    (GraphicsDevice.Viewport.Height - messageSize.Y) / 2 - 20);

                var subMessagePosition = new Vector2(
                    (GraphicsDevice.Viewport.Width - subMessageSize.X) / 2,
                    messagePosition.Y + messageSize.Y + 10);

                _spriteBatch.Draw(_pixel, GraphicsDevice.Viewport.Bounds, Color.Black * 0.7f);
                _spriteBatch.DrawString(_messageFont, message, messagePosition, Color.White);
                _spriteBatch.DrawString(_uiFont, subMessage, subMessagePosition, Color.LightGray);
            }

            _spriteBatch?.End();

            base.Draw(gameTime);
        }

        public void RestartGame() => _gameState.RestartGame();
        public void TogglePause() => _gameState.IsPaused = !_gameState.IsPaused;
    }

    #region Game Logic Classes

    public class Ball(Vector2 position, Vector2 velocity)
    {
        public Vector2 Position { get; private set; } = position;
        public Vector2 Velocity { get; set; } = velocity;
        public float Radius { get; } = 8;

        public void Update(float dt, Rectangle bounds)
        {
            var originalPosition = Position;
            Position += Velocity * dt;

            if (Position.X - Radius <= bounds.Left || Position.X + Radius >= bounds.Right)
            {
                Velocity = new Vector2(-Velocity.X, Velocity.Y);
                Position = new Vector2(MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius), Position.Y);
            }
            else if (Position.Y - Radius <= bounds.Top || Position.Y + Radius >= bounds.Bottom)
            {
                Velocity = new Vector2(Velocity.X, -Velocity.Y);
                Position = new Vector2(Position.X, MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius));
            }

            // If position changed, play bounce sound
            if (Position != originalPosition)
            {
                JezzballSound.Play(JezzballSoundEvent.BallBounce);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Draw a proper circle by drawing multiple rectangles
            DrawCircle(spriteBatch, pixel, Position, Radius, Color.Crimson);
        }

        private static void DrawCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, Color color)
        {
            var radiusSquared = radius * radius;
            var diameter = (int)(radius * 2);
            
            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    var dx = x - radius;
                    var dy = y - radius;
                    var distanceSquared = dx * dx + dy * dy;
                    
                    if (distanceSquared <= radiusSquared)
                    {
                        var rect = new Rectangle((int)(center.X + dx), (int)(center.Y + dy), 1, 1);
                        spriteBatch.Draw(pixel, rect, color);
                    }
                }
            }
        }
    }

    public class Wall(Vector2 origin, string direction, float maxLength)
    {
        public Vector2 Origin { get; } = origin;
        public string Direction { get; } = direction;
        public float Length { get; private set; }
        public float MaxLength { get; } = maxLength;
        public bool Active { get; set; } = true;
        public bool IsComplete { get; private set; } = false;
        public bool ShouldEvaluateArea { get; set; } = false;

        public void Update(float dt, List<Wall> otherWalls, List<Ball> balls, Rectangle gameBounds)
        {
            if (Active && !IsComplete && Length < MaxLength)
            {
                var growthSpeed = 300f * dt; // Match Python RAY_SPEED
                var newLength = Length + growthSpeed;
                
                // Check for collisions during growth
                if (CheckCollisionDuringGrowth(newLength, otherWalls, balls, gameBounds))
                {
                    // Hit something, stop growing and convert to blocks
                    Length = newLength - growthSpeed; // Stop at previous length
                    IsComplete = true;
                    Active = false;
                    // Mark this wall for area evaluation
                    ShouldEvaluateArea = true;
                }
                else
                {
                    Length = Math.Min(newLength, MaxLength);
                    if (Length >= MaxLength)
                    {
                        // Reached border
                        IsComplete = true;
                        Active = false;
                    }
                }
            }
        }

        private bool CheckCollisionDuringGrowth(float newLength, List<Wall> otherWalls, List<Ball> balls, Rectangle gameBounds)
        {
            // Get the current wall segment endpoints
            var (start, end) = GetWallEndpoints(newLength);
            
            // Check collision with balls
            foreach (var ball in balls)
            {
                if (LineIntersectsCircle(start, end, ball.Position, ball.Radius + 2))
                {
                    return true; // Hit a ball
                }
            }
            
            // Check collision with other completed walls only
            foreach (var otherWall in otherWalls)
            {
                if (otherWall == this || otherWall.Active) continue; // Skip self and active walls
                
                var (otherStart, otherEnd) = otherWall.GetWallEndpoints(otherWall.Length);
                if (LineIntersectsLine(start, end, otherStart, otherEnd))
                {
                    return true; // Hit another wall
                }
            }
            
            return false;
        }

        public (Vector2 start, Vector2 end) GetWallEndpoints(float length)
        {
            return Direction switch
            {
                "left" => (new Vector2(Origin.X - length, Origin.Y), Origin),
                "right" => (Origin, new Vector2(Origin.X + length, Origin.Y)),
                "up" => (new Vector2(Origin.X, Origin.Y - length), Origin),
                "down" => (Origin, new Vector2(Origin.X, Origin.Y + length)),
                _ => (Origin, Origin)
            };
        }

        private static bool LineIntersectsCircle(Vector2 lineStart, Vector2 lineEnd, Vector2 circleCenter, float radius)
        {
            var lineVector = lineEnd - lineStart;
            var startToCenter = circleCenter - lineStart;
            
            var lineLengthSq = lineVector.LengthSquared();
            if (lineLengthSq == 0) return Vector2.DistanceSquared(lineStart, circleCenter) <= radius * radius;
            
            var t = Vector2.Dot(startToCenter, lineVector) / lineLengthSq;
            t = MathHelper.Clamp(t, 0, 1);
            
            var closestPoint = lineStart + t * lineVector;
            return Vector2.DistanceSquared(circleCenter, closestPoint) <= radius * radius;
        }

        private static bool LineIntersectsLine(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            // Simple line intersection check
            var d1 = (line1End.X - line1Start.X) * (line2Start.Y - line1Start.Y) - (line1End.Y - line1Start.Y) * (line2Start.X - line1Start.X);
            var d2 = (line1End.X - line1Start.X) * (line2End.Y - line1Start.Y) - (line1End.Y - line1Start.Y) * (line2End.X - line1Start.X);
            var d3 = (line2End.X - line2Start.X) * (line1Start.Y - line2Start.Y) - (line2End.Y - line2Start.Y) * (line1Start.X - line2Start.X);
            var d4 = (line2End.X - line2Start.X) * (line1End.Y - line2Start.Y) - (line2End.Y - line2Start.Y) * (line1End.X - line2Start.X);
            
            return (d1 * d2 < 0) && (d3 * d4 < 0);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (Length <= 0) return;

            Rectangle rect;

            switch (Direction)
            {
                case "left":
                    rect = new Rectangle((int)(Origin.X - Length), (int)Origin.Y - 2, (int)Length, 4);
                    break;
                case "right":
                    rect = new Rectangle((int)Origin.X, (int)Origin.Y - 2, (int)Length, 4);
                    break;
                case "up":
                    rect = new Rectangle((int)Origin.X - 2, (int)(Origin.Y - Length), 4, (int)Length);
                    break;
                case "down":
                    rect = new Rectangle((int)Origin.X - 2, (int)Origin.Y, 4, (int)Length);
                    break;
                default: return;
            }
            // Color walls based on direction: red for horizontal, blue for vertical
            var wallColor = (Direction == "left" || Direction == "right") ? Color.Red : Color.Blue;
            spriteBatch.Draw(pixel, rect, wallColor);
        }

        public bool Intersects(Ball ball)
        {
            // Use more accurate line-segment to circle collision detection.
            Vector2 start = Origin;
            Vector2 end = Direction switch
            {
                "left" => new Vector2(Origin.X - Length, Origin.Y),
                "right" => new Vector2(Origin.X + Length, Origin.Y),
                "up" => new Vector2(Origin.X, Origin.Y - Length),
                "down" => new Vector2(Origin.X, Origin.Y + Length),
                _ => Origin,
            };

            Vector2 wallVector = end - start;
            Vector2 startToBall = ball.Position - start;

            float wallLengthSq = wallVector.LengthSquared();
            if (wallLengthSq == 0.0f)
            {
                return Vector2.DistanceSquared(start, ball.Position) <= ball.Radius * ball.Radius;
            }

            // Project startToBall onto wallVector to find the closest point on the infinite line.
            float t = Vector2.Dot(startToBall, wallVector) / wallLengthSq;

            // Clamp t to be on the segment.
            t = MathHelper.Clamp(t, 0, 1);

            // Find the closest point on the segment to the ball.
            Vector2 closestPoint = start + t * wallVector;

            // Check if the distance to the closest point is less than the ball's radius (plus wall thickness).
            return Vector2.DistanceSquared(ball.Position, closestPoint) <= (ball.Radius + 2) * (ball.Radius + 2);
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
        public string RoundState { get; set; } = "running";
        public Microsoft.Xna.Framework.Point GameSize { get; set; }

        private const int GridCellSize = 10;
        private readonly List<GridCell> _grid = new();
        private readonly List<Ball> _balls = new();
        private readonly List<Wall> _walls = new();
        private readonly Random _random = new();

        public JezzballGameState()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            _grid.Clear();
            if (GameSize.X == 0 || GameSize.Y == 0) return;

            for (int y = 0; y < GameSize.Y / GridCellSize; y++)
            {
                for (int x = 0; x < GameSize.X / GridCellSize; x++)
                {
                    _grid.Add(new GridCell(new Rectangle(x * GridCellSize, y * GridCellSize, GridCellSize, GridCellSize)));
                }
            }
        }

        public void StartLevel()
        {
            _balls.Clear();
            _walls.Clear();
            RoundState = "running";

            var ballCount = Math.Min(Level + 1, 8);
            for (int i = 0; i < ballCount; i++)
            {
                var x = _random.Next(50, GameSize.X - 50);
                var y = _random.Next(50, GameSize.Y - 50);
                var angle = (float)(_random.NextDouble() * 2 * Math.PI);
                var speed = 100 + (float)_random.NextDouble() * 50;
                var velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                _balls.Add(new Ball(new Vector2(x, y), velocity));
            }

            // Reset grid fill state
            foreach (var cell in _grid)
            {
                cell.IsFilled = false;
            }
        }

        public void Update(float dt)
        {
            if (IsPaused || RoundState != "running") return;

            var bounds = new Rectangle(0, 0, GameSize.X, GameSize.Y);
            foreach (var ball in _balls)
            {
                ball.Update(dt, bounds);
            }

            foreach (var wall in _walls.ToList())
            {
                wall.Update(dt, _walls, _balls, new Rectangle(0, 0, GameSize.X, GameSize.Y));
            }

            // FIXED: Add safety checks to prevent crashes during collision detection
            foreach (var ball in _balls.ToList()) // Create a copy to avoid modification during iteration
            {
                foreach (var wall in _walls.ToList()) // Create a copy to avoid modification during iteration
                {
                    try
                    {
                        if (wall.Active && wall.Intersects(ball))
                        {
                            LoseLife();
                            return;
                        }
                        else if (!wall.Active && wall.Intersects(ball))
                        {
                            // Ball hit a completed wall - bounce it
                            BounceBallOffWall(ball, wall);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log collision detection errors but don't crash the game
                        System.Diagnostics.Debug.WriteLine($"Collision detection error: {ex.Message}");
                    }
                }
            }

            // Check if any wall has completed and needs area evaluation
            var completedWalls = _walls.Where(w => w.ShouldEvaluateArea).ToList();
            if (completedWalls.Count > 0)
            {
                // Convert completed walls to blocks and evaluate areas
                foreach (var wall in completedWalls)
                {
                    ConvertWallToBlocks(wall);
                    wall.ShouldEvaluateArea = false;
                }
                EvaluateGrid();
            }
        }

        private void ConvertWallToBlocks(Wall wall)
        {
            // Convert wall to filled grid cells
            var (start, end) = wall.GetWallEndpoints(wall.Length);
            
            if (wall.Direction == "left" || wall.Direction == "right")
            {
                // Horizontal wall - fill cells along the line
                var startX = (int)Math.Min(start.X, end.X);
                var endX = (int)Math.Max(start.X, end.X);
                var y = (int)start.Y;
                
                for (int x = startX; x <= endX; x += GridCellSize)
                {
                    var cell = GetGridCellAt(x, y);
                    if (cell != null)
                    {
                        cell.IsFilled = true;
                    }
                }
            }
            else
            {
                // Vertical wall - fill cells along the line
                var x = (int)start.X;
                var startY = (int)Math.Min(start.Y, end.Y);
                var endY = (int)Math.Max(start.Y, end.Y);
                
                for (int y = startY; y <= endY; y += GridCellSize)
                {
                    var cell = GetGridCellAt(x, y);
                    if (cell != null)
                    {
                        cell.IsFilled = true;
                    }
                }
            }
        }

        private GridCell? GetGridCellAt(int x, int y)
        {
            var gridX = x / GridCellSize;
            var gridY = y / GridCellSize;
            var gridWidth = GameSize.X / GridCellSize;
            
            if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < GameSize.Y / GridCellSize)
            {
                return _grid[gridY * gridWidth + gridX];
            }
            return null;
        }

        private void LoseLife()
        {
            try
            {
                Lives--;
                JezzballSound.Play(JezzballSoundEvent.WallHit);
                _walls.Clear();

                if (Lives <= 0)
                {
                    RoundState = "lost";
                }
            }
            catch (Exception ex)
            {
                // Log any errors in LoseLife but don't crash
                System.Diagnostics.Debug.WriteLine($"Error in LoseLife: {ex.Message}");
                RoundState = "lost"; // Fallback to game over
            }
        }

        private void EvaluateGrid()
        {
            // 1. Find all areas that don't contain a ball using flood fill.
            var fillableAreas = FindFillableAreas();

            // 2. If any areas were found, fill them.
            if (fillableAreas.Count > 0)
            {
                int filledCellCount = 0;
                foreach (var area in fillableAreas)
                {
                    foreach (var cell in area)
                    {
                        if (!cell.IsFilled)
                        {
                            cell.IsFilled = true;
                            filledCellCount++;
                        }
                    }
                }

                // 3. Update score and area cleared percentage.
                Score += (long)filledCellCount * 10;
                var totalCells = _grid.Count;
                var totalFilled = _grid.Count(c => c.IsFilled);
                if (totalCells > 0)
                {
                    AreaCleared = (double)totalFilled / totalCells * 100;
                }
                JezzballSound.Play(JezzballSoundEvent.WallBreak);
            }

            // 4. Clear the temporary walls.
            _walls.Clear();

            // 5. Check for level complete.
            if (AreaCleared >= 75)
            {
                RoundState = "won";
                JezzballSound.Play(JezzballSoundEvent.LevelComplete);
            }
        }

        private List<List<GridCell>> FindFillableAreas()
        {
            var fillableAreas = new List<List<GridCell>>();
            var visited = new HashSet<GridCell>();
            int gridWidth = GameSize.X / GridCellSize;

            foreach (var startCell in _grid)
            {
                if (visited.Contains(startCell) || startCell.IsFilled) continue;

                var area = new List<GridCell>();
                var queue = new Queue<GridCell>();
                bool areaContainsBall = false;

                queue.Enqueue(startCell);
                visited.Add(startCell);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    area.Add(current);

                    if (_balls.Any(b => current.Bounds.Contains(b.Position)))
                    {
                        areaContainsBall = true;
                    }

                    // Add neighbors to the queue
                    foreach (var neighbor in GetNeighbors(current, gridWidth, visited))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }

                if (!areaContainsBall)
                {
                    fillableAreas.Add(area);
                }
            }
            return fillableAreas;
        }

        private IEnumerable<GridCell> GetNeighbors(GridCell cell, int gridWidth, HashSet<GridCell> visited)
        {
            int index = _grid.IndexOf(cell);
            int x = index % gridWidth;
            int y = index / gridWidth;

            // Check left, right, up, down
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < GameSize.Y / GridCellSize)
                {
                    var neighbor = _grid[ny * gridWidth + nx];
                    if (!visited.Contains(neighbor) && !neighbor.IsFilled && !IsWallBetween(cell, neighbor))
                    {
                        yield return neighbor;
                    }
                }
            }
        }

        private bool IsWallBetween(GridCell cell1, GridCell cell2)
        {
            // Check if there's a wall between two adjacent cells
            var center1 = new Vector2(cell1.Bounds.Center.X, cell1.Bounds.Center.Y);
            var center2 = new Vector2(cell2.Bounds.Center.X, cell2.Bounds.Center.Y);
            
            foreach (var wall in _walls)
            {
                if (!wall.Active) // Only check completed walls
                {
                    var (wallStart, wallEnd) = wall.GetWallEndpoints(wall.Length);
                    if (LineIntersectsLine(center1, center2, wallStart, wallEnd))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool LineIntersectsLine(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            // Simple line intersection check
            var d1 = (line1End.X - line1Start.X) * (line2Start.Y - line1Start.Y) - (line1End.Y - line1Start.Y) * (line2Start.X - line1Start.X);
            var d2 = (line1End.X - line1Start.X) * (line2End.Y - line1Start.Y) - (line1End.Y - line1Start.Y) * (line2End.X - line1Start.X);
            var d3 = (line2End.X - line2Start.X) * (line1Start.Y - line2Start.Y) - (line2End.Y - line2Start.Y) * (line1Start.X - line2Start.X);
            var d4 = (line2End.X - line2Start.X) * (line1End.Y - line2Start.Y) - (line2End.Y - line2Start.Y) * (line1End.X - line2Start.X);
            
            return (d1 * d2 < 0) && (d3 * d4 < 0);
        }

        public void NextLevel()
        {
            Level++;
            AreaCleared = 0; // Reset area for the new level
            StartLevel();
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Draw grid
            for (int x = 0; x < GameSize.X; x += GridCellSize)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, 0, 1, GameSize.Y), Color.DimGray);
            }
            for (int y = 0; y < GameSize.Y; y += GridCellSize)
            {
                spriteBatch.Draw(pixel, new Rectangle(0, y, GameSize.X, 1), Color.DimGray);
            }

            // Draw filled grid cells
            foreach (var cell in _grid)
            {
                cell.Draw(spriteBatch, pixel);
            }


            foreach (var wall in _walls)
            {
                wall.Draw(spriteBatch, pixel);
            }

            foreach (var ball in _balls)
            {
                ball.Draw(spriteBatch, pixel);
            }

            // Draw UI text would go here
        }

        public void TryAddWall(Microsoft.Xna.Framework.Point position, string direction)
        {
            if (_walls.Any(w => w.Active)) return;

            // Create walls that grow in both directions from the click point
            if (direction == "Horizontal")
            {
                // Create left and right walls from the click point
                var leftMaxLength = position.X;
                var rightMaxLength = GameSize.X - position.X;
                
                if (leftMaxLength > 0)
                    _walls.Add(new Wall(position.ToVector2(), "left", leftMaxLength));
                if (rightMaxLength > 0)
                    _walls.Add(new Wall(position.ToVector2(), "right", rightMaxLength));
            }
            else // Vertical
            {
                // Create up and down walls from the click point
                var upMaxLength = position.Y;
                var downMaxLength = GameSize.Y - position.Y;
                
                if (upMaxLength > 0)
                    _walls.Add(new Wall(position.ToVector2(), "up", upMaxLength));
                if (downMaxLength > 0)
                    _walls.Add(new Wall(position.ToVector2(), "down", downMaxLength));
            }
            
            JezzballSound.Play(JezzballSoundEvent.WallBuild);
        }

        public void RestartGame()
        {
            Level = 1;
            Lives = 3;
            Score = 0;
            AreaCleared = 0;
            RoundState = "running";
            StartLevel();
        }

        private void BounceBallOffWall(Ball ball, Wall wall)
        {
            // Get wall endpoints
            var (wallStart, wallEnd) = wall.GetWallEndpoints(wall.Length);
            
            // Calculate wall normal (perpendicular to wall direction)
            Vector2 wallVector = wallEnd - wallStart;
            Vector2 wallNormal;
            
            if (wall.Direction == "left" || wall.Direction == "right")
            {
                // Horizontal wall - bounce vertically
                wallNormal = new Vector2(0, wall.Direction == "left" ? -1 : 1);
            }
            else
            {
                // Vertical wall - bounce horizontally  
                wallNormal = new Vector2(wall.Direction == "up" ? -1 : 1, 0);
            }
            
            // Reflect ball velocity off the wall
            var dotProduct = Vector2.Dot(ball.Velocity, wallNormal);
            ball.Velocity = ball.Velocity - 2 * dotProduct * wallNormal;
            
            // Play bounce sound
            JezzballSound.Play(JezzballSoundEvent.BallBounce);
        }
    }

    public class GridCell(Rectangle bounds)
    {
        public Rectangle Bounds { get; } = bounds;
        public bool IsFilled { get; set; }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (IsFilled)
            {
                spriteBatch.Draw(pixel, Bounds, Color.DarkSlateGray);
            }
        }
    }
    #endregion
}

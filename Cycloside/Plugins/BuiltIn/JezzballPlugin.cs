using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public Version Version => new(1, 7, 0); // Version bump for FlowerBox theme
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            var themeName = SettingsManager.Settings.PluginGameThemes.TryGetValue("Jezzball", out var t) ? t : "Classic";
            var theme = JezzballThemes.All.TryGetValue(themeName, out var th) ? th : JezzballThemes.All["Classic"];
            _control = new JezzballControl(theme);

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

            ThemeManager.ApplyForPlugin(_window, this);
            if (SettingsManager.Settings.PluginSkins.TryGetValue("Jezzball", out var skin))
            {
                SkinManager.ApplySkinTo(_window, skin);
            }
            BuildMenu(themeName, skin);
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

        private void BuildMenu(string currentTheme, string? currentSkin)
        {
            if (_control == null) return;
            var menu = _control.MenuBar;
            menu.Items.Clear();

            var themeMenu = new MenuItem { Header = "Theme" };
            foreach (var name in JezzballThemes.All.Keys)
            {
                var item = new MenuItem
                {
                    Header = name,
                    ToggleType = MenuItemToggleType.CheckBox,
                    IsChecked = name == currentTheme
                };
                item.Click += (_, _) =>
                {
                    _control.SetTheme(name);
                    SettingsManager.Settings.PluginGameThemes["Jezzball"] = name;
                    SettingsManager.Save();
                    foreach (var mi in themeMenu.Items!.OfType<MenuItem>()) mi.IsChecked = mi == item;
                };
                ((IList)themeMenu.Items).Add(item);
            }

            var skinMenu = new MenuItem { Header = "Skin" };
            foreach (var skin in GetSkinNames())
            {
                var item = new MenuItem
                {
                    Header = skin,
                    ToggleType = MenuItemToggleType.CheckBox,
                    IsChecked = skin == currentSkin
                };
                item.Click += (_, _) =>
                {
                    if (_window != null)
                        SkinManager.ApplySkinTo(_window, skin);
                    SettingsManager.Settings.PluginSkins["Jezzball"] = skin;
                    SettingsManager.Save();
                    foreach (var mi in skinMenu.Items!.OfType<MenuItem>()) mi.IsChecked = mi == item;
                };
                ((IList)skinMenu.Items).Add(item);
            }

            var soundMenu = new MenuItem { Header = "Sounds" };
            foreach (JezzballSoundEvent ev in Enum.GetValues(typeof(JezzballSoundEvent)))
            {
                var item = new MenuItem { Header = $"Set {ev}..." };
                item.Click += async (_, _) =>
                {
                    if (_window == null) return;
                    var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = $"Select sound for {ev}",
                        AllowMultiple = false,
                        FileTypeFilter = new[] { new FilePickerFileType("Audio") { Patterns = new[] { "*.wav", "*.ogg" } } }
                    });
                    var file = result.FirstOrDefault();
                    if (file?.Path.LocalPath != null) _control?.SetSound(ev, file.Path.LocalPath);
                };
                ((IList)soundMenu.Items).Add(item);
            }

            menu.Items.Clear();
            ((IList)menu.Items).Add(themeMenu);
            ((IList)menu.Items).Add(skinMenu);
            ((IList)menu.Items).Add(soundMenu);
        }

        private static IEnumerable<string> GetSkinNames()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Skins");
            return Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*.axaml")
                    .Select(f => Path.GetFileNameWithoutExtension(f) ?? string.Empty)
                : Array.Empty<string>();
        }
    }
    #endregion

    #region Game Model (State and Logic)

    public enum WallOrientation { Vertical, Horizontal }
    public enum BallType { Normal, Slow, Fast, Splitting, Teleporting }
    public enum PowerUpType { IceWall, ExtraLife, Freeze, DoubleScore }

    public enum JezzballSoundEvent
    {
        Click,
        WallBuild,
        WallHit,
        WallBreak,
        BallBounce,
        LevelComplete
    }

    public class JezzballTheme
    {
        public IBrush BackgroundBrush { get; init; } = Brushes.Black;
        public Pen WallPen { get; init; } = new Pen(Brushes.Cyan, 4, lineCap: PenLineCap.Round);
        public Pen PreviewPen { get; init; } = new Pen(new SolidColorBrush(Colors.Yellow, 0.7), 2, DashStyle.Dash);
        public IBrush FilledBrush { get; init; } = Brushes.Gray;
        public IBrush FlashBrush { get; init; } = new SolidColorBrush(Colors.White, 0.3);
        public Pen IceWallPen { get; init; } = new Pen(Brushes.LightCyan, 5, lineCap: PenLineCap.Round);
        public Pen IcePreviewPen { get; init; } = new Pen(new SolidColorBrush(Colors.LightCyan, 0.8), 2, DashStyle.Dash);
        public IBrush PowerUpBrush { get; init; } = Brushes.Aqua;
        public IBrush ExtraLifeBrush { get; init; } = Brushes.LimeGreen;
        public IBrush FreezeBrush { get; init; } = Brushes.LightBlue;
        public IBrush DoubleScoreBrush { get; init; } = Brushes.Gold;
        public IBrush BallNormalBrush { get; init; } = Brushes.Crimson;
        public IBrush BallSlowBrush { get; init; } = Brushes.DeepSkyBlue;
        public IBrush BallFastBrush { get; init; } = Brushes.OrangeRed;
        public IBrush BallSplittingBrush { get; init; } = Brushes.MediumPurple;
        public IBrush BallTeleportBrush { get; init; } = Brushes.Gold;
    }

    // NEW: A static class to generate the procedural brushes for the FlowerBox theme.
    internal static class FlowerBoxResources
    {
        // Creates a brush that looks like the 3D cube from the screensaver.
        public static IBrush CreateCubeBrush()
        {
            var drawing = new DrawingGroup
            {
                Children =
                {
                    // Back face (darker)
                    new GeometryDrawing { Geometry = CreatePath(new Point(0.3, 0), new Point(0.8, 0.2), new Point(0.7, 0.7), new Point(0.2, 0.5)), Brush = new SolidColorBrush(Color.FromRgb(0, 180, 180)) },
                    // Left face (medium)
                    new GeometryDrawing { Geometry = CreatePath(new Point(0, 0.2), new Point(0.2, 0.5), new Point(0.2, 1), new Point(0, 0.7)), Brush = new SolidColorBrush(Color.FromRgb(0, 220, 220)) },
                    // Top face (brightest)
                    new GeometryDrawing { Geometry = CreatePath(new Point(0.3, 0), new Point(0.2, 0.5), new Point(0, 0.7), new Point(0.1, 0.2)), Brush = new SolidColorBrush(Color.FromRgb(0, 255, 255)) }
                }
            };
            return new DrawingBrush { Drawing = drawing, Stretch = Stretch.Uniform };
        }

        // Creates a brush that looks like the 3D tetrahedron.
        public static IBrush CreateTetraBrush()
        {
            var drawing = new DrawingGroup
            {
                Children =
                {
                    new GeometryDrawing { Geometry = CreatePath(new Point(0.5, 0), new Point(1, 0.6), new Point(0.5, 1)), Brush = new SolidColorBrush(Color.FromRgb(255, 0, 0)) },
                    new GeometryDrawing { Geometry = CreatePath(new Point(0.5, 0), new Point(0, 0.6), new Point(0.5, 1)), Brush = new SolidColorBrush(Color.FromRgb(200, 0, 0)) }
                }
            };
            return new DrawingBrush { Drawing = drawing, Stretch = Stretch.Uniform };
        }
        
        // Creates a brush that looks like the double pyramid.
        public static IBrush CreatePyramidBrush()
        {
             var drawing = new DrawingGroup
            {
                Children =
                {
                    new GeometryDrawing { Geometry = CreatePath(new Point(0.5, 0), new Point(1, 0.5), new Point(0.5, 1)), Brush = new SolidColorBrush(Color.FromRgb(0, 0, 255)) },
                    new GeometryDrawing { Geometry = CreatePath(new Point(0.5, 0), new Point(0, 0.5), new Point(0.5, 1)), Brush = new SolidColorBrush(Color.FromRgb(0, 0, 180)) },
                }
            };
            return new DrawingBrush { Drawing = drawing, Stretch = Stretch.Uniform };
        }

        private static PathGeometry CreatePath(params Point[] points)
        {
            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = true,
                Segments = new PathSegments()
            };

            figure.Segments!.Add(new PolyLineSegment(points.Skip(1)) { IsStroked = true });

            return new PathGeometry
            {
                Figures = new PathFigures { figure }
            };
        }
    }

    internal static class JezzballThemes
    {
        public static readonly Dictionary<string, JezzballTheme> All = new()
        {
            // NEW: Added the FlowerBox theme, which uses the procedural brushes.
            ["FlowerBox"] = new JezzballTheme
            {
                BackgroundBrush = Brushes.Black,
                WallPen = new Pen(Brushes.White, 2),
                FilledBrush = new SolidColorBrush(Colors.DarkGray, 0.2),
                BallNormalBrush = FlowerBoxResources.CreateCubeBrush(),
                BallSlowBrush = FlowerBoxResources.CreateTetraBrush(),
                BallFastBrush = FlowerBoxResources.CreatePyramidBrush(),
                BallSplittingBrush = FlowerBoxResources.CreateCubeBrush(), // Can be another shape
                BallTeleportBrush = Brushes.Gold,
                ExtraLifeBrush = Brushes.LimeGreen,
                FreezeBrush = Brushes.LightBlue,
                DoubleScoreBrush = Brushes.Gold,
            },
            ["Classic"] = new JezzballTheme
            {
                BackgroundBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(0.8, RelativeUnit.Relative),
                    GradientStops = new GradientStops { new(Colors.DarkSlateBlue, 0), new(Colors.Black, 1) }
                },
                WallPen = new Pen(Brushes.Cyan, 4, lineCap: PenLineCap.Round),
                PreviewPen = new Pen(new SolidColorBrush(Colors.Yellow, 0.7), 2, DashStyle.Dash),
                FilledBrush = new SolidColorBrush(Color.FromRgb(0, 50, 70), 0.6),
                FlashBrush = new SolidColorBrush(Colors.White, 0.3),
                IceWallPen = new Pen(Brushes.LightCyan, 5, lineCap: PenLineCap.Round),
                IcePreviewPen = new Pen(new SolidColorBrush(Colors.LightCyan, 0.8), 2, DashStyle.Dash),
                PowerUpBrush = Brushes.Aqua,
                BallNormalBrush = Brushes.Crimson,
                BallSlowBrush = Brushes.DeepSkyBlue,
                BallFastBrush = Brushes.OrangeRed,
                BallSplittingBrush = Brushes.MediumPurple,
                BallTeleportBrush = Brushes.Gold,
                ExtraLifeBrush = Brushes.LimeGreen,
                FreezeBrush = Brushes.LightBlue,
                DoubleScoreBrush = Brushes.Gold
            },
            ["Neon"] = new JezzballTheme
            {
                BackgroundBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(0.8, RelativeUnit.Relative),
                    GradientStops = new GradientStops { new(Color.FromRgb(10, 10, 30), 0), new(Colors.Black, 1) }
                },
                WallPen = new Pen(Brushes.Magenta, 4, lineCap: PenLineCap.Round),
                PreviewPen = new Pen(new SolidColorBrush(Colors.Lime, 0.7), 2, DashStyle.Dash),
                FilledBrush = new SolidColorBrush(Color.FromArgb(160, 0, 255, 100)),
                FlashBrush = new SolidColorBrush(Colors.White, 0.3),
                IceWallPen = new Pen(Brushes.Cyan, 5, lineCap: PenLineCap.Round),
                IcePreviewPen = new Pen(new SolidColorBrush(Colors.Cyan, 0.8), 2, DashStyle.Dash),
                PowerUpBrush = Brushes.Lime,
                BallNormalBrush = Brushes.HotPink,
                BallSlowBrush = Brushes.Lime,
                BallFastBrush = Brushes.Yellow,
                BallSplittingBrush = Brushes.Cyan,
                BallTeleportBrush = Brushes.Gold,
                ExtraLifeBrush = Brushes.LimeGreen,
                FreezeBrush = Brushes.LightBlue,
                DoubleScoreBrush = Brushes.Gold
            },
            ["Pastel"] = new JezzballTheme
            {
                BackgroundBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(0.8, RelativeUnit.Relative),
                    GradientStops = new GradientStops { new(Color.FromRgb(250, 240, 255), 0), new(Color.FromRgb(230, 215, 240), 1) }
                },
                WallPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 182, 193)), 4, lineCap: PenLineCap.Round),
                PreviewPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 218, 185), 0.7), 2, DashStyle.Dash),
                FilledBrush = new SolidColorBrush(Color.FromArgb(150, 220, 200, 240)),
                FlashBrush = new SolidColorBrush(Colors.White, 0.3),
                IceWallPen = new Pen(new SolidColorBrush(Color.FromRgb(175, 238, 238)), 5, lineCap: PenLineCap.Round),
                IcePreviewPen = new Pen(new SolidColorBrush(Color.FromRgb(224, 255, 255), 0.8), 2, DashStyle.Dash),
                PowerUpBrush = new SolidColorBrush(Color.FromRgb(255, 192, 203)),
                BallNormalBrush = new SolidColorBrush(Color.FromRgb(255, 105, 97)),
                BallSlowBrush = new SolidColorBrush(Color.FromRgb(135, 206, 235)),
                BallFastBrush = new SolidColorBrush(Color.FromRgb(255, 160, 122)),
                BallSplittingBrush = new SolidColorBrush(Color.FromRgb(216, 191, 216)),
                BallTeleportBrush = Brushes.Gold,
                ExtraLifeBrush = Brushes.LimeGreen,
                FreezeBrush = Brushes.LightBlue,
                DoubleScoreBrush = Brushes.Gold
            },
            ["Retro"] = new JezzballTheme
            {
                BackgroundBrush = Brushes.Black,
                WallPen = new Pen(Brushes.Gray, 3, lineCap: PenLineCap.Square),
                PreviewPen = new Pen(Brushes.White, 1, DashStyle.Dot),
                FilledBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30), 0.6),
                FlashBrush = new SolidColorBrush(Colors.White, 0.3),
                IceWallPen = new Pen(Brushes.Silver, 5),
                IcePreviewPen = new Pen(Brushes.Silver, 1, DashStyle.Dash),
                PowerUpBrush = Brushes.Yellow,
                BallNormalBrush = Brushes.White,
                BallSlowBrush = Brushes.LightGray,
                BallFastBrush = Brushes.Silver,
                BallSplittingBrush = Brushes.Gray,
                BallTeleportBrush = Brushes.Gold,
                ExtraLifeBrush = Brushes.LimeGreen,
                FreezeBrush = Brushes.LightBlue,
                DoubleScoreBrush = Brushes.Gold
            }
        };
    }

    public class PowerUp
    {
        public Point Position { get; }
        public PowerUpType Type { get; }
        public double Radius => 8;
        public Rect BoundingBox => new(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);

        public PowerUp(Point position, PowerUpType type)
        {
            Position = position;
            Type = type;
        }
    }

    public class BuildingWall
    {
        public Rect Area { get; }
        public WallOrientation Orientation { get; }
        public Point Origin { get; }
        public Rect WallPart1 { get; set; }
        public Rect WallPart2 { get; set; }
        public bool IsPart1Active { get; set; } = true;
        public bool IsPart2Active { get; set; } = true;
        public bool IsPart1Invincible { get; set; }
        public bool IsPart2Invincible { get; set; }

        public BuildingWall(Rect area, Point origin, WallOrientation orientation, bool isIceWall)
        {
            Area = area;
            Origin = origin;
            Orientation = orientation;
            double thickness = 4;
            WallPart1 = new Rect(origin, new Size(thickness, thickness));
            WallPart2 = new Rect(origin, new Size(thickness, thickness));

            if (isIceWall)
            {
                IsPart1Invincible = true;
                IsPart2Invincible = true;
            }
        }

        public bool IsComplete => !IsPart1Active && !IsPart2Active;
    }

    public class Ball
    {
        public Point Position { get; private set; }
        public Vector Velocity { get; private set; }
        public double Radius { get; }
        public IBrush Fill { get; private set; }
        public BallType Type { get; }
        private double _teleportTimer;

        public Ball(Point position, Vector velocity, JezzballTheme theme, BallType type = BallType.Normal, double radius = 8)
        {
            Position = position;
            Velocity = velocity;
            Type = type;
            Radius = radius;

            switch (Type)
            {
                case BallType.Slow:
                    Fill = theme.BallSlowBrush;
                    Velocity *= 0.7;
                    break;
                case BallType.Fast:
                    Fill = theme.BallFastBrush;
                    Velocity *= 1.3;
                    break;
                case BallType.Splitting:
                    Fill = theme.BallSplittingBrush;
                    break;
                case BallType.Teleporting:
                    Fill = theme.BallTeleportBrush;
                    _teleportTimer = 2.0;
                    break;
                default:
                    Fill = theme.BallNormalBrush;
                    break;
            }
        }

        public void ApplyTheme(JezzballTheme theme)
        {
            Fill = Type switch
            {
                BallType.Slow => theme.BallSlowBrush,
                BallType.Fast => theme.BallFastBrush,
                BallType.Splitting => theme.BallSplittingBrush,
                BallType.Teleporting => theme.BallTeleportBrush,
                _ => theme.BallNormalBrush
            };
        }

        public Rect BoundingBox => new(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);

        public void Update(Rect bounds, double dt)
        {
            if (Type == BallType.Teleporting)
            {
                _teleportTimer -= dt;
                if (_teleportTimer <= 0)
                {
                    var rand = new Random();
                    Position = new Point(
                        rand.NextDouble() * (bounds.Width - Radius * 2) + bounds.Left + Radius,
                        rand.NextDouble() * (bounds.Height - Radius * 2) + bounds.Top + Radius);
                    _teleportTimer = 2.0;
                }
            }

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

        public void Bounce(WallOrientation wallOrientation)
        {
            Velocity = wallOrientation == WallOrientation.Vertical
                ? Velocity.WithX(-Velocity.X)
                : Velocity.WithY(-Velocity.Y);
            JezzballSound.Play(JezzballSoundEvent.BallBounce);
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
        public bool FlashEffect { get; set; }
        public bool HasIceWallPowerUp { get; private set; }
        public bool FreezeActive { get; private set; }
        public bool DoubleScoreActive { get; private set; }

        public IReadOnlyList<Ball> Balls => _balls;
        public IReadOnlyList<Rect> ActiveAreas => _activeAreas;
        public IReadOnlyList<Rect> FilledAreas => _filledAreas;
        public IReadOnlyList<PowerUp> PowerUps => _powerUps;
        public BuildingWall? CurrentWall { get; private set; }

        private readonly List<Ball> _balls = new();
        private readonly List<Rect> _activeAreas = new();
        private readonly List<Rect> _filledAreas = new();
        private readonly List<PowerUp> _powerUps = new();
        private double _totalPlayArea;
        private JezzballTheme _theme;
        private double _freezeTimer;
        private double _doubleScoreTimer;

        private const double WallSpeed = 150.0;
        private const double CaptureRequirement = 0.75;

        public JezzballGameState(JezzballTheme theme)
        {
            _theme = theme;
            StartLevel(new Size(800, 570));
        }

        public void ApplyTheme(JezzballTheme theme)
        {
            _theme = theme;
            foreach (var b in _balls) b.ApplyTheme(_theme);
        }

        public void StartNewGame(Size gameSize)
        {
            Level = 1;
            Lives = 3;
            Score = 0;
            StartLevel(gameSize);
        }

        public void StartLevel(Size gameSize)
        {
            _activeAreas.Clear();
            _filledAreas.Clear();
            _balls.Clear();
            _powerUps.Clear();
            CurrentWall = null;
            HasIceWallPowerUp = false;
            FreezeActive = false;
            DoubleScoreActive = false;
            _freezeTimer = 0;
            _doubleScoreTimer = 0;
            Message = $"Level {Level}";

            var bounds = new Rect(0, 0, gameSize.Width, gameSize.Height);
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
                if (Level > 1 && rand.NextDouble() > 0.7) type = BallType.Slow;
                if (Level > 2 && rand.NextDouble() > 0.7) type = BallType.Fast;
                if (Level > 3 && rand.NextDouble() > 0.8) type = BallType.Splitting;
                if (Level > 4 && rand.NextDouble() > 0.85) type = BallType.Teleporting;

                _balls.Add(new Ball(bounds.Center, velocity, _theme, type));
            }
            RecalculateCapturedArea();
        }

        public void HandleClick(Point clickPosition)
        {
            if (Message != string.Empty)
            {
                if (IsGameOver) StartNewGame(new Size(800, 570));
                else StartLevel(new Size(800, 570));
                return;
            }

            var clickedPowerUp = _powerUps.FirstOrDefault(p => p.BoundingBox.Contains(clickPosition));
            if (clickedPowerUp != null)
            {
                switch (clickedPowerUp.Type)
                {
                    case PowerUpType.IceWall:
                        HasIceWallPowerUp = true;
                        break;
                    case PowerUpType.ExtraLife:
                        Lives++;
                        break;
                    case PowerUpType.Freeze:
                        FreezeActive = true;
                        _freezeTimer = 5.0;
                        break;
                    case PowerUpType.DoubleScore:
                        DoubleScoreActive = true;
                        _doubleScoreTimer = 10.0;
                        break;
                }
                _powerUps.Remove(clickedPowerUp);
                return;
            }
        }

        public bool TryStartWall(Point position, WallOrientation orientation)
        {
            if (CurrentWall != null || Message != string.Empty) return false;

            var area = _activeAreas.FirstOrDefault(r => r.Contains(position));
            if (area != default)
            {
                CurrentWall = new BuildingWall(area, position, orientation, HasIceWallPowerUp);
                if (HasIceWallPowerUp) HasIceWallPowerUp = false;
                return true;
            }
            return false;
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

            if (FreezeActive)
            {
                _freezeTimer -= dt;
                if (_freezeTimer <= 0)
                {
                    FreezeActive = false;
                }
            }

            if (DoubleScoreActive)
            {
                _doubleScoreTimer -= dt;
                if (_doubleScoreTimer <= 0)
                {
                    DoubleScoreActive = false;
                }
            }

            UpdateBalls(dt);
            UpdateWall(dt);
        }

        private void UpdateBalls(double dt)
        {
            if (FreezeActive)
            {
                return;
            }
            foreach (var ball in _balls.ToList())
            {
                var area = _activeAreas.FirstOrDefault(r => r.Intersects(ball.BoundingBox));
                if (area == default)
                {
                    area = _activeAreas
                        .OrderBy(a => Math.Abs(a.Center.X - ball.Position.X) + Math.Abs(a.Center.Y - ball.Position.Y))
                        .FirstOrDefault();
                }

                if (area != default) ball.Update(area, dt);
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
                
                if (CurrentWall.Orientation == WallOrientation.Vertical) w1 = w1.WithY(Math.Max(w1.Y, CurrentWall.Area.Y));
                else w1 = w1.WithX(Math.Max(w1.X, CurrentWall.Area.X));

                if ((CurrentWall.Orientation == WallOrientation.Vertical && w1.Top <= CurrentWall.Area.Top) ||
                    (CurrentWall.Orientation == WallOrientation.Horizontal && w1.Left <= CurrentWall.Area.Left))
                {
                    CurrentWall.IsPart1Active = false;
                }
                CurrentWall.WallPart1 = w1;

                foreach (var ball in _balls.Where(b => b.BoundingBox.Intersects(CurrentWall.WallPart1)))
                {
                    if (CurrentWall.IsPart1Invincible)
                    {
                        ball.Bounce(CurrentWall.Orientation);
                        JezzballSound.Play(JezzballSoundEvent.WallHit);
                        CurrentWall.IsPart1Invincible = false;
                    }
                    else { LoseLife("Wall Broken!"); return; }
                }
            }

            if (CurrentWall.IsPart2Active)
            {
                var w2 = CurrentWall.WallPart2;
                w2 = CurrentWall.Orientation == WallOrientation.Vertical
                    ? new Rect(w2.X, w2.Y, w2.Width, w2.Height + growAmount)
                    : new Rect(w2.X, w2.Y, w2.Width + growAmount, w2.Height);
                
                if (CurrentWall.Orientation == WallOrientation.Vertical) w2 = w2.WithHeight(Math.Min(w2.Height, CurrentWall.Area.Bottom - w2.Y));
                else w2 = w2.WithWidth(Math.Min(w2.Width, CurrentWall.Area.Right - w2.X));

                if ((CurrentWall.Orientation == WallOrientation.Vertical && w2.Bottom >= CurrentWall.Area.Bottom) ||
                    (CurrentWall.Orientation == WallOrientation.Horizontal && w2.Right >= CurrentWall.Area.Right))
                {
                    CurrentWall.IsPart2Active = false;
                }
                CurrentWall.WallPart2 = w2;

                foreach (var ball in _balls.Where(b => b.BoundingBox.Intersects(CurrentWall.WallPart2)))
                {
                    if (CurrentWall.IsPart2Invincible)
                    {
                        ball.Bounce(CurrentWall.Orientation);
                        JezzballSound.Play(JezzballSoundEvent.WallHit);
                        CurrentWall.IsPart2Invincible = false;
                    }
                    else { LoseLife("Wall Broken!"); return; }
                }
            }

            if (CurrentWall.IsComplete) CaptureAreas();
        }

        private void LoseLife(string reason)
        {
            Lives--;
            CurrentWall = null;
            JezzballSound.Play(JezzballSoundEvent.WallBreak);
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

            var ballsInArea1 = _balls.Where(b => newArea1.Intersects(b.BoundingBox)).ToList();
            var ballsInArea2 = _balls.Where(b => newArea2.Intersects(b.BoundingBox)).ToList();

            if (ballsInArea1.Count == 0)
            {
                _filledAreas.Add(newArea1);
                MaybeSpawnPowerUp(newArea1.Center);
            }
            else _activeAreas.Add(newArea1);

            if (ballsInArea2.Count == 0)
            {
                _filledAreas.Add(newArea2);
                MaybeSpawnPowerUp(newArea2.Center);
            }
            else _activeAreas.Add(newArea2);

            var ballsToSplit = _balls.Where(b => b.Type == BallType.Splitting && (ballsInArea1.Contains(b) || ballsInArea2.Contains(b))).ToList();
            foreach (var ball in ballsToSplit)
            {
                _balls.Remove(ball);
                var rand = new Random();
                for (int i = 0; i < 2; i++)
                {
                    var angle = rand.NextDouble() * 2 * Math.PI;
                    var speed = 100 + Level * 5;
                    var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                    _balls.Add(new Ball(ball.Position, velocity, _theme, BallType.Normal, ball.Radius * 0.8));
                }
            }

            CurrentWall = null;
            FlashEffect = true;
            RecalculateCapturedArea();

            if (CapturedPercentage >= CaptureRequirement)
            {
                Level++;
                long timeBonus = (long)TimeLeft.TotalSeconds * 100;
                Score += 1000 + timeBonus;
                JezzballSound.Play(JezzballSoundEvent.LevelComplete);
                Message = $"Level Complete!\nTime Bonus: {timeBonus}";
            }
        }

        private void MaybeSpawnPowerUp(Point location)
        {
            var rand = new Random();
            if (rand.NextDouble() < 0.3)
            {
                var typeRoll = rand.Next(4);
                var type = typeRoll switch
                {
                    0 => PowerUpType.IceWall,
                    1 => PowerUpType.ExtraLife,
                    2 => PowerUpType.Freeze,
                    _ => PowerUpType.DoubleScore
                };
                _powerUps.Add(new PowerUp(location, type));
            }
        }

        private void RecalculateCapturedArea()
        {
            double filledAreaSum = _filledAreas.Sum(r => r.Width * r.Height);
            if (filledAreaSum > 0)
            {
                var added = (long)filledAreaSum / 100;
                if (DoubleScoreActive) added *= 2;
                Score += added;
            }
            CapturedPercentage = _totalPlayArea > 0 ? filledAreaSum / _totalPlayArea : 0;
        }
    }
    #endregion

    #region Game View (UI Control)

    internal class JezzballControl : UserControl, IDisposable
    {
        private readonly JezzballGameState _gameState;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private Point _mousePosition;
        private WallOrientation _orientation = WallOrientation.Vertical;

        private readonly GameCanvas _gameCanvas;

        private IBrush _backgroundBrush = null!;
        private Pen _wallPen = null!;
        private Pen _previewPen = null!;
        private IBrush _filledBrush = null!;
        private IBrush _flashBrush = null!;
        private Pen _iceWallPen = null!;
        private Pen _icePreviewPen = null!;
        private IBrush _powerUpBrush = null!;
        private IBrush _powerUpExtraLifeBrush = null!;
        private IBrush _powerUpFreezeBrush = null!;
        private IBrush _powerUpDoubleScoreBrush = null!;
        private readonly Dictionary<JezzballSoundEvent, string> _soundPaths = new();
        private readonly Menu _menu = new();
        private JezzballTheme _theme;

        private readonly TextBlock _levelText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _livesText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _scoreText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _timeText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _capturedText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.WhiteSmoke };
        private readonly TextBlock _effectText = new() { Margin = new Thickness(10, 0), Foreground = Brushes.LightGreen };

        public Menu MenuBar => _menu;

        public JezzballControl(JezzballTheme theme)
        {
            _theme = theme;
            _gameState = new JezzballGameState(theme);
            LoadSoundSettings();
            var statusBar = new DockPanel { Background = Brushes.Black, Height = 30, Opacity = 0.8 };
            var restartButton = new Button { Content = "Restart", Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            restartButton.Click += (_, _) => RestartGame();

            DockPanel.SetDock(_levelText, Dock.Left);
            DockPanel.SetDock(_livesText, Dock.Left);
            DockPanel.SetDock(_scoreText, Dock.Left);
            DockPanel.SetDock(restartButton, Dock.Right);
            DockPanel.SetDock(_capturedText, Dock.Right);
            DockPanel.SetDock(_timeText, Dock.Right);
            statusBar.Children.AddRange(new Control[] { _levelText, _livesText, _scoreText, restartButton, _capturedText, _timeText, _effectText });

            var layout = new DockPanel();
            DockPanel.SetDock(_menu, Dock.Top);
            layout.Children.Add(_menu);
            DockPanel.SetDock(statusBar, Dock.Bottom);
            layout.Children.Add(statusBar);
            _gameCanvas = new GameCanvas(this);
            layout.Children.Add(_gameCanvas);

            Content = layout;
            ClipToBounds = true;
            Focusable = true;

            _gameCanvas.PointerPressed += OnPointerPressed;
            _gameCanvas.PointerMoved += OnPointerMoved;
            this.SizeChanged += OnControlResized;

            ApplyTheme(_theme);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += GameTick;
            _timer.Start();
            _stopwatch.Start();
        }

        internal void SetSound(JezzballSoundEvent ev, string path)
        {
            _soundPaths[ev] = path;
            if (!SettingsManager.Settings.PluginSoundEffects.TryGetValue("Jezzball", out var map))
            {
                map = new Dictionary<string, string>();
                SettingsManager.Settings.PluginSoundEffects["Jezzball"] = map;
            }
            map[ev.ToString()] = path;
            SettingsManager.Save();
            JezzballSound.Paths[ev] = path;
        }

        private void LoadSoundSettings()
        {
            if (SettingsManager.Settings.PluginSoundEffects.TryGetValue("Jezzball", out var map))
            {
                foreach (var kv in map)
                {
                    if (Enum.TryParse<JezzballSoundEvent>(kv.Key, out var ev))
                        _soundPaths[ev] = kv.Value;
                }
            }
            JezzballSound.Paths.Clear();
            foreach (var kv in _soundPaths)
                JezzballSound.Paths[kv.Key] = kv.Value;
        }

        public void SetTheme(string name)
        {
            if (JezzballThemes.All.TryGetValue(name, out var t))
            {
                _theme = t;
                ApplyTheme(_theme);
                _gameState.ApplyTheme(_theme);
            }
        }

        private void ApplyTheme(JezzballTheme theme)
        {
            _backgroundBrush = theme.BackgroundBrush;
            _wallPen = theme.WallPen;
            _previewPen = theme.PreviewPen;
            _filledBrush = theme.FilledBrush;
            _flashBrush = theme.FlashBrush;
            _iceWallPen = theme.IceWallPen;
            _icePreviewPen = theme.IcePreviewPen;
            _powerUpBrush = theme.PowerUpBrush;
            _powerUpExtraLifeBrush = theme.ExtraLifeBrush;
            _powerUpFreezeBrush = theme.FreezeBrush;
            _powerUpDoubleScoreBrush = theme.DoubleScoreBrush;
        }

        public void RestartGame() => _gameState.StartLevel(this.Bounds.Size);

        public void Dispose()
        {
            _timer.Stop();
            this.SizeChanged -= OnControlResized;
            _gameCanvas.PointerPressed -= OnPointerPressed;
            _gameCanvas.PointerMoved -= OnPointerMoved;
        }

        private void OnControlResized(object? sender, SizeChangedEventArgs e)
        {
            _gameState.StartLevel(e.NewSize);
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
                {
                    JezzballSound.Play(JezzballSoundEvent.Click);
                    _gameState.HandleClick(point.Position);
                }
                else
                {
                    var clickedPowerUp = _gameState.PowerUps.FirstOrDefault(p => p.BoundingBox.Contains(point.Position));
                    if (clickedPowerUp != null)
                    {
                        JezzballSound.Play(JezzballSoundEvent.Click);
                        _gameState.HandleClick(point.Position);
                    }
                    else
                    {
                        if (_gameState.TryStartWall(_mousePosition, _orientation))
                        {
                            JezzballSound.Play(JezzballSoundEvent.WallBuild);
                        }
                    }
                }
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
            var effects = new List<string>();
            if (_gameState.FreezeActive) effects.Add("Freeze");
            if (_gameState.DoubleScoreActive) effects.Add("2x Score");
            _effectText.Text = effects.Count > 0 ? $"Effects: {string.Join(", ", effects)}" : string.Empty;
        }

        internal async void RenderGame(DrawingContext context)
        {
            context.FillRectangle(_backgroundBrush, _gameCanvas.Bounds);

            foreach (var area in _gameState.FilledAreas) context.FillRectangle(_filledBrush, area);

            var gridPen = new Pen(new SolidColorBrush(Colors.White, 0.1), 1);
            foreach (var area in _gameState.ActiveAreas)
            {
                for (double x = area.Left; x < area.Right; x += 20) context.DrawLine(gridPen, new Point(x, area.Top), new Point(x, area.Bottom));
                for (double y = area.Top; y < area.Bottom; y += 20) context.DrawLine(gridPen, new Point(area.Left, y), new Point(area.Right, y));
            }

            foreach (var ball in _gameState.Balls) context.DrawEllipse(ball.Fill, null, ball.Position, ball.Radius, ball.Radius);
            foreach (var p in _gameState.PowerUps)
            {
                var brush = p.Type switch
                {
                    PowerUpType.ExtraLife => _powerUpExtraLifeBrush,
                    PowerUpType.Freeze => _powerUpFreezeBrush,
                    PowerUpType.DoubleScore => _powerUpDoubleScoreBrush,
                    _ => _powerUpBrush
                };
                context.DrawEllipse(brush, null, p.Position, p.Radius, p.Radius);
            }

            if (_gameState.CurrentWall is { } wall)
            {
                var pen = wall.IsPart1Invincible || wall.IsPart2Invincible ? _iceWallPen : _wallPen;
                
                if (wall.Orientation == WallOrientation.Vertical)
                {
                    if (wall.IsPart1Active) context.DrawLine(pen, new Point(wall.Origin.X, wall.WallPart1.Top), wall.Origin);
                    if (wall.IsPart2Active) context.DrawLine(pen, wall.Origin, new Point(wall.Origin.X, wall.WallPart2.Bottom));
                }
                else // Horizontal
                {
                    if (wall.IsPart1Active) context.DrawLine(pen, new Point(wall.WallPart1.Left, wall.Origin.Y), wall.Origin);
                    if (wall.IsPart2Active) context.DrawLine(pen, wall.Origin, new Point(wall.WallPart2.Right, wall.Origin.Y));
                }
            }
            else if (_gameState.Message == string.Empty)
            {
                var area = _gameState.ActiveAreas.FirstOrDefault(r => r.Contains(_mousePosition));
                if (area != default)
                {
                    var pen = _gameState.HasIceWallPowerUp ? _icePreviewPen : _previewPen;
                    if (_orientation == WallOrientation.Vertical)
                        context.DrawLine(pen, new Point(_mousePosition.X, area.Top), new Point(_mousePosition.X, area.Bottom));
                    else
                        context.DrawLine(pen, new Point(area.Left, _mousePosition.Y), new Point(area.Right, _mousePosition.Y));
                }
            }

            if (_gameState.FlashEffect)
            {
                context.FillRectangle(_flashBrush, _gameCanvas.Bounds);
                await Task.Delay(50);
                _gameState.FlashEffect = false;
            }

            if (_gameState.Message != string.Empty)
            {
                context.FillRectangle(new SolidColorBrush(Colors.Black, 0.5), _gameCanvas.Bounds);
                var formatted = new FormattedText(_gameState.Message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 36, Brushes.WhiteSmoke);
                var textPos = new Point((_gameCanvas.Bounds.Width - formatted.Width) / 2, (_gameCanvas.Bounds.Height - formatted.Height) / 2);
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

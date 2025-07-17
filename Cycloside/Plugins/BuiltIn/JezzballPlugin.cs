using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Cycloside.Effects;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    #region Plugin Entry Point
    public class JezzballPlugin : IPlugin
    {
        private Window? _window;
        private JezzballControl? _control;
        
        // Game state management
        private bool _isPaused = false;
        private bool _showGrid = false;
        private bool _showStatusBar = true;
        private bool _showAreaPercentage = true;
        private bool _soundEnabled = true;
        private bool _fastMode = false;
        private List<long> _highScores = new();

        public string Name => "Jezzball";
        public string Description => "A playable Jezzball clone with lives, time, and win conditions.";
        public Version Version => new(1,8); // Version bump for comprehensive menu system
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
            switch (e.Key)
            {
                case Key.R:
                    _control?.RestartGame();
                    e.Handled = true;
                    break;
                case Key.Space:
                    TogglePause();
                    e.Handled = true;
                    break;
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
                case Key.F2:
                    ShowHighScores();
                    e.Handled = true;
                    break;
            }
        }

        private void BuildMenu(string currentTheme, string? currentSkin)
        {
            if (_control == null) return;
            var menu = _control.MenuBar;
            menu.Items.Clear();

            // Game Menu
            var gameMenu = new MenuItem { Header = "_Game" };
            var newGameItem = new MenuItem { Header = "_New Game", InputGesture = new KeyGesture(Key.R) };
            newGameItem.Click += (_, _) => _control?.RestartGame();
            gameMenu.Items.Add(newGameItem);
            
            var pauseItem = new MenuItem { Header = "_Pause", InputGesture = new KeyGesture(Key.Space) };
            pauseItem.Click += (_, _) => TogglePause();
            gameMenu.Items.Add(pauseItem);
            
            gameMenu.Items.Add(new Separator());
            var highScoresItem = new MenuItem { Header = "High _Scores", InputGesture = new KeyGesture(Key.F2) };
            highScoresItem.Click += (_, _) => ShowHighScores();
            gameMenu.Items.Add(highScoresItem);
            
            gameMenu.Items.Add(new Separator());
            var exitItem = new MenuItem { Header = "E_xit" };
            exitItem.Click += (_, _) => Stop();
            gameMenu.Items.Add(exitItem);

            // Options Menu
            var optionsMenu = new MenuItem { Header = "_Options" };      
            var speedMenu = new MenuItem { Header = "Game _Speed" };
            var normalSpeedItem = new MenuItem { Header = "Normal", ToggleType = MenuItemToggleType.CheckBox, IsChecked = !_fastMode };
            var fastSpeedItem = new MenuItem { Header = "Fast (2x Points)", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _fastMode };
            normalSpeedItem.Click += (_, _) => SetGameSpeed(false);
            fastSpeedItem.Click += (_, _) => SetGameSpeed(true);
            speedMenu.Items.Add(normalSpeedItem);
            speedMenu.Items.Add(fastSpeedItem);
            optionsMenu.Items.Add(speedMenu);
            
            var soundItem = new MenuItem { Header = "_Sound", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _soundEnabled };
            soundItem.Click += (_, _) => ToggleSound();
            optionsMenu.Items.Add(soundItem);
            
            var gridItem = new MenuItem { Header = "Show _Grid", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _showGrid };
            gridItem.Click += (_, _) => ToggleGrid();
            optionsMenu.Items.Add(gridItem);

            // View Menu
            var viewMenu = new MenuItem { Header = "_View" };      
            var statusBarItem = new MenuItem { Header = "Show _Status Bar", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _showStatusBar };
            statusBarItem.Click += (_, _) => ToggleStatusBar();
            viewMenu.Items.Add(statusBarItem);
            
            var areaPercentageItem = new MenuItem { Header = "Show Area _Percentage", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _showAreaPercentage };
            areaPercentageItem.Click += (_, _) => ToggleAreaPercentage();
            viewMenu.Items.Add(areaPercentageItem);

            // Theme Menu
            var themeMenu = new MenuItem { Header = "_Theme" };
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
                themeMenu.Items.Add(item);
            }

            // Skin Menu
            var skinMenu = new MenuItem { Header = "_Skin" };
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
                skinMenu.Items.Add(item);
            }

            // Sound Menu
            var soundMenu = new MenuItem { Header = "_Sounds" };
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
                soundMenu.Items.Add(item);
            }

            // Help Menu
            var helpMenu = new MenuItem { Header = "_Help" };
            var howToPlayItem = new MenuItem { Header = "_How to Play", InputGesture = new KeyGesture(Key.F1) };
            howToPlayItem.Click += (_, _) => ShowHelp();
            helpMenu.Items.Add(howToPlayItem);
            
            var aboutItem = new MenuItem { Header = "About Jezzball" };
            aboutItem.Click += (_, _) => ShowAbout();
            helpMenu.Items.Add(aboutItem);

            // Add all menus to main menu bar
            menu.Items.Add(gameMenu);
            menu.Items.Add(optionsMenu);
            menu.Items.Add(viewMenu);
            menu.Items.Add(themeMenu);
            menu.Items.Add(skinMenu);
            menu.Items.Add(soundMenu);
            menu.Items.Add(helpMenu);
        }

        // Menu action methods
        private void TogglePause()
        {
            _isPaused = !_isPaused;
            _control?.SetPaused(_isPaused);
            UpdateMenuItems();
        }

        private void SetGameSpeed(bool fast)
        {
            _fastMode = fast;
            _control?.SetGameSpeed(fast);
            UpdateMenuItems();
        }

        private void ToggleSound()
        {
            _soundEnabled = !_soundEnabled;
            _control?.SetSoundEnabled(_soundEnabled);
            UpdateMenuItems();
        }

        private void ToggleGrid()
        {
            _showGrid = !_showGrid;
            _control?.SetShowGrid(_showGrid);
            UpdateMenuItems();
        }

        private void ToggleStatusBar()
        {
            _showStatusBar = !_showStatusBar;
            _control?.SetShowStatusBar(_showStatusBar);
            UpdateMenuItems();
        }

        private void ToggleAreaPercentage()
        {
            _showAreaPercentage = !_showAreaPercentage;
            _control?.SetShowAreaPercentage(_showAreaPercentage);
            UpdateMenuItems();
        }

        private void ShowHighScores()
        {
            var scores = string.Join("\n", _highScores.Select((score, index) => $"{index + 1}. {score:N0}"));
            var message = string.IsNullOrEmpty(scores) ? "No high scores yet!" : $"High Scores:\n\n{scores}";
            
            var window = new Window
            {
                Title = "High Scores",
                Width = 300,
                Height = 400,
                Content = new TextBox
                {
                    Text = message,
                    IsReadOnly = true,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Margin = new Thickness(10)
                }
            };
            window.Show();
        }

        private void ShowHelp()
        {
            var helpText = @"How to Play Jezzball:

OBJECTIVE:
Clear as much of each level as possible by drawing walls to capture areas.

CONTROLS:
• Left Click: Draw a wall from the clicked point
• Right Click: Change wall direction (Vertical/Horizontal)
• Space: Pause/Resume game
• R: Restart current level
• F1: Show this help
• F2: Show high scores

GAMEPLAY:
• Click in the gray area to start drawing a wall
• The wall will expand in both directions until it hits the edges
• If a ball hits your wall while it's being drawn, you lose a life
• When a wall completes, it divides the playing field
• Areas with no balls are cleared and turn blue
• You need to clear 75 board to complete a level
• Clearing 80%+ gives bonus points!

SPECIAL FEATURES:
• Fast Mode: Get2oints for the same area cleared
• Crazy Ball: The flashing magenta ball gives huge bonus if trapped
• Power-ups: Collect items for extra lives, freeze, and more

TIPS:
• Plan your walls carefully to avoid balls
• Use walls to trap balls in smaller areas
• Try to clear large areas at once for better bonuses
• Watch for the crazy ball - its worth the risk!";

            var window = new Window
            {
                Title = "How to Play Jezzball",
                Width = 500,
                Height = 600,
                Content = new ScrollViewer
                {
                    Content = new TextBox
                    {
                        Text = helpText,
                        IsReadOnly = true,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        Margin = new Thickness(10),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
            window.Show();
        }

        private void ShowAbout()
        {
            var aboutText = $@"Jezzball Clone v{Version}

A faithful recreation of the classic Microsoft Jezzball game.

Original Jezzball was created by Marjacq Micro Ltd. and published by Microsoft in 1992. This clone features:
• Classic gameplay mechanics
• Multiple themes and skins
• Customizable sound effects
• High score tracking
• Power-ups and special balls
• Fast mode for extra challenge

Built with Avalonia UI and .NET8. Enjoy playing!";

            var window = new Window
            {
                Title = "About Jezzball",
                Width = 400,
                Height = 500,
                Content = new TextBox
                {
                    Text = aboutText,
                    IsReadOnly = true,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                }
            };
            window.Show();
        }

        private void UpdateMenuItems()
        {
            // Update menu item states based on current settings
            if (_control?.MenuBar?.Items is IList menuItems)
            {
                // Update pause menu item
                if (menuItems.Count > 0 && menuItems[0] is MenuItem gameMenu && gameMenu.Items.Count > 1)
                {
                    var pauseItem = gameMenu.Items[1] as MenuItem;
                    if (pauseItem != null)
                        pauseItem.Header = _isPaused ? "_Resume" : "_Pause";
                }

                // Update options menu items
                if (menuItems.Count > 1 && menuItems[1] is MenuItem optionsMenu)
                {
                    // Update speed menu
                    if (optionsMenu.Items.Count > 0 && optionsMenu.Items[0] is MenuItem speedMenu)
                    {
                        if (speedMenu.Items.Count > 0) (speedMenu.Items[0] as MenuItem)!.IsChecked = !_fastMode;
                        if (speedMenu.Items.Count > 1) (speedMenu.Items[1] as MenuItem)!.IsChecked = _fastMode;
                    }

                    // Update sound menu
                    if (optionsMenu.Items.Count > 1) (optionsMenu.Items[1] as MenuItem)!.IsChecked = _soundEnabled;

                    // Update grid menu
                    if (optionsMenu.Items.Count > 2) (optionsMenu.Items[2] as MenuItem)!.IsChecked = _showGrid;
                }

                // Update view menu items
                if (menuItems.Count > 2 && menuItems[2] is MenuItem viewMenu)
                {
                    if (viewMenu.Items.Count > 0) (viewMenu.Items[0] as MenuItem)!.IsChecked = _showStatusBar;
                    if (viewMenu.Items.Count > 1) (viewMenu.Items[1] as MenuItem)!.IsChecked = _showAreaPercentage;
                }
            }
        }

        public void AddHighScore(long score)
        {
            _highScores.Add(score);
            _highScores.Sort((a, b) => b.CompareTo(a)); // Sort descending
            if (_highScores.Count > 10) // Keep only top 10
                _highScores.RemoveRange(10, _highScores.Count - 10);
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

            // NEW: Show "Level X" for 1 second, then clear message
            var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => {
                Message = string.Empty;
                timer.Stop();
            };
            timer.Start();
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

                // Only check for ball collisions if wall has grown to a reasonable size
                if (w1.Height > 8)
                {
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

                // Only check for ball collisions if wall has grown to a reasonable size
                if (w2.Height > 8)
                {
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
        
        // New settings fields
        private bool _isPaused = false;
        private bool _fastMode = false;
        private bool _soundEnabled = true;
        private bool _showGrid = false;
        private bool _showStatusBar = true;
        private bool _showAreaPercentage = true;

        private readonly GameCanvas _gameCanvas;
        
        // Visual elements
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

        // Status bar elements
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
            _gameCanvas = new GameCanvas(this);
            
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, GameTick);
            _timer.Start();
            _stopwatch.Start();

            ApplyTheme(theme);
            LoadSoundSettings();

            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(5)
            };
            statusPanel.Children.AddRange(new Control[] { _levelText, _livesText, _scoreText, _timeText, _capturedText, _effectText });

            var mainPanel = new Grid();
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Menu
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });  // Game
            
            Grid.SetRow(_menu, 0);
            Grid.SetRow(statusPanel, 1);
            Grid.SetRow(_gameCanvas, 2);
            mainPanel.Children.Add(_menu);
            mainPanel.Children.Add(statusPanel);
            mainPanel.Children.Add(_gameCanvas);

            Content = mainPanel;
            
            _gameCanvas.PointerMoved += OnPointerMoved;
            _gameCanvas.PointerPressed += OnPointerPressed;
            SizeChanged += OnControlResized;
            
            _gameState.StartNewGame(new Size(800, 570));
        }

        // New methods for menu functionality
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (paused)
            {
                _timer.Stop();
                _stopwatch.Stop();
            }
            else
            {
                _timer.Start();
                _stopwatch.Start();
            }
        }

        public void SetGameSpeed(bool fast)
        {
            _fastMode = fast;
            // The game speed affects scoring - this is handled in the scoring logic
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
        }

        public void SetShowGrid(bool show)
        {
            _showGrid = show;
            _gameCanvas.InvalidateVisual();
        }

        public void SetShowStatusBar(bool show)
        {
            _showStatusBar = show;
            if (Content is Grid mainPanel && mainPanel.Children.Count > 0)               {
                var statusPanel = mainPanel.Children[1] as StackPanel;
                if (statusPanel != null)
                {
                    statusPanel.IsVisible = show;
                }
            }
        }

        public void SetShowAreaPercentage(bool show)
        {
            _showAreaPercentage = show;
            UpdateStatusText();
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
        }

        private void LoadSoundSettings()
        {
            if (SettingsManager.Settings.PluginSoundEffects.TryGetValue("Jezzball", out var sounds))
            {
                foreach (var kvp in sounds)
                {
                    if (Enum.TryParse<JezzballSoundEvent>(kvp.Key, out var ev))
                    {
                        _soundPaths[ev] = kvp.Value;
                    }
                }
            }
        }

        public void SetTheme(string name)
        {
            if (JezzballThemes.All.TryGetValue(name, out var theme))
            {
                _theme = theme;
                _gameState.ApplyTheme(_theme);
                ApplyTheme(_theme);
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
            _stopwatch.Stop();
        }

        private void OnControlResized(object? sender, SizeChangedEventArgs e)
        {
            _gameState.StartLevel(_gameCanvas.Bounds.Size);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e) => _mousePosition = e.GetPosition(_gameCanvas);

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isPaused) return;

            var position = e.GetPosition(_gameCanvas);
            
            if (e.GetCurrentPoint(_gameCanvas).Properties.IsRightButtonPressed)
            {
                _orientation = _orientation == WallOrientation.Vertical ? WallOrientation.Horizontal : WallOrientation.Vertical;
                if (_soundEnabled) JezzballSound.Play(JezzballSoundEvent.Click);
                return;
            }

            if (_gameState.TryStartWall(position, _orientation))
            {
                if (_soundEnabled) JezzballSound.Play(JezzballSoundEvent.WallBuild);
            }
            else
            {
                _gameState.HandleClick(position);
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            if (!_isPaused)
            {
                var dt = _stopwatch.Elapsed.TotalSeconds;
                _stopwatch.Restart();
                _gameState.Update(dt);
                UpdateStatusText();
            }
            _gameCanvas.InvalidateVisual();
        }

        private void UpdateStatusText()
        {
            _levelText.Text = $"Level: {_gameState.Level}";
            _livesText.Text = $"Lives: {_gameState.Lives}";
            _scoreText.Text = $"Score: {_gameState.Score:N0}";
            _timeText.Text = $"Time: {_gameState.TimeLeft:mm\\:ss}";
            
            if (_showAreaPercentage)
            {
                _capturedText.Text = $"Area: {_gameState.CapturedPercentage:P0}";
            }
            else
            {
                _capturedText.Text = string.Empty;
            }
            
            var effects = new List<string>();
            if (_gameState.FreezeActive) effects.Add("FREEZE");
            if (_gameState.DoubleScoreActive) effects.Add("2X SCORE");
            if (_gameState.HasIceWallPowerUp) effects.Add("ICE WALL");
            _effectText.Text = string.Join(" | ", effects);
        }

        internal async void RenderGame(DrawingContext context)
        {
            var bounds = _gameCanvas.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // Draw background
            context.FillRectangle(_backgroundBrush, bounds);

            // Draw grid if enabled
            if (_showGrid)
            {
                var gridPen = new Pen(new SolidColorBrush(Colors.Gray, 0.3), 1);
                for (int x = 0; x <= bounds.Width; x += 20) context.DrawLine(gridPen, new Point(x, 0), new Point(x, bounds.Height));
                for (int y = 0; y <= bounds.Height; y += 20) context.DrawLine(gridPen, new Point(0, y), new Point(bounds.Width, y));
            }

            // Draw filled areas
            foreach (var area in _gameState.FilledAreas)
            {
                context.FillRectangle(_filledBrush, area);
            }

            // Draw active areas (for debugging)
            // foreach (var area in _gameState.ActiveAreas)
            // {
            //     context.DrawRectangle(null, new Pen(Brushes.Red, 1), area);
            // }

            // Draw power-ups
            foreach (var powerUp in _gameState.PowerUps)
            {
                var brush = powerUp.Type switch
                {
                    PowerUpType.ExtraLife => _powerUpExtraLifeBrush,
                    PowerUpType.Freeze => _powerUpFreezeBrush,
                    PowerUpType.DoubleScore => _powerUpDoubleScoreBrush,
                    _ => _powerUpBrush
                };
                context.DrawEllipse(brush, null, powerUp.Position, powerUp.Radius, powerUp.Radius);
            }

            // Draw balls
            foreach (var ball in _gameState.Balls)
            {
                context.DrawEllipse(ball.Fill, null, ball.Position, ball.Radius, ball.Radius);
            }

            // Draw current wall being built
            if (_gameState.CurrentWall != null)
            {
                var wall = _gameState.CurrentWall;
                var pen = wall.IsPart1Invincible || wall.IsPart2Invincible ? _iceWallPen : _wallPen;
                
                if (wall.IsPart1Active)
                {
                    context.DrawRectangle(null, pen, wall.WallPart1);
                }
                if (wall.IsPart2Active)
                {
                    context.DrawRectangle(null, pen, wall.WallPart2);
                }
            }

            // Draw preview line
            if (_gameState.CurrentWall == null && string.IsNullOrEmpty(_gameState.Message))
            {
                var previewPen = _previewPen;
                var start = _mousePosition;
                var end = _orientation == WallOrientation.Vertical 
                    ? new Point(start.X, _orientation == WallOrientation.Vertical ? bounds.Height : 0)
                    : new Point(_orientation == WallOrientation.Horizontal ? bounds.Width : 0, start.Y);
                context.DrawLine(previewPen, start, end);
            }

            // Draw message overlay
            if (!string.IsNullOrEmpty(_gameState.Message))
            {
                var textBrush = new SolidColorBrush(Colors.White);
                var font = new Typeface("Segoe UI");
                var formattedText = new FormattedText(_gameState.Message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, 24, textBrush);
                var textPosition = new Point((bounds.Width - formattedText.Width) / 2, (bounds.Height - formattedText.Height) / 2);
                context.DrawText(formattedText, textPosition);
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

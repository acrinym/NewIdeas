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
using Avalonia.Media.Immutable;
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
            SettingsManager.Settings.PluginSkins.TryGetValue("Jezzball", out var skin);
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

            var originalModeItem = new MenuItem { Header = "_Original Mode", ToggleType = MenuItemToggleType.CheckBox, IsChecked = false };
            originalModeItem.Click += (_, _) => ToggleOriginalMode();
            optionsMenu.Items.Add(originalModeItem);

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

        private void ToggleOriginalMode()
        {
            var originalMode = !_control?.MenuBar?.Items?.OfType<MenuItem>()
                ?.FirstOrDefault(m => m.Header?.ToString()?.Contains("Original Mode") == true)
                ?.Items?.OfType<MenuItem>()
                ?.FirstOrDefault()?.IsChecked ?? false;
            
            _control?.SetOriginalMode(originalMode);
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
    public enum BallType { Normal, Slow, Fast, Splitting, Teleporting, RedWhite }
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
        public IBrush BallRedWhiteBrush { get; init; } = Brushes.White; // Will be rendered specially
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
        private double _pulseTimer = 0;
        private readonly Queue<Point> _trail = new();
        private const int TrailLength = 5;

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
                case BallType.RedWhite:
                    Fill = theme.BallRedWhiteBrush;
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
                BallType.RedWhite => theme.BallRedWhiteBrush,
                _ => theme.BallNormalBrush
            };
        }

        public Rect BoundingBox => new(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);

        public void Update(Rect bounds, double dt)
        {
            _pulseTimer += dt * 33;
            
            // Update trail
            _trail.Enqueue(Position);
            if (_trail.Count > TrailLength)
                _trail.Dequeue();

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
                    _trail.Clear(); // Clear trail on teleport
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

        public double GetPulseScale() => 1 + Math.Sin(_pulseTimer) * 0.1; // 10% size variation
        
        public IReadOnlyList<Point> Trail => _trail.ToList();
    }

    public class Particle
    {
        public Point Position { get; private set; }
        public Vector Velocity { get; private set; }
        public double Life { get; private set; }
        public double MaxLife { get; }
        public IBrush Color { get; }
        public double Size { get; }

        public Particle(Point position, Vector velocity, IBrush color, double life = 1.0, double size = 3.0)
        {
            Position = position;
            Velocity = velocity;
            MaxLife = life;
            Life = life;
            Color = color;
            Size = size;
        }

        public void Update(double dt)
        {
            Position += Velocity * dt;
            Velocity *= 0.95; // Slow down over time
            Life -= dt;
        }

        public bool IsDead => Life <= 0;
        public double Alpha => Life / MaxLife;
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
        public bool ScreenShake { get; set; }
        public bool OriginalMode { get; set; }

        public IReadOnlyList<Ball> Balls => _balls;
        public IReadOnlyList<Rect> ActiveAreas => _activeAreas;
        public IReadOnlyList<Rect> FilledAreas => _filledAreas;
        public IReadOnlyList<PowerUp> PowerUps => _powerUps;
        public IReadOnlyList<Particle> Particles => _particles;
        public BuildingWall? CurrentWall { get; private set; }

        private readonly List<Ball> _balls = new();
        private readonly List<Rect> _activeAreas = new();
        private readonly List<Rect> _filledAreas = new();
        private readonly List<PowerUp> _powerUps = new();
        private readonly List<Particle> _particles = new();
        private double _totalPlayArea;
        private JezzballTheme _theme;
        private double _freezeTimer;
        private double _doubleScoreTimer;
        private double _screenShakeTimer;
        private double _lastCaptureTime;
        private int _comboCount;

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
            _particles.Clear();
            CurrentWall = null;
            HasIceWallPowerUp = false;
            FreezeActive = false;
            DoubleScoreActive = false;
            _freezeTimer = 0;
            _doubleScoreTimer = 0;
            _screenShakeTimer = 0;
            _lastCaptureTime = 0;
            _comboCount = 0;
            Message = $"Level {Level}";
            var bounds = new Rect(0, 0, gameSize.Width, gameSize.Height);
            _totalPlayArea = bounds.Width * bounds.Height;
            _activeAreas.Add(bounds);
            TimeLeft = TimeSpan.FromSeconds(20 + Level * 5);

            var rand = new Random();
            int ballCount = Level + 1;
            for (int i = 0; i < ballCount; i++)
            {
                var angle = rand.NextDouble() * 2 * Math.PI;
                var speed = 10 + Level * 15; // Balls get faster with level
                var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

                BallType type = BallType.Normal;
                if (Level > 1 && rand.NextDouble() > 0.7) type = BallType.Slow;
                if (Level > 2 && rand.NextDouble() > 0.7) type = BallType.Fast;
                if (Level > 3 && rand.NextDouble() > 0.8) type = BallType.Splitting;
                if (Level > 4 && rand.NextDouble() > 0.85) type = BallType.Teleporting;
                if (Level > 2 && rand.NextDouble() > 0.8) type = BallType.RedWhite;
                
                // Add crazy ball (golden ball) with 5% chance
                if (rand.NextDouble() > 0.95)
                {
                    type = BallType.Teleporting; // Use teleporting as crazy ball
                    _balls.Add(new Ball(bounds.Center, velocity, _theme, type, 12)); // Bigger radius
                }
                else
                {
                    _balls.Add(new Ball(bounds.Center, velocity, _theme, type));
                }
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
                // Create collection particles
                var rand = new Random();
                for (int i = 0; i < 8; i++)
                {
                    var angle = rand.NextDouble() * 2 * Math.PI;
                    var speed = 30 + rand.NextDouble() * 50;
                    var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                    var color = clickedPowerUp.Type switch
                    {
                        PowerUpType.ExtraLife => Brushes.LimeGreen,
                        PowerUpType.Freeze => Brushes.LightBlue,
                        PowerUpType.DoubleScore => Brushes.Gold,
                        _ => Brushes.Aqua
                    };
                    _particles.Add(new Particle(clickedPowerUp.Position, velocity, color, 1.0, 3.0));
                }

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

            // Update screen shake
            if (_screenShakeTimer > 0)
            {
                _screenShakeTimer -= dt;
                if (_screenShakeTimer <= 0)
                {
                    ScreenShake = false;
                }
            }

            UpdateBalls(dt);
            UpdateWall(dt);
            UpdateParticles(dt);
        }

        private void UpdateParticles(double dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update(dt);
                if (_particles[i].IsDead)
                    _particles.RemoveAt(i);
            }
        }

        private void UpdateBalls(double dt)
        {
            if (FreezeActive)
            {
                return;
            }
            foreach (var ball in _balls.ToList())
            {
                // Find the area the ball is in
                var area = _activeAreas.FirstOrDefault(r => r.Intersects(ball.BoundingBox));
                if (area == default)
                {
                    area = _activeAreas
                        .OrderBy(a => Math.Abs(a.Center.X - ball.Position.X) + Math.Abs(a.Center.Y - ball.Position.Y))
                        .FirstOrDefault();
                }

                if (area != default)
                {
                    // Subdivide the movement into small steps to avoid phasing through walls
                    double step = 1.0 / 120.0; // 120Hz substeps
                    double t = 0;
                    while (t < dt)
                    {
                        double subdt = Math.Min(step, dt - t);
                        ball.Update(area, subdt);
                        t += subdt;
                    }
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
                if (CurrentWall.Orientation == WallOrientation.Vertical) w1 = w1.WithY(Math.Max(w1.Y, CurrentWall.Area.Y));
                else w1 = w1.WithX(Math.Max(w1.X, CurrentWall.Area.X));
                if ((CurrentWall.Orientation == WallOrientation.Vertical && w1.Top <= CurrentWall.Area.Top) ||
                    (CurrentWall.Orientation == WallOrientation.Horizontal && w1.Left <= CurrentWall.Area.Left))
                {
                    CurrentWall.IsPart1Active = false;
                }
                CurrentWall.WallPart1 = w1;
                // Check for ball collisions at every substep
                if (w1.Height > 2 && w1.Width > 2)
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
                // Check for ball collisions at every substep
                if (w2.Height > 2 && w2.Width > 2)
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
            
            // Create explosion particles
            var rand = new Random();
            for (int i = 0; i < 15; i++)
            {
                var angle = rand.NextDouble() * 2 * Math.PI;
                var speed = 50 + rand.NextDouble() * 100;
                var velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
                var position = new Point(400 + rand.NextDouble() * 200, 300 + rand.NextDouble() * 200); // Center of screen
                _particles.Add(new Particle(position, velocity, Brushes.Red, 1.5, 4));
            }
            
            Message = Lives <= 0 ? "Game Over! Click to restart." : reason + " Click to continue.";
        }

        private void CaptureAreas()
        {
            if (CurrentWall == null) return;
            var area = CurrentWall.Area;
            Rect newArea1, newArea2;

            // Clamp split to area bounds
            if (CurrentWall.Orientation == WallOrientation.Vertical)
            {
                double splitX = Math.Clamp(CurrentWall.Origin.X, area.Left + 1, area.Right - 1);
                newArea1 = new Rect(area.Left, area.Top, splitX - area.Left, area.Height);
                newArea2 = new Rect(splitX, area.Top, area.Right - splitX, area.Height);
            }
            else
            {
                double splitY = Math.Clamp(CurrentWall.Origin.Y, area.Top + 1, area.Bottom - 1);
                newArea1 = new Rect(area.Left, area.Top, area.Width, splitY - area.Top);
                newArea2 = new Rect(area.Left, splitY, area.Width, area.Bottom - splitY);
            }

            // Only add valid (non-empty) areas
            List<Rect> newAreas = new();
            if (newArea1.Width > 2 && newArea1.Height > 2) newAreas.Add(newArea1);
            if (newArea2.Width > 2 && newArea2.Height > 2) newAreas.Add(newArea2);

            _activeAreas.Remove(area);

            int capturedAreas = 0;
            foreach (var newArea in newAreas)
            {
                var ballsInArea = _balls.Where(b => newArea.Intersects(b.BoundingBox)).ToList();
                if (ballsInArea.Count == 0)
                {
                    _filledAreas.Add(newArea);
                    capturedAreas++;
                    MaybeSpawnPowerUp(newArea.Center);
                }
                else
                {
                    _activeAreas.Add(newArea);
                }
            }

            // Handle splitting balls
            var ballsToSplit = _balls.Where(b => b.Type == BallType.Splitting && newAreas.Any(a => a.Intersects(b.BoundingBox))).ToList();
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
            
            // Screen shake effect
            ScreenShake = true;
            _screenShakeTimer = 0.2;
            RecalculateCapturedArea();

            // Combo system
            double currentTime = Environment.TickCount / 100;
            if (currentTime - _lastCaptureTime < 2.0) // Within 2 seconds
            {
                _comboCount++;
            }
            else
            {
                _comboCount = 1;
            }
            _lastCaptureTime = currentTime;

            if (CapturedPercentage >= CaptureRequirement)
            {
                Level++;
                long timeBonus = (long)TimeLeft.TotalSeconds * 100;
                long comboBonus = _comboCount * 500;
                Score += 1000 + timeBonus + comboBonus;
                JezzballSound.Play(JezzballSoundEvent.LevelComplete);
                Message = $"Level Complete!\nTime Bonus: {timeBonus}\nCombo Bonus: {comboBonus}";
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
                
                // Bonus for crazy balls captured
                var crazyBalls = _balls.Where(b => b.Type == BallType.Teleporting && b.Radius > 10).Count();
                if (crazyBalls > 0)
                {
                    added += crazyBalls * 10; // Huge bonus for crazy balls
                }
                
                Score += added;
            }
            CapturedPercentage = _totalPlayArea > 0 ? filledAreaSum / _totalPlayArea : 0;
        }

        public void SetOriginalMode(bool originalMode)
        {
            OriginalMode = originalMode;
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
        
        // Flash effect timer for area capture
        private double _flashTimer = 0;
        private readonly DispatcherTimer _flashUpdateTimer;
        
        public Menu MenuBar => _menu;

        public JezzballControl(JezzballTheme theme)
        {
            _theme = theme;
            _gameState = new JezzballGameState(theme);
            _gameCanvas = new GameCanvas(this);
            _flashUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16), // ~60fps
                IsEnabled = false
            };
            _flashUpdateTimer.Tick += (s, e) =>
            {
                _flashTimer += 0.1;
                if (_flashTimer >= 1.0)
                {
                    _flashTimer = 0;
                    _gameState.FlashEffect = false;
                    _flashUpdateTimer.IsEnabled = false;
                }
                InvalidateVisual();
            };
            
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
            // Actually change the timer interval for fast mode
            if (_timer != null)
                _timer.Interval = fast ? TimeSpan.FromMilliseconds(8) : TimeSpan.FromMilliseconds(16);
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

        public void SetOriginalMode(bool originalMode)
        {
            _gameState.SetOriginalMode(originalMode);
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
            _flashUpdateTimer.Stop();
            _timer.Tick -= GameTick;
            _flashUpdateTimer.Tick -= (s, e) => _flashTimer += 0.016;
        }

        private void OnControlResized(object? sender, SizeChangedEventArgs e)
        {
            _gameState.StartLevel(_gameCanvas.Bounds.Size);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var position = e.GetPosition(_gameCanvas);
            _mousePosition = new Point(position.X, position.Y);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_gameState.IsGameOver || _isPaused) return;

            var position = e.GetPosition(_gameCanvas);
            var point = new Point(position.X, position.Y);

            if (e.GetCurrentPoint(_gameCanvas).Properties.IsLeftButtonPressed)
            {
                if (_gameState.TryStartWall(point, _orientation))
                {
                    if (_soundEnabled) JezzballSound.Play(JezzballSoundEvent.WallBuild);
                }
                else
                {
                    _gameState.HandleClick(point);
                }
            }
            else if (e.GetCurrentPoint(_gameCanvas).Properties.IsRightButtonPressed)
            {
                _orientation = _orientation == WallOrientation.Vertical
                    ? WallOrientation.Horizontal
                    : WallOrientation.Vertical;
                if (_soundEnabled) JezzballSound.Play(JezzballSoundEvent.Click);
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            var elapsed = _stopwatch.Elapsed;
            _stopwatch.Restart();
            
            if (!_isPaused)
            {
                var dt = elapsed.TotalSeconds;
                if (_fastMode) dt *= 2;
                
                _gameState.Update(dt);
                
                if (_gameState.FlashEffect && _flashTimer <= 0)
                {
                    _flashTimer = 0.2; // Flash for 0.2 seconds
                }
                if (_flashTimer > 0)
                {
                    _flashTimer -= dt;
                    if (_flashTimer < 0) _flashTimer = 0;
                }
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

        internal void RenderGame(DrawingContext context)
        {
            var bounds = _gameCanvas.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // Apply screen shake effect
            var shakeOffset = new Point(0, 0);
            var transform = Matrix.Identity;
            if (_gameState.ScreenShake)
            {
                var rand = new Random();
                shakeOffset = new Point(
                    (rand.NextDouble() - 0.5) * 4,
                    (rand.NextDouble() - 0.5) * 4
                );
                transform = Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y);
                context.PushTransform(transform);
            }

            try
            {
                // Draw background - use solid black in original mode
                if (_gameState.OriginalMode)
                {
                    context.FillRectangle(Brushes.Black, bounds);
                }
                else
                {
                    // Draw background with subtle gradient
                    var backgroundBrush = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromRgb(20, 20, 30), 0),
                            new GradientStop(Color.FromRgb(10, 10, 20), 1)
                        }
                    };
                    context.FillRectangle(backgroundBrush, bounds);
                }

                // Draw grid if enabled
                if (_showGrid)
                {
                    var gridPen = new Pen(new SolidColorBrush(Colors.Gray, 0.3), 1);
                    for (double x = 0.0; x <= bounds.Width; x += 20.0)
                    {
                        var startPoint = new Point(x, 0.0);
                        var endPoint = new Point(x, bounds.Height);
                        context.DrawLine(gridPen, startPoint, endPoint);
                    }
                    for (double y = 0.0; y <= bounds.Height; y += 20.0)
                    {
                        var startPoint = new Point(0.0, y);
                        var endPoint = new Point(bounds.Width, y);
                        context.DrawLine(gridPen, startPoint, endPoint);
                    }
                }

                // Draw filled areas
                foreach (var area in _gameState.FilledAreas)
                {
                    context.FillRectangle(_filledBrush, area);
                }

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

                    // Add glow effect to power-ups (only in modern mode)
                    if (!_gameState.OriginalMode && brush is SolidColorBrush solidBrush)
                    {
                        var glowBrush = new SolidColorBrush(Color.FromArgb(
                            (byte)Math.Round(solidBrush.Color.A * 0.5),
                            solidBrush.Color.R,
                            solidBrush.Color.G,
                            solidBrush.Color.B));
                        context.DrawEllipse(glowBrush, null, powerUp.Position, powerUp.Radius * 1.5, powerUp.Radius * 1.5);
                    }
                    context.DrawEllipse(brush, null, powerUp.Position, powerUp.Radius, powerUp.Radius);
                }

                // Draw ball trails (only in modern mode)
                if (!_gameState.OriginalMode)
                {
                    foreach (var ball in _gameState.Balls)
                    {
                        if (ball.Fill is SolidColorBrush solidBrush)
                        {
                            var trail = ball.Trail.ToList();
                            for (int i = 0; i < trail.Count; i++)
                            {
                                double alpha = (double)i / trail.Count * 0.3;
                                var trailBrush = new SolidColorBrush(Color.FromArgb(
                                    (byte)Math.Round(solidBrush.Color.A * alpha),
                                    solidBrush.Color.R,
                                    solidBrush.Color.G,
                                    solidBrush.Color.B));
                                context.DrawEllipse(trailBrush, null, trail[i], ball.Radius * 0.8, ball.Radius * 0.8);
                            }
                        }
                    }
                }

                // Draw balls with glow and pulse effects
                foreach (var ball in _gameState.Balls)
                {
                    var pulseScale = ball.GetPulseScale();
                    
                    // Add glow effect (only in modern mode)
                    if (!_gameState.OriginalMode && ball.Fill is SolidColorBrush solidBrush)
                    {
                        var glowBrush = new SolidColorBrush(Color.FromArgb(
                            (byte)Math.Round(solidBrush.Color.A * 0.7),
                            solidBrush.Color.R,
                            solidBrush.Color.G,
                            solidBrush.Color.B));
                        context.DrawEllipse(glowBrush, null, ball.Position, ball.Radius * pulseScale, ball.Radius * pulseScale);
                    }
                    
                    // Special rendering for different ball types
                    if (ball.Type == BallType.Normal)
                    {
                        var radius = ball.Radius;
                        var center = ball.Position;
                        
                        // Create semicircle geometries
                        var leftHalf = new EllipseGeometry(new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
                        var rightHalf = new EllipseGeometry(new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
                        
                        // Clip to left half (red)
                        var leftClip = new RectangleGeometry(new Rect(center.X - radius, center.Y - radius, radius, radius * 2));
                        var leftCombined = new CombinedGeometry(GeometryCombineMode.Intersect, leftHalf, leftClip);
                        context.DrawGeometry(new SolidColorBrush(Colors.Red), null, leftCombined);
                        
                        // Clip to right half (blue)
                        var rightClip = new RectangleGeometry(new Rect(center.X, center.Y - radius, radius, radius * 2));
                        var rightCombined = new CombinedGeometry(GeometryCombineMode.Intersect, rightHalf, rightClip);
                        context.DrawGeometry(new SolidColorBrush(Colors.Blue), null, rightCombined);
                    }
                    else if (ball.Type == BallType.RedWhite)
                    {
                        var radius = ball.Radius;
                        var center = ball.Position;
                        
                        // Create semicircle geometries for red/white ball
                        var leftHalf = new EllipseGeometry(new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
                        var rightHalf = new EllipseGeometry(new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
                        
                        // Clip to left half (red)
                        var leftClip = new RectangleGeometry(new Rect(center.X - radius, center.Y - radius, radius, radius * 2));
                        var leftCombined = new CombinedGeometry(GeometryCombineMode.Intersect, leftHalf, leftClip);
                        context.DrawGeometry(new SolidColorBrush(Colors.Red), null, leftCombined);
                        
                        // Clip to right half (white)
                        var rightClip = new RectangleGeometry(new Rect(center.X, center.Y - radius, radius, radius * 2));
                        var rightCombined = new CombinedGeometry(GeometryCombineMode.Intersect, rightHalf, rightClip);
                        context.DrawGeometry(new SolidColorBrush(Colors.White), null, rightCombined);
                    }
                    else
                    {
                        // Regular rendering for other ball types
                        context.DrawEllipse(ball.Fill, null, ball.Position, ball.Radius, ball.Radius);
                    }
                }

                // Draw current wall being built
                if (_gameState.CurrentWall != null)
                {
                    var wall = _gameState.CurrentWall;
                    Pen pen;
                    
                    if (_gameState.OriginalMode)
                    {
                        pen = wall.IsPart1Invincible || wall.IsPart2Invincible ? 
                            new Pen(new SolidColorBrush(Colors.Red), 3) : new Pen(new SolidColorBrush(Colors.Blue), 3);
                    }
                    else
                    {
                        pen = wall.IsPart1Invincible || wall.IsPart2Invincible ? _iceWallPen : _wallPen;
                        
                        // Add glow effect to walls being built
                        if (pen.Brush is SolidColorBrush solidBrush)
                        {
                            var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(
                                (byte)Math.Round(solidBrush.Color.A * 0.5),
                                solidBrush.Color.R,
                                solidBrush.Color.G,
                                solidBrush.Color.B)), pen.Thickness + 2);
                            if (wall.IsPart1Active)
                            {
                                context.DrawRectangle(null, glowPen, wall.WallPart1);
                            }
                            if (wall.IsPart2Active)
                            {
                                context.DrawRectangle(null, glowPen, wall.WallPart2);
                            }
                        }
                    }
                    
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
                    Pen previewPen = _gameState.OriginalMode ?
                        new Pen(new SolidColorBrush(Colors.Blue), 2, DashStyle.Dash) :
                        _previewPen;

                    if (previewPen.Brush is SolidColorBrush solidBrush)
                    {
                        var glowPreviewPen = new Pen(new SolidColorBrush(Color.FromArgb(
                            (byte)Math.Round(solidBrush.Color.A * 0.5),
                            solidBrush.Color.R,
                            solidBrush.Color.G,
                            solidBrush.Color.B)), previewPen.Thickness + 1);

                        var start = _mousePosition;
                        if (_orientation == WallOrientation.Vertical)
                        {
                            var top = new Point(start.X, bounds.Top);
                            var bottom = new Point(start.X, bounds.Bottom);
                            context.DrawLine(glowPreviewPen, top, bottom);
                            context.DrawLine(previewPen, top, bottom);
                        }
                        else
                        {
                            var left = new Point(bounds.Left, start.Y);
                            var right = new Point(bounds.Right, start.Y);
                            context.DrawLine(glowPreviewPen, left, right);
                            context.DrawLine(previewPen, left, right);
                        }
                    }
                }

                // Draw flash effect
                if (_gameState.FlashEffect)
                {
                    var flashOpacity = Math.Sin(_flashTimer * Math.PI * 2) * 0.5 + 0.5;
                    var flashBrush = new SolidColorBrush(Colors.White, flashOpacity * 0.3);
                    context.FillRectangle(flashBrush, bounds);
                }

                // Draw message text
                if (!string.IsNullOrEmpty(_gameState.Message))
                {
                    var formattedText = new FormattedText(
                        _gameState.Message,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        Typeface.Default,
                        36.0,
                        new SolidColorBrush(Colors.White)
                    );

                    var textPosition = new Point(
                        (bounds.Width - formattedText.Width) / 2.0,
                        (bounds.Height - formattedText.Height) / 2.0
                    );

                    context.DrawText(formattedText, textPosition);
                }
            }
            finally
            {
                // Clean up any remaining transforms
                if (_gameState.ScreenShake)
                {
                    context.PushTransform(Matrix.CreateTranslation(-transform.M31, -transform.M32));
                }
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

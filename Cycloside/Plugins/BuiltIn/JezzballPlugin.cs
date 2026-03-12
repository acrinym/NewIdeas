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

        private bool _isPaused = false;
        private bool _showGrid = false;
        private bool _showStatusBar = true;
        private bool _showAreaPercentage = true;
        private bool _soundEnabled = true;
        private bool _fastMode = false;
        private bool _originalMode = false;
        private readonly List<long> _highScores = new();
        private MenuItem? _pauseMenuItem;
        private MenuItem? _normalSpeedMenuItem;
        private MenuItem? _fastSpeedMenuItem;
        private MenuItem? _soundMenuItem;
        private MenuItem? _gridMenuItem;
        private MenuItem? _originalModeMenuItem;
        private MenuItem? _statusBarMenuItem;
        private MenuItem? _areaPercentageMenuItem;

        public string Name => "Jezzball";
        public string Description => "A retro Jezzball clone with themes, sounds, Original Mode, and shell-era visual styles.";
        public Version Version => new(1, 10);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            LoadPersistentSettings();
            var themeName = SettingsManager.Settings.PluginGameThemes.TryGetValue("Jezzball", out var t) ? t : "Classic";
            var theme = JezzballThemes.All.TryGetValue(themeName, out var th) ? th : JezzballThemes.All["Classic"];
            _control = new JezzballControl(theme, SettingsManager.Settings.Jezzball, RecordHighScore);

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
            if (!string.IsNullOrWhiteSpace(skin))
            {
                SkinManager.ApplySkinTo(_window, skin);
            }
            BuildMenu(themeName, skin);
            ApplyTransientSettingsToControl();
            UpdateMenuItems();
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
            ClearMenuReferences();
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _control?.StartNewGame();
                e.Handled = true;
                return;
            }

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
            var newGameItem = new MenuItem { Header = "_New Game", InputGesture = new KeyGesture(Key.N, KeyModifiers.Control) };
            newGameItem.Click += (_, _) => _control?.StartNewGame();
            gameMenu.Items.Add(newGameItem);

            var restartLevelItem = new MenuItem { Header = "_Restart Level", InputGesture = new KeyGesture(Key.R) };
            restartLevelItem.Click += (_, _) => _control?.RestartGame();
            gameMenu.Items.Add(restartLevelItem);

            _pauseMenuItem = new MenuItem { Header = "_Pause", InputGesture = new KeyGesture(Key.Space) };
            _pauseMenuItem.Click += (_, _) => TogglePause();
            gameMenu.Items.Add(_pauseMenuItem);
            
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
            _normalSpeedMenuItem = new MenuItem { Header = "Normal", ToggleType = MenuItemToggleType.CheckBox, IsChecked = !_fastMode };
            _fastSpeedMenuItem = new MenuItem { Header = "Fast (2x Points)", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _fastMode };
            _normalSpeedMenuItem.Click += (_, _) => SetGameSpeed(false);
            _fastSpeedMenuItem.Click += (_, _) => SetGameSpeed(true);
            speedMenu.Items.Add(_normalSpeedMenuItem);
            speedMenu.Items.Add(_fastSpeedMenuItem);
            optionsMenu.Items.Add(speedMenu);
            
            _soundMenuItem = new MenuItem { Header = "_Sound", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _soundEnabled };
            _soundMenuItem.Click += (_, _) => ToggleSound();
            optionsMenu.Items.Add(_soundMenuItem);
            
            _gridMenuItem = new MenuItem { Header = "Show _Grid", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _showGrid };
            _gridMenuItem.Click += (_, _) => ToggleGrid();
            optionsMenu.Items.Add(_gridMenuItem);

            _originalModeMenuItem = new MenuItem { Header = "_Original Mode", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _originalMode };
            _originalModeMenuItem.Click += (_, _) => ToggleOriginalMode();
            optionsMenu.Items.Add(_originalModeMenuItem);

            optionsMenu.Items.Add(new Separator());
            var settingsItem = new MenuItem { Header = "_Settings..." };
            settingsItem.Click += async (_, _) => await ShowSettingsAsync();
            optionsMenu.Items.Add(settingsItem);

            // View Menu
            var viewMenu = new MenuItem { Header = "_View" };      
            _statusBarMenuItem = new MenuItem { Header = "Show _Status Bar", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _showStatusBar };
            _statusBarMenuItem.Click += (_, _) => ToggleStatusBar();
            viewMenu.Items.Add(_statusBarMenuItem);
            
            _areaPercentageMenuItem = new MenuItem { Header = "Show Area _Percentage", ToggleType = MenuItemToggleType.CheckBox, IsChecked = _showAreaPercentage };
            _areaPercentageMenuItem.Click += (_, _) => ToggleAreaPercentage();
            viewMenu.Items.Add(_areaPercentageMenuItem);

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
            SavePersistentSettings();
            UpdateMenuItems();
        }

        private void ToggleSound()
        {
            _soundEnabled = !_soundEnabled;
            _control?.SetSoundEnabled(_soundEnabled);
            SavePersistentSettings();
            UpdateMenuItems();
        }

        private void ToggleGrid()
        {
            _showGrid = !_showGrid;
            _control?.SetShowGrid(_showGrid);
            SavePersistentSettings();
            UpdateMenuItems();
        }

        private void ToggleStatusBar()
        {
            _showStatusBar = !_showStatusBar;
            _control?.SetShowStatusBar(_showStatusBar);
            SavePersistentSettings();
            UpdateMenuItems();
        }

        private void ToggleAreaPercentage()
        {
            _showAreaPercentage = !_showAreaPercentage;
            _control?.SetShowAreaPercentage(_showAreaPercentage);
            SavePersistentSettings();
            UpdateMenuItems();
        }

        private void ToggleOriginalMode()
        {
            _originalMode = !_originalMode;
            _control?.SetOriginalMode(_originalMode);
            SavePersistentSettings();
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
            ApplyChildWindowAppearance(window);
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
• You need to clear the target area percentage to complete a level
• Time's up costs a life, so keep moving
• Clearing the board quickly gives time bonus points

SPECIAL FEATURES:
• Fast Mode: Get 2x points for the same area cleared
• Crazy Ball: The flashing magenta ball gives huge bonus if trapped
• Power-ups: Collect items for extra lives, freeze, and more
• Settings let you tune time limit, lives, target area, and power-up rate

TIPS:
• Plan your walls carefully to avoid balls
• Use walls to trap balls in smaller areas
• Try to clear large areas at once for better bonuses
• Watch for the crazy ball - it's worth the risk!";

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
            ApplyChildWindowAppearance(window);
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

Built with Avalonia UI and .NET 8. Enjoy playing!";

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
            ApplyChildWindowAppearance(window);
            window.Show();
        }

        private void UpdateMenuItems()
        {
            if (_pauseMenuItem != null)
            {
                _pauseMenuItem.Header = _isPaused ? "_Resume" : "_Pause";
            }

            if (_normalSpeedMenuItem != null)
            {
                _normalSpeedMenuItem.IsChecked = !_fastMode;
            }

            if (_fastSpeedMenuItem != null)
            {
                _fastSpeedMenuItem.IsChecked = _fastMode;
            }

            if (_soundMenuItem != null)
            {
                _soundMenuItem.IsChecked = _soundEnabled;
            }

            if (_gridMenuItem != null)
            {
                _gridMenuItem.IsChecked = _showGrid;
            }

            if (_originalModeMenuItem != null)
            {
                _originalModeMenuItem.IsChecked = _originalMode;
            }

            if (_statusBarMenuItem != null)
            {
                _statusBarMenuItem.IsChecked = _showStatusBar;
            }

            if (_areaPercentageMenuItem != null)
            {
                _areaPercentageMenuItem.IsChecked = _showAreaPercentage;
            }
        }

        public void AddHighScore(long score)
        {
            _highScores.Add(score);
            _highScores.Sort((a, b) => b.CompareTo(a));
            if (_highScores.Count > 10)
                _highScores.RemoveRange(10, _highScores.Count - 10);
        }

        private static IEnumerable<string> GetSkinNames()
        {
            return SkinManager.GetAvailableSkins();
        }

        private void LoadPersistentSettings()
        {
            var settings = SettingsManager.Settings.Jezzball;
            var changed = NormalizeJezzballSettings(settings);
            _fastMode = settings.FastMode;
            _soundEnabled = settings.SoundEnabled;
            _showGrid = settings.ShowGrid;
            _showStatusBar = settings.ShowStatusBar;
            _showAreaPercentage = settings.ShowAreaPercentage;
            _originalMode = settings.OriginalMode;

            _highScores.Clear();
            _highScores.AddRange(settings.HighScores.OrderByDescending(score => score).Take(10));

            if (changed)
            {
                SavePersistentSettings();
            }
        }

        private void SavePersistentSettings()
        {
            var settings = SettingsManager.Settings.Jezzball;
            settings.FastMode = _fastMode;
            settings.SoundEnabled = _soundEnabled;
            settings.ShowGrid = _showGrid;
            settings.ShowStatusBar = _showStatusBar;
            settings.ShowAreaPercentage = _showAreaPercentage;
            settings.OriginalMode = _originalMode;
            settings.HighScores = _highScores.Take(10).ToList();
            NormalizeJezzballSettings(settings);
            SettingsManager.Save();
        }

        private void ApplyTransientSettingsToControl()
        {
            if (_control == null)
            {
                return;
            }

            _control.SetGameSpeed(_fastMode);
            _control.SetSoundEnabled(_soundEnabled);
            _control.SetShowGrid(_showGrid);
            _control.SetShowStatusBar(_showStatusBar);
            _control.SetShowAreaPercentage(_showAreaPercentage);
            _control.SetOriginalMode(_originalMode);
        }

        private async Task ShowSettingsAsync()
        {
            if (_window == null || _control == null)
            {
                return;
            }

            var settings = SettingsManager.Settings.Jezzball;
            NormalizeJezzballSettings(settings);

            var startingLivesBox = CreateNumberBox(settings.StartingLives);
            var baseTimeBox = CreateNumberBox(settings.BaseLevelTimeSeconds);
            var timePerLevelBox = CreateNumberBox(settings.TimePerLevelSeconds);
            var captureRequirementBox = CreateNumberBox(settings.CaptureRequirementPercent);
            var powerUpSpawnBox = CreateNumberBox(settings.PowerUpSpawnPercent);
            var fastModeBox = new CheckBox { Content = "Start in Fast Mode", IsChecked = _fastMode };
            var soundEnabledBox = new CheckBox { Content = "Enable Sound", IsChecked = _soundEnabled };
            var showGridBox = new CheckBox { Content = "Show Grid", IsChecked = _showGrid };
            var showStatusBarBox = new CheckBox { Content = "Show Status Bar", IsChecked = _showStatusBar };
            var showAreaBox = new CheckBox { Content = "Show Area Percentage", IsChecked = _showAreaPercentage };
            var originalModeBox = new CheckBox { Content = "Start in Original Mode", IsChecked = _originalMode };
            var errorText = new TextBlock
            {
                IsVisible = false,
                Foreground = Brushes.OrangeRed,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = new Window
            {
                Title = "Jezzball Settings",
                Width = 440,
                Height = 560,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            ApplyChildWindowAppearance(dialog);

            var restoreDefaultsButton = new Button { Content = "Restore Defaults", MinWidth = 120 };
            restoreDefaultsButton.Click += (_, _) =>
            {
                startingLivesBox.Text = "3";
                baseTimeBox.Text = "20";
                timePerLevelBox.Text = "5";
                captureRequirementBox.Text = "75";
                powerUpSpawnBox.Text = "30";
                fastModeBox.IsChecked = false;
                soundEnabledBox.IsChecked = true;
                showGridBox.IsChecked = false;
                showStatusBarBox.IsChecked = true;
                showAreaBox.IsChecked = true;
                originalModeBox.IsChecked = false;
                errorText.IsVisible = false;
                errorText.Text = string.Empty;
            };

            var cancelButton = new Button { Content = "Cancel", MinWidth = 90, IsCancel = true };
            cancelButton.Click += (_, _) => dialog.Close();

            var saveButton = new Button { Content = "Save", MinWidth = 90, IsDefault = true };
            saveButton.Click += (_, _) =>
            {
                if (!TryReadBoundedValue(startingLivesBox, "Starting lives", 1, 9, out var startingLives, out var errorMessage) ||
                    !TryReadBoundedValue(baseTimeBox, "Base level time", 10, 300, out var baseLevelTime, out errorMessage) ||
                    !TryReadBoundedValue(timePerLevelBox, "Time per level", 0, 120, out var timePerLevel, out errorMessage) ||
                    !TryReadBoundedValue(captureRequirementBox, "Capture requirement", 50, 95, out var captureRequirement, out errorMessage) ||
                    !TryReadBoundedValue(powerUpSpawnBox, "Power-up spawn chance", 0, 100, out var powerUpSpawn, out errorMessage))
                {
                    errorText.Text = errorMessage;
                    errorText.IsVisible = true;
                    return;
                }

                settings.StartingLives = startingLives;
                settings.BaseLevelTimeSeconds = baseLevelTime;
                settings.TimePerLevelSeconds = timePerLevel;
                settings.CaptureRequirementPercent = captureRequirement;
                settings.PowerUpSpawnPercent = powerUpSpawn;
                _fastMode = fastModeBox.IsChecked == true;
                _soundEnabled = soundEnabledBox.IsChecked == true;
                _showGrid = showGridBox.IsChecked == true;
                _showStatusBar = showStatusBarBox.IsChecked == true;
                _showAreaPercentage = showAreaBox.IsChecked == true;
                _originalMode = originalModeBox.IsChecked == true;

                SavePersistentSettings();
                ApplyTransientSettingsToControl();
                UpdateMenuItems();
                dialog.Close();
            };

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 10
            };
            contentPanel.Children.Add(new TextBlock
            {
                Text = "Rule changes apply on the next new game or level restart. Visual and sound options apply immediately.",
                TextWrapping = TextWrapping.Wrap
            });
            contentPanel.Children.Add(errorText);
            contentPanel.Children.Add(CreateSettingsRow("Starting lives", startingLivesBox));
            contentPanel.Children.Add(CreateSettingsRow("Base level time (seconds)", baseTimeBox));
            contentPanel.Children.Add(CreateSettingsRow("Extra seconds per level", timePerLevelBox));
            contentPanel.Children.Add(CreateSettingsRow("Capture target (%)", captureRequirementBox));
            contentPanel.Children.Add(CreateSettingsRow("Power-up spawn chance (%)", powerUpSpawnBox));
            contentPanel.Children.Add(new Separator());
            contentPanel.Children.Add(fastModeBox);
            contentPanel.Children.Add(soundEnabledBox);
            contentPanel.Children.Add(showGridBox);
            contentPanel.Children.Add(showStatusBarBox);
            contentPanel.Children.Add(showAreaBox);
            contentPanel.Children.Add(originalModeBox);
            contentPanel.Children.Add(new Separator());
            contentPanel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    restoreDefaultsButton,
                    cancelButton,
                    saveButton
                }
            });

            dialog.Content = new ScrollViewer { Content = contentPanel };
            await dialog.ShowDialog(_window);
        }

        private static TextBox CreateNumberBox(int value)
        {
            return new TextBox
            {
                Width = 90,
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = value.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static Control CreateSettingsRow(string label, Control control)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 12
            };

            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(control, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(control);
            return grid;
        }

        private static bool TryReadBoundedValue(TextBox source, string name, int min, int max, out int value, out string errorMessage)
        {
            if (!int.TryParse(source.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                errorMessage = $"{name} must be a whole number.";
                return false;
            }

            if (value < min || value > max)
            {
                errorMessage = $"{name} must be between {min} and {max}.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private void RecordHighScore(long score)
        {
            if (score <= 0)
            {
                return;
            }

            AddHighScore(score);
            SavePersistentSettings();
        }

        private void ApplyChildWindowAppearance(Window window)
        {
            ThemeManager.ApplyForPlugin(window, this);
            if (SettingsManager.Settings.PluginSkins.TryGetValue("Jezzball", out var skin) &&
                !string.IsNullOrWhiteSpace(skin))
            {
                SkinManager.ApplySkinTo(window, skin);
            }
        }

        private void ClearMenuReferences()
        {
            _pauseMenuItem = null;
            _normalSpeedMenuItem = null;
            _fastSpeedMenuItem = null;
            _soundMenuItem = null;
            _gridMenuItem = null;
            _originalModeMenuItem = null;
            _statusBarMenuItem = null;
            _areaPercentageMenuItem = null;
        }

        private static bool NormalizeJezzballSettings(JezzballSettings settings)
        {
            var changed = false;

            if (settings.HighScores == null)
            {
                settings.HighScores = new List<long>();
                changed = true;
            }

            if (settings.StartingLives < 1)
            {
                settings.StartingLives = 1;
                changed = true;
            }
            else if (settings.StartingLives > 9)
            {
                settings.StartingLives = 9;
                changed = true;
            }

            if (settings.BaseLevelTimeSeconds < 10)
            {
                settings.BaseLevelTimeSeconds = 10;
                changed = true;
            }
            else if (settings.BaseLevelTimeSeconds > 300)
            {
                settings.BaseLevelTimeSeconds = 300;
                changed = true;
            }

            if (settings.TimePerLevelSeconds < 0)
            {
                settings.TimePerLevelSeconds = 0;
                changed = true;
            }
            else if (settings.TimePerLevelSeconds > 120)
            {
                settings.TimePerLevelSeconds = 120;
                changed = true;
            }

            if (settings.CaptureRequirementPercent < 50)
            {
                settings.CaptureRequirementPercent = 50;
                changed = true;
            }
            else if (settings.CaptureRequirementPercent > 95)
            {
                settings.CaptureRequirementPercent = 95;
                changed = true;
            }

            if (settings.PowerUpSpawnPercent < 0)
            {
                settings.PowerUpSpawnPercent = 0;
                changed = true;
            }
            else if (settings.PowerUpSpawnPercent > 100)
            {
                settings.PowerUpSpawnPercent = 100;
                changed = true;
            }

            settings.HighScores = settings.HighScores
                .Where(score => score >= 0)
                .OrderByDescending(score => score)
                .Take(10)
                .ToList();

            return changed;
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

            // FIXED: Proper boundary detection like the Canvas version
            // Check if ball hits the walls and bounce properly
            if (Position.X - Radius <= bounds.Left && Velocity.X < 0)
            {
                Velocity = Velocity.WithX(-Velocity.X);
                Position = new Point(bounds.Left + Radius, Position.Y);
            }
            else if (Position.X + Radius >= bounds.Right && Velocity.X > 0)
            {
                Velocity = Velocity.WithX(-Velocity.X);
                Position = new Point(bounds.Right - Radius, Position.Y);
            }
            
            if (Position.Y - Radius <= bounds.Top && Velocity.Y < 0)
            {
                Velocity = Velocity.WithY(-Velocity.Y);
                Position = new Point(Position.X, bounds.Top + Radius);
            }
            else if (Position.Y + Radius >= bounds.Bottom && Velocity.Y > 0)
            {
                Velocity = Velocity.WithY(-Velocity.Y);
                Position = new Point(Position.X, bounds.Bottom - Radius);
            }

            // FIXED: Ensure ball stays within bounds (safety check)
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

        public void Draw(DrawingContext context, Rect bounds)
        {
            var scale = GetPulseScale();
            var radius = Radius * scale;
            
            // FIXED: Add ball rotation/spinning effect like the Canvas version
            var rotationAngle = _pulseTimer * 2.0; // Spin to the left (negative rotation)
            
            // Create a transform for rotation around the ball center
            var center = Position;
            var transform = Matrix.CreateRotation(rotationAngle, center);
            
            // Apply rotation transform using the correct Avalonia pattern
            using (context.PushTransform(transform))
            {
                // Draw ball with rotation
                if (Type == BallType.RedWhite)
                {
                    // Special rendering for red/white balls
                    var brush1 = new SolidColorBrush(Colors.Red);
                    var brush2 = new SolidColorBrush(Colors.White);
                    
                    // Draw alternating segments
                    for (int i = 0; i < 8; i++)
                    {
                        var angle = i * Math.PI / 4;
                        var brush = i % 2 == 0 ? brush1 : brush2;
                        var rect = new Rect(
                            center.X - radius + Math.Cos(angle) * radius * 0.3,
                            center.Y - radius + Math.Sin(angle) * radius * 0.3,
                            radius * 0.6,
                            radius * 0.6
                        );
                        context.DrawEllipse(brush, null, rect);
                    }
                }
                else
                {
                    // Regular ball rendering
                    var rect = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    context.DrawEllipse(Fill, null, rect);
                    
                    // Add a subtle highlight for 3D effect
                    var highlightBrush = new SolidColorBrush(Colors.White, 0.3);
                    var highlightRect = new Rect(center.X - radius * 0.6, center.Y - radius * 0.6, radius * 1.2, radius * 1.2);
                    context.DrawEllipse(highlightBrush, null, highlightRect);
                }
            }
            
            // Draw trail with rotation effect
            if (_trail.Count > 1)
            {
                // FIXED: Handle different brush types for trail color
                Color trailColor = Colors.Gray; // Default color
                if (Fill is SolidColorBrush solidBrush)
                {
                    trailColor = solidBrush.Color;
                }
                
                var trailBrush = new SolidColorBrush(trailColor, 0.5);
                var points = _trail.Select(p => new Point(p.X, p.Y)).ToArray();
                if (points.Length > 1)
                {
                    // Draw trail as connected lines
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        context.DrawLine(new Pen(trailBrush, 2), points[i], points[i + 1]);
                    }
                }
            }
        }
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
        public int CaptureRequirementPercent => GetCaptureRequirementPercent();
        public string Message { get; private set; } = string.Empty;
        public bool IsGameOver => Lives <= 0;
        public bool FlashEffect { get; set; }
        public bool HasIceWallPowerUp { get; private set; }
        public bool FreezeActive { get; private set; }
        public bool DoubleScoreActive { get; private set; }
        public bool ScreenShake { get; set; }
        public bool OriginalMode { get; set; }
        public Size GameSize => _gameSize;

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
        private readonly JezzballSettings _settings;
        private double _totalPlayArea;
        private JezzballTheme _theme;
        private double _freezeTimer;
        private double _doubleScoreTimer;
        private double _screenShakeTimer;
        private double _lastCaptureTime;
        private int _comboCount;
        private Size _gameSize = new(800, 570);

        private const double WallSpeed = 150.0;

        public JezzballGameState(JezzballTheme theme, JezzballSettings settings)
        {
            _theme = theme;
            _settings = settings;
            OriginalMode = settings.OriginalMode;
            StartLevel(_gameSize);
        }

        public void ApplyTheme(JezzballTheme theme)
        {
            _theme = theme;
            foreach (var b in _balls) b.ApplyTheme(_theme);
        }

        public void StartNewGame(Size gameSize)
        {
            Level = 1;
            Lives = GetStartingLives();
            Score = 0;
            StartLevel(gameSize);
        }

        public void StartLevel(Size gameSize)
        {
            _gameSize = GetPlayableSize(gameSize);
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
            OriginalMode = _settings.OriginalMode;
            var bounds = new Rect(0, 0, _gameSize.Width, _gameSize.Height);
            _totalPlayArea = bounds.Width * bounds.Height;
            _activeAreas.Add(bounds);
            TimeLeft = TimeSpan.FromSeconds(GetBaseLevelTimeSeconds() + Level * GetTimePerLevelSeconds());

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
                if (IsGameOver) StartNewGame(_gameSize);
                else StartLevel(_gameSize);
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
                var position = new Point(
                    _gameSize.Width * 0.5 + (rand.NextDouble() - 0.5) * 120,
                    _gameSize.Height * 0.5 + (rand.NextDouble() - 0.5) * 120);
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

            if (CapturedPercentage >= GetCaptureRequirementRatio())
            {
                Level++;
                long timeBonus = (long)TimeLeft.TotalSeconds * 100;
                long comboBonus = _comboCount * 500;
                Score += 1000 + timeBonus + comboBonus;
                JezzballSound.Play(JezzballSoundEvent.LevelComplete);
                Message = $"Well Done!\nTime Bonus: {timeBonus}\nCombo Bonus: {comboBonus}";
            }
        }

        private void MaybeSpawnPowerUp(Point location)
        {
            var rand = new Random();
            if (rand.NextDouble() < GetPowerUpSpawnPercent() / 100.0)
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

        private int GetStartingLives()
        {
            return Math.Clamp(_settings.StartingLives, 1, 9);
        }

        private int GetBaseLevelTimeSeconds()
        {
            return Math.Clamp(_settings.BaseLevelTimeSeconds, 10, 300);
        }

        private int GetTimePerLevelSeconds()
        {
            return Math.Clamp(_settings.TimePerLevelSeconds, 0, 120);
        }

        private int GetCaptureRequirementPercent()
        {
            return Math.Clamp(_settings.CaptureRequirementPercent, 50, 95);
        }

        private double GetCaptureRequirementRatio()
        {
            return GetCaptureRequirementPercent() / 100.0;
        }

        private int GetPowerUpSpawnPercent()
        {
            return Math.Clamp(_settings.PowerUpSpawnPercent, 0, 100);
        }

        private Size GetPlayableSize(Size requestedSize)
        {
            if (requestedSize.Width > 0 && requestedSize.Height > 0)
            {
                return requestedSize;
            }

            if (_gameSize.Width > 0 && _gameSize.Height > 0)
            {
                return _gameSize;
            }

            return new Size(800, 570);
        }
    }
    #endregion

    #region Game View (UI Control)

    internal class JezzballControl : UserControl, IDisposable
    {
        private readonly JezzballGameState _gameState;
        private readonly JezzballSettings _settings;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private readonly Action<long>? _recordHighScore;
        private Point _mousePosition;
        private WallOrientation _orientation = WallOrientation.Vertical;
        private bool _isPaused = false;
        private bool _fastMode = false;
        private bool _soundEnabled = true;
        private bool _showGrid = false;
        private bool _showStatusBar = true;
        private bool _showAreaPercentage = true;
        private bool _gameOverRecorded = false;
        private bool _layoutInitialized = false;

        private readonly GameCanvas _gameCanvas;
        private readonly StackPanel _statusPanel;
        
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
        private double _flashTimer = 0;
        private readonly DispatcherTimer _flashUpdateTimer;
        
        public Menu MenuBar => _menu;

        public JezzballControl(JezzballTheme theme, JezzballSettings settings, Action<long>? recordHighScore)
        {
            _settings = settings;
            _theme = theme;
            _recordHighScore = recordHighScore;
            _gameState = new JezzballGameState(theme, settings);
            _fastMode = settings.FastMode;
            _soundEnabled = settings.SoundEnabled;
            _showGrid = settings.ShowGrid;
            _showStatusBar = settings.ShowStatusBar;
            _showAreaPercentage = settings.ShowAreaPercentage;
            _gameCanvas = new GameCanvas(this);
            _statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(5)
            };
            _statusPanel.Children.AddRange(new Control[] { _levelText, _livesText, _scoreText, _timeText, _capturedText, _effectText });
            _flashUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16),
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
            JezzballSound.Enabled = _soundEnabled;
            _gameState.SetOriginalMode(settings.OriginalMode);

            var mainPanel = new Grid();
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            
            Grid.SetRow(_menu, 0);
            Grid.SetRow(_statusPanel, 1);
            Grid.SetRow(_gameCanvas, 2);
            mainPanel.Children.Add(_menu);
            mainPanel.Children.Add(_statusPanel);
            mainPanel.Children.Add(_gameCanvas);

            Content = mainPanel;
            
            _gameCanvas.PointerMoved += OnPointerMoved;
            _gameCanvas.PointerPressed += OnPointerPressed;
            SizeChanged += OnControlResized;

            SetGameSpeed(_fastMode);
            SetSoundEnabled(_soundEnabled);
            SetShowGrid(_showGrid);
            SetShowStatusBar(_showStatusBar);
            SetShowAreaPercentage(_showAreaPercentage);
            SetOriginalMode(settings.OriginalMode);
            StartNewGame();
        }

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
            _timer.Interval = fast ? TimeSpan.FromMilliseconds(8) : TimeSpan.FromMilliseconds(16);
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
            JezzballSound.Enabled = enabled;
        }

        public void SetShowGrid(bool show)
        {
            _showGrid = show;
            _gameCanvas.InvalidateVisual();
        }

        public void SetShowStatusBar(bool show)
        {
            _showStatusBar = show;
            _statusPanel.IsVisible = show;
        }

        public void SetShowAreaPercentage(bool show)
        {
            _showAreaPercentage = show;
            UpdateStatusText();
        }

        public void SetOriginalMode(bool originalMode)
        {
            _settings.OriginalMode = originalMode;
            _gameState.SetOriginalMode(originalMode);
            _gameCanvas.InvalidateVisual();
        }

        internal void SetSound(JezzballSoundEvent ev, string path)
        {
            _soundPaths[ev] = path;
            JezzballSound.Paths[ev] = path;
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
            _soundPaths.Clear();
            JezzballSound.Paths.Clear();
            if (SettingsManager.Settings.PluginSoundEffects.TryGetValue("Jezzball", out var sounds))
            {
                foreach (var kvp in sounds)
                {
                    if (Enum.TryParse<JezzballSoundEvent>(kvp.Key, out var ev))
                    {
                        _soundPaths[ev] = kvp.Value;
                        JezzballSound.Paths[ev] = kvp.Value;
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

        public void StartNewGame()
        {
            _gameOverRecorded = false;
            _gameState.StartNewGame(GetPlayableSize());
            UpdateStatusText();
            _gameCanvas.InvalidateVisual();
        }

        public void RestartGame()
        {
            _gameOverRecorded = false;
            _gameState.StartLevel(GetPlayableSize());
            UpdateStatusText();
            _gameCanvas.InvalidateVisual();
        }

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
            if (_layoutInitialized)
            {
                return;
            }

            var size = GetPlayableSize();
            if (size.Width <= 0 || size.Height <= 0)
            {
                return;
            }

            _layoutInitialized = true;
            _gameOverRecorded = false;
            _gameState.StartNewGame(size);
            UpdateStatusText();
            _gameCanvas.InvalidateVisual();
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var position = e.GetPosition(_gameCanvas);
            _mousePosition = new Point(position.X, position.Y);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isPaused) return;

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

                if (_gameState.IsGameOver && !_gameOverRecorded)
                {
                    _gameOverRecorded = true;
                    _recordHighScore?.Invoke(_gameState.Score);
                }
                else if (!_gameState.IsGameOver)
                {
                    _gameOverRecorded = false;
                }
                
                if (_gameState.FlashEffect && _flashTimer <= 0)
                {
                    _flashTimer = 0.2;
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
                _capturedText.Text = $"Area: {_gameState.CapturedPercentage:P0} / {_gameState.CaptureRequirementPercent}%";
            }
            else
            {
                _capturedText.Text = string.Empty;
            }
            
            var effects = new List<string>();
            if (_gameState.FreezeActive) effects.Add("FREEZE");
            if (_gameState.DoubleScoreActive) effects.Add("2X SCORE");
            if (_gameState.HasIceWallPowerUp) effects.Add("ICE WALL");
            if (_gameState.OriginalMode) effects.Add("ORIGINAL");
            _effectText.Text = string.Join(" | ", effects);
        }

        private Size GetPlayableSize()
        {
            if (_gameCanvas.Bounds.Width > 0 && _gameCanvas.Bounds.Height > 0)
            {
                return _gameCanvas.Bounds.Size;
            }

            if (_gameState.GameSize.Width > 0 && _gameState.GameSize.Height > 0)
            {
                return _gameState.GameSize;
            }

            return new Size(800, 570);
        }

        internal void RenderGame(DrawingContext context)
        {
            var bounds = _gameCanvas.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var transform = Matrix.Identity;
            IDisposable? shakeState = null;
            if (_gameState.ScreenShake)
            {
                var rand = new Random();
                var shakeOffset = new Point(
                    (rand.NextDouble() - 0.5) * 4,
                    (rand.NextDouble() - 0.5) * 4
                );
                transform = Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y);
                shakeState = context.PushTransform(transform);
            }

            try
            {
                if (_gameState.OriginalMode)
                {
                    context.FillRectangle(Brushes.Black, bounds);
                }
                else
                {
                    context.FillRectangle(_backgroundBrush, bounds);
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

                // FIXED: Draw balls with rotation and effects using the new Draw method
                foreach (var ball in _gameState.Balls)
                {
                    ball.Draw(context, bounds);
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

                // Draw preview line - FIXED: Always show cursor/crosshair when not building a wall
                if (_gameState.CurrentWall == null && string.IsNullOrEmpty(_gameState.Message))
                {
                    Pen previewPen = _gameState.OriginalMode ?
                        new Pen(new SolidColorBrush(Colors.Blue), 2, DashStyle.Dash) :
                        _previewPen;

                    // FIXED: Ensure cursor is always visible with multiple layers
                    var cursorPen = new Pen(new SolidColorBrush(Colors.Yellow), 3, DashStyle.Dash);
                    var cursorGlowPen = new Pen(new SolidColorBrush(Colors.Orange), 5, DashStyle.Dash);
                    
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
                            // Draw glow first
                            context.DrawLine(glowPreviewPen, top, bottom);
                            // Draw main preview line
                            context.DrawLine(previewPen, top, bottom);
                            // Draw prominent cursor line with glow
                            context.DrawLine(cursorGlowPen, top, bottom);
                            context.DrawLine(cursorPen, top, bottom);
                        }
                        else
                        {
                            var left = new Point(bounds.Left, start.Y);
                            var right = new Point(bounds.Right, start.Y);
                            // Draw glow first
                            context.DrawLine(glowPreviewPen, left, right);
                            // Draw main preview line
                            context.DrawLine(previewPen, left, right);
                            // Draw prominent cursor line with glow
                            context.DrawLine(cursorGlowPen, left, right);
                            context.DrawLine(cursorPen, left, right);
                        }
                    }
                }

                // FIXED: Much more subtle flash effect like the Canvas version
                if (_gameState.FlashEffect)
                {
                    // Use a very subtle flash with better timing
                    var flashOpacity = Math.Sin(_flashTimer * Math.PI * 6) * 0.15 + 0.05; // Much more subtle
                    var flashBrush = new SolidColorBrush(Colors.White, flashOpacity);
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
                shakeState?.Dispose();
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

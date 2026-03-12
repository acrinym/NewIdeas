using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Effects;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public sealed class TileWorldPlugin : IPlugin
    {
        private PluginWindowBase? _window;
        private TileWorldControl? _control;

        public string Name => "Tile World";
        public string Description => "Chip's Challenge style puzzle boards with keys, blocks, hazards, and exit runs.";
        public Version Version => new(0, 1, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _control = new TileWorldControl();

            _window = new PluginWindowBase
            {
                Title = "Tile World",
                Width = 880,
                Height = 720,
                MinWidth = 720,
                MinHeight = 560,
                Content = _control,
                Plugin = this
            };
            _window.ApplyPluginThemeAndSkin(this);
            _window.KeyDown += OnWindowKeyDown;
            _window.Opened += OnWindowOpened;
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TileWorldPlugin));
            _window.Show();
        }

        public void Stop()
        {
            if (_window != null)
            {
                _window.KeyDown -= OnWindowKeyDown;
                _window.Opened -= OnWindowOpened;
                (_window.Content as IDisposable)?.Dispose();
                _window.Close();
            }

            _window = null;
            _control = null;
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            _control?.FocusBoard();
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (_control == null)
            {
                return;
            }

            if (e.Key == Key.F1)
            {
                _control.ShowHelp();
                e.Handled = true;
                return;
            }

            if (_control.HandleKey(e.Key))
            {
                e.Handled = true;
            }
        }
    }

    internal enum TileWorldTile
    {
        Floor,
        Wall,
        Chip,
        Exit,
        Socket,
        KeyBlue,
        KeyRed,
        KeyYellow,
        KeyGreen,
        DoorBlue,
        DoorRed,
        DoorYellow,
        DoorGreen,
        Block,
        Water,
        Fire,
        BootsWater,
        BootsFire,
        Hint
    }

    internal readonly record struct TileWorldPoint(int X, int Y);

    internal sealed class TileWorldLevel
    {
        public TileWorldLevel(string name, string hint, params string[] rows)
        {
            Name = name;
            Hint = hint;
            Rows = rows;
        }

        public TileWorldLevel(string name, string hint, TileWorldTile[,] tiles, TileWorldPoint playerStart)
        {
            Name = name;
            Hint = hint;
            Tiles = tiles;
            PlayerStart = playerStart;
            Rows = Array.Empty<string>();
        }

        public string Name { get; }

        public string Hint { get; }

        public IReadOnlyList<string> Rows { get; }

        public TileWorldTile[,]? Tiles { get; }

        public TileWorldPoint? PlayerStart { get; }
    }

    internal static class TileWorldLevels
    {
        public static IReadOnlyList<TileWorldLevel> CreateDefaultSet()
        {
            return new[]
            {
                new TileWorldLevel(
                    "Training Yard",
                    "Collect every chip, grab the key, then open the exit run.",
                    "############",
                    "#@...c....e#",
                    "#.####.###.#",
                    "#....#...#.#",
                    "#.c..#.k.#.#",
                    "#....#...#.#",
                    "#.####d###.#",
                    "#....c.....#",
                    "############"),
                new TileWorldLevel(
                    "Canal Locks",
                    "Use the block to bridge water before you take the last chip.",
                    "##############",
                    "#@...c....e..#",
                    "#.#######.##.#",
                    "#....w....##.#",
                    "#.b..w..c....#",
                    "#....w....##.#",
                    "#.#######.##.#",
                    "#....k....d..#",
                    "##############"),
                new TileWorldLevel(
                    "Fire Hall",
                    "Do not rush the fire lane. Clear chips first and route around the hazard.",
                    "##############",
                    "#@..c....##e.#",
                    "#.####.#.##..#",
                    "#......#.##..#",
                    "#.####.#.##..#",
                    "#.c..#.#..f..#",
                    "#.##.#.#######",
                    "#.k..d.......#",
                    "##############")
            };
        }
    }

    internal sealed class TileWorldState
    {
        private IReadOnlyList<TileWorldLevel> _levels;
        private TileWorldTile[,] _tiles = new TileWorldTile[1, 1];
        private TileWorldPoint _player;

        public TileWorldState(IReadOnlyList<TileWorldLevel> levels)
        {
            _levels = levels;
            LoadLevel(0);
        }

        public int LevelIndex { get; private set; }

        public TileWorldLevel CurrentLevel => _levels[LevelIndex];

        public int Width => _tiles.GetLength(0);

        public int Height => _tiles.GetLength(1);

        public int ChipsRemaining { get; private set; }

        public int BlueKeys { get; private set; }

        public int RedKeys { get; private set; }

        public int YellowKeys { get; private set; }

        public int GreenKeys { get; private set; }

        public int MoveCount { get; private set; }

        public int AttemptCount { get; private set; }

        public bool LevelComplete { get; private set; }

        public bool HasWaterBoots { get; private set; }

        public bool HasFireBoots { get; private set; }

        public string StatusMessage { get; private set; } = string.Empty;

        public bool CanGoPrevious => LevelIndex > 0;

        public bool CanGoNext => LevelComplete && LevelIndex < _levels.Count - 1;

        public TileWorldPoint Player => _player;

        public TileWorldTile GetTile(int x, int y) => _tiles[x, y];

        public void SetLevels(IReadOnlyList<TileWorldLevel> levels, int startIndex = 0, string? statusOverride = null)
        {
            _levels = levels;
            LoadLevel(startIndex, statusOverride);
        }

        public void RestartLevel()
        {
            AttemptCount++;
            LoadLevel(LevelIndex, "Level restarted.");
        }

        public bool LoadPreviousLevel()
        {
            if (!CanGoPrevious)
            {
                return false;
            }

            LoadLevel(LevelIndex - 1, "Loaded previous board.");
            return true;
        }

        public bool LoadNextLevel()
        {
            if (!CanGoNext)
            {
                return false;
            }

            LoadLevel(LevelIndex + 1);
            return true;
        }

        public bool TryMove(int dx, int dy)
        {
            if (LevelComplete)
            {
                StatusMessage = LevelIndex == _levels.Count - 1
                    ? "Board set clear."
                    : "Board clear. Press N or use Next to continue.";
                return false;
            }

            var target = new TileWorldPoint(_player.X + dx, _player.Y + dy);
            if (!IsInside(target))
            {
                return false;
            }

            var targetTile = GetTile(target.X, target.Y);
            if (targetTile == TileWorldTile.Wall)
            {
                StatusMessage = "Solid wall.";
                return false;
            }

            if (IsDoorTile(targetTile))
            {
                if (!TryOpenDoor(target, targetTile))
                {
                    return false;
                }
            }

            if (targetTile == TileWorldTile.Block)
            {
                if (!TryPushBlock(target, dx, dy))
                {
                    StatusMessage = "The block will not move.";
                    return false;
                }

                targetTile = TileWorldTile.Floor;
            }

            if ((targetTile == TileWorldTile.Exit || targetTile == TileWorldTile.Socket) && ChipsRemaining > 0)
            {
                StatusMessage = targetTile == TileWorldTile.Socket
                    ? $"Socket locked. Need {ChipsRemaining} more chip{(ChipsRemaining == 1 ? string.Empty : "s")}."
                    : $"Need {ChipsRemaining} more chip{(ChipsRemaining == 1 ? string.Empty : "s")}.";
                return false;
            }

            MoveCount++;

            if (targetTile == TileWorldTile.Water && !HasWaterBoots)
            {
                AttemptCount++;
                LoadLevel(LevelIndex, "Splash. The board reset.");
                return true;
            }

            if (targetTile == TileWorldTile.Fire && !HasFireBoots)
            {
                AttemptCount++;
                LoadLevel(LevelIndex, "Fire tile. The board reset.");
                return true;
            }

            _player = target;

            if (targetTile == TileWorldTile.Chip)
            {
                ChipsRemaining--;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = ChipsRemaining == 0
                    ? "Exit unlocked."
                    : $"{ChipsRemaining} chip{(ChipsRemaining == 1 ? string.Empty : "s")} left.";
            }
            else if (targetTile == TileWorldTile.KeyBlue)
            {
                BlueKeys++;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Blue key collected.";
            }
            else if (targetTile == TileWorldTile.KeyRed)
            {
                RedKeys++;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Red key collected.";
            }
            else if (targetTile == TileWorldTile.KeyYellow)
            {
                YellowKeys++;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Yellow key collected.";
            }
            else if (targetTile == TileWorldTile.KeyGreen)
            {
                GreenKeys++;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Green key collected.";
            }
            else if (targetTile == TileWorldTile.BootsWater)
            {
                HasWaterBoots = true;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Flippers collected.";
            }
            else if (targetTile == TileWorldTile.BootsFire)
            {
                HasFireBoots = true;
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Fire boots collected.";
            }
            else if (targetTile == TileWorldTile.Socket)
            {
                SetTile(target.X, target.Y, TileWorldTile.Floor);
                StatusMessage = "Socket opened.";
            }
            else if (targetTile == TileWorldTile.Exit)
            {
                LevelComplete = true;
                StatusMessage = LevelIndex == _levels.Count - 1
                    ? "Board set clear."
                    : "Board clear. Press N or use Next to continue.";
            }
            else if (targetTile == TileWorldTile.Hint)
            {
                StatusMessage = CurrentLevel.Hint;
            }
            else
            {
                StatusMessage = CurrentLevel.Hint;
            }

            return true;
        }

        private void LoadLevel(int index, string? statusOverride = null)
        {
            LevelIndex = index;
            BlueKeys = 0;
            RedKeys = 0;
            YellowKeys = 0;
            GreenKeys = 0;
            MoveCount = 0;
            LevelComplete = false;
            HasWaterBoots = false;
            HasFireBoots = false;

            var level = _levels[index];
            ChipsRemaining = 0;
            _player = new TileWorldPoint(1, 1);

            if (level.Tiles != null)
            {
                var width = level.Tiles.GetLength(0);
                var height = level.Tiles.GetLength(1);
                _tiles = new TileWorldTile[width, height];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var tile = level.Tiles[x, y];
                        _tiles[x, y] = tile;
                        if (tile == TileWorldTile.Chip)
                        {
                            ChipsRemaining++;
                        }
                    }
                }

                if (level.PlayerStart != null)
                {
                    _player = level.PlayerStart.Value;
                }
            }
            else
            {
                var width = 0;
                foreach (var row in level.Rows)
                {
                    if (row.Length > width)
                    {
                        width = row.Length;
                    }
                }

                var height = level.Rows.Count;
                _tiles = new TileWorldTile[width, height];

                for (var y = 0; y < height; y++)
                {
                    var row = level.Rows[y];
                    for (var x = 0; x < width; x++)
                    {
                        var symbol = x < row.Length ? row[x] : '#';
                        var tile = symbol switch
                        {
                            '#' => TileWorldTile.Wall,
                            '.' => TileWorldTile.Floor,
                            'c' => TileWorldTile.Chip,
                            'e' => TileWorldTile.Exit,
                            'o' => TileWorldTile.Socket,
                            'k' => TileWorldTile.KeyBlue,
                            'r' => TileWorldTile.KeyRed,
                            'y' => TileWorldTile.KeyYellow,
                            'g' => TileWorldTile.KeyGreen,
                            'd' => TileWorldTile.DoorBlue,
                            'R' => TileWorldTile.DoorRed,
                            'Y' => TileWorldTile.DoorYellow,
                            'G' => TileWorldTile.DoorGreen,
                            'b' => TileWorldTile.Block,
                            'w' => TileWorldTile.Water,
                            'f' => TileWorldTile.Fire,
                            'u' => TileWorldTile.BootsFire,
                            'p' => TileWorldTile.BootsWater,
                            'h' => TileWorldTile.Hint,
                            '@' => TileWorldTile.Floor,
                            _ => TileWorldTile.Floor
                        };

                        _tiles[x, y] = tile;
                        if (symbol == '@')
                        {
                            _player = new TileWorldPoint(x, y);
                        }

                        if (tile == TileWorldTile.Chip)
                        {
                            ChipsRemaining++;
                        }
                    }
                }
            }

            StatusMessage = statusOverride ?? level.Hint;
        }

        private static bool IsDoorTile(TileWorldTile tile)
        {
            return tile == TileWorldTile.DoorBlue
                || tile == TileWorldTile.DoorRed
                || tile == TileWorldTile.DoorYellow
                || tile == TileWorldTile.DoorGreen;
        }

        private bool TryOpenDoor(TileWorldPoint target, TileWorldTile tile)
        {
            switch (tile)
            {
                case TileWorldTile.DoorBlue:
                    if (BlueKeys <= 0)
                    {
                        StatusMessage = "Blue door is locked.";
                        return false;
                    }

                    BlueKeys--;
                    break;
                case TileWorldTile.DoorRed:
                    if (RedKeys <= 0)
                    {
                        StatusMessage = "Red door is locked.";
                        return false;
                    }

                    RedKeys--;
                    break;
                case TileWorldTile.DoorYellow:
                    if (YellowKeys <= 0)
                    {
                        StatusMessage = "Yellow door is locked.";
                        return false;
                    }

                    YellowKeys--;
                    break;
                case TileWorldTile.DoorGreen:
                    if (GreenKeys <= 0)
                    {
                        StatusMessage = "Green door is locked.";
                        return false;
                    }

                    GreenKeys--;
                    break;
                default:
                    return true;
            }

            SetTile(target.X, target.Y, TileWorldTile.Floor);
            StatusMessage = "Door opened.";
            return true;
        }

        private bool TryPushBlock(TileWorldPoint blockPosition, int dx, int dy)
        {
            var pushTarget = new TileWorldPoint(blockPosition.X + dx, blockPosition.Y + dy);
            if (!IsInside(pushTarget))
            {
                return false;
            }

            var tileAhead = GetTile(pushTarget.X, pushTarget.Y);
            if (tileAhead == TileWorldTile.Floor)
            {
                SetTile(pushTarget.X, pushTarget.Y, TileWorldTile.Block);
                SetTile(blockPosition.X, blockPosition.Y, TileWorldTile.Floor);
                StatusMessage = "Block pushed.";
                return true;
            }

            if (tileAhead == TileWorldTile.Water)
            {
                SetTile(pushTarget.X, pushTarget.Y, TileWorldTile.Floor);
                SetTile(blockPosition.X, blockPosition.Y, TileWorldTile.Floor);
                StatusMessage = "Block bridged the canal.";
                return true;
            }

            return false;
        }

        private bool IsInside(TileWorldPoint point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height;
        }

        private void SetTile(int x, int y, TileWorldTile tile)
        {
            _tiles[x, y] = tile;
        }
    }

    internal sealed class TileWorldControl : UserControl, IDisposable
    {
        private const string SuggestedLibraryRoot = @"C:\Users\User\Downloads\TWorld\tworld-1.3.2-CCLPs.tar\tworld-1.3.2-CCLPs\tworld-1.3.2";

        private readonly TileWorldState _state;
        private readonly TileWorldBoard _board;
        private readonly IReadOnlyList<TileWorldLevel> _builtInLevels;
        private readonly TextBlock _levelText;
        private readonly TextBlock _chipsText;
        private readonly TextBlock _keysText;
        private readonly TextBlock _movesText;
        private readonly TextBlock _bootsText;
        private readonly TextBlock _statusText;
        private readonly TextBlock _hintText;
        private readonly Button _previousButton;
        private readonly Button _restartButton;
        private readonly Button _nextButton;
        private readonly Button _helpButton;
        private readonly Button _restoreBuiltInButton;
        private readonly TextBox _libraryPathTextBox;
        private readonly TextBlock _libraryStatusText;
        private readonly ListBox _packListBox;
        private readonly ListBox _levelListBox;
        private readonly TextBlock _importDetailsText;
        private readonly Button _scanLibraryButton;
        private readonly Button _playImportedButton;
        private IReadOnlyList<TileWorldImportedPack> _importedPacks = Array.Empty<TileWorldImportedPack>();
        private TileWorldImportedLevel? _selectedImportedLevel;

        public TileWorldControl()
        {
            _builtInLevels = TileWorldLevels.CreateDefaultSet();
            _state = new TileWorldState(_builtInLevels);
            _board = new TileWorldBoard(this);
            _board.Focusable = false;

            _levelText = new TextBlock();
            _chipsText = new TextBlock();
            _keysText = new TextBlock();
            _movesText = new TextBlock();
            _bootsText = new TextBlock();
            _statusText = new TextBlock { TextWrapping = TextWrapping.Wrap };
            _hintText = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            AppearanceHelper.ApplySecondaryText(_hintText);
            AppearanceHelper.ApplySecondaryText(_statusText);

            _previousButton = new Button { Content = "Prev Board", MinWidth = 96 };
            _restartButton = new Button { Content = "Restart", MinWidth = 96 };
            _nextButton = new Button { Content = "Next Board", MinWidth = 96 };
            _helpButton = new Button { Content = "How To Play", MinWidth = 112 };
            _restoreBuiltInButton = new Button { Content = "Built-In Set", MinWidth = 104 };
            _libraryPathTextBox = new TextBox
            {
                Text = ResolveInitialLibraryPath(),
                Watermark = @"C:\path\to\tworld",
                MinWidth = 220
            };
            _libraryStatusText = new TextBlock { TextWrapping = TextWrapping.Wrap };
            _packListBox = new ListBox { MinHeight = 120 };
            _levelListBox = new ListBox { MinHeight = 180 };
            _importDetailsText = new TextBlock { TextWrapping = TextWrapping.Wrap };
            _scanLibraryButton = new Button { Content = "Scan Packs", MinWidth = 90 };
            _playImportedButton = new Button { Content = "Play Imported Level", MinWidth = 144, IsEnabled = false };

            AppearanceHelper.ApplyButtonRole(_previousButton, SemanticButtonRole.Neutral);
            AppearanceHelper.ApplyButtonRole(_restartButton, SemanticButtonRole.Warning);
            AppearanceHelper.ApplyButtonRole(_nextButton, SemanticButtonRole.Success);
            AppearanceHelper.ApplyButtonRole(_helpButton, SemanticButtonRole.Accent);
            AppearanceHelper.ApplyButtonRole(_restoreBuiltInButton, SemanticButtonRole.Neutral);
            AppearanceHelper.ApplyButtonRole(_scanLibraryButton, SemanticButtonRole.Accent);
            AppearanceHelper.ApplyButtonRole(_playImportedButton, SemanticButtonRole.Success);
            AppearanceHelper.ApplySecondaryText(_libraryStatusText);
            AppearanceHelper.ApplySecondaryText(_importDetailsText);

            _previousButton.Click += (_, _) =>
            {
                if (_state.LoadPreviousLevel())
                {
                    RefreshUi();
                }
            };
            _restartButton.Click += (_, _) =>
            {
                _state.RestartLevel();
                RefreshUi();
            };
            _nextButton.Click += (_, _) =>
            {
                if (_state.LoadNextLevel())
                {
                    RefreshUi();
                }
            };
            _helpButton.Click += (_, _) => ShowHelp();
            _restoreBuiltInButton.Click += (_, _) =>
            {
                _state.SetLevels(_builtInLevels, 0, "Loaded built-in Cycloside boards.");
                RefreshUi();
            };
            _scanLibraryButton.Click += (_, _) => ScanLibrary();
            _playImportedButton.Click += (_, _) => PlaySelectedImportedLevel();
            _packListBox.SelectionChanged += (_, _) => OnPackSelectionChanged();
            _levelListBox.SelectionChanged += (_, _) => OnImportedLevelSelectionChanged();

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    _previousButton,
                    _restartButton,
                    _nextButton,
                    _restoreBuiltInButton,
                    _helpButton
                }
            };

            var statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 18,
                Margin = new Thickness(0, 8, 0, 0),
                Children =
                {
                    _levelText,
                    _chipsText,
                    _keysText,
                    _movesText,
                    _bootsText
                }
            };

            var headerPanel = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    toolbar,
                    statsPanel,
                    _statusText,
                    _hintText
                }
            };

            var headerCard = new Border
            {
                Padding = new Thickness(14),
                Margin = new Thickness(12, 12, 12, 8),
                Child = headerPanel
            };
            AppearanceHelper.ApplyCardSurface(headerCard);

            var boardCard = new Border
            {
                Margin = new Thickness(12, 0, 12, 12),
                Padding = new Thickness(14),
                Child = BuildBoardWorkspace()
            };
            AppearanceHelper.ApplyCardSurface(boardCard);

            var root = new DockPanel();
            DockPanel.SetDock(headerCard, Dock.Top);
            root.Children.Add(headerCard);
            root.Children.Add(boardCard);

            Content = root;

            AttachedToVisualTree += OnAttachedToVisualTree;
            RefreshUi();

            if (!string.IsNullOrWhiteSpace(_libraryPathTextBox.Text))
            {
                ScanLibrary();
            }
        }

        private Control BuildBoardWorkspace()
        {
            var browserIntro = new TextBlock
            {
                Text = "External packs are read from a local Tile World library. Cycloside imports DAT and DAC metadata, then only enables native play for boards that fit the current rules subset.",
                TextWrapping = TextWrapping.Wrap
            };
            AppearanceHelper.ApplySecondaryText(browserIntro);

            var pathRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    _libraryPathTextBox,
                    _scanLibraryButton
                }
            };

            var importPanel = new StackPanel
            {
                Spacing = 8,
                Width = 320,
                Children =
                {
                    new TextBlock { Text = "Local Pack Browser" },
                    browserIntro,
                    pathRow,
                    _libraryStatusText,
                    new TextBlock { Text = "Packs" },
                    _packListBox,
                    new TextBlock { Text = "Levels" },
                    _levelListBox,
                    _playImportedButton,
                    _importDetailsText
                }
            };

            var workspace = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            workspace.Children.Add(_board);
            workspace.Children.Add(importPanel);
            Grid.SetColumn(_board, 0);
            Grid.SetColumn(importPanel, 1);

            return workspace;
        }

        private string ResolveInitialLibraryPath()
        {
            var configuredPath = SettingsManager.Settings.TileWorldLibraryPath;
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            return Directory.Exists(SuggestedLibraryRoot) ? SuggestedLibraryRoot : string.Empty;
        }

        private void ScanLibrary()
        {
            var rootPath = _libraryPathTextBox.Text?.Trim() ?? string.Empty;
            _importedPacks = TileWorldPackLibrary.ScanLibrary(rootPath, out var statusMessage);
            _libraryStatusText.Text = statusMessage;
            _packListBox.ItemsSource = _importedPacks;
            _levelListBox.ItemsSource = null;
            _selectedImportedLevel = null;
            _playImportedButton.IsEnabled = false;
            _importDetailsText.Text = "Select a pack to inspect imported levels.";

            SettingsManager.Settings.TileWorldLibraryPath = rootPath;
            SettingsManager.SaveSoon();
        }

        private void OnPackSelectionChanged()
        {
            if (_packListBox.SelectedItem is not TileWorldImportedPack pack)
            {
                _levelListBox.ItemsSource = null;
                _selectedImportedLevel = null;
                _playImportedButton.IsEnabled = false;
                _importDetailsText.Text = "Select a pack to inspect imported levels.";
                return;
            }

            _levelListBox.ItemsSource = pack.Levels;
            _levelListBox.SelectedItem = null;
            _selectedImportedLevel = null;
            _playImportedButton.IsEnabled = false;
            var lastLevelText = pack.LastLevel > 0 ? pack.LastLevel.ToString(CultureInfo.InvariantCulture) : "Unknown";
            var configPathText = string.IsNullOrWhiteSpace(pack.ConfigFilePath) ? "Direct DAT scan" : pack.ConfigFilePath;
            _importDetailsText.Text = $"{pack.DisplayName}\nRuleset: {pack.Ruleset}\nLevels: {pack.Levels.Count}\nNative Play: {pack.NativePlayableCount}/{pack.Levels.Count}\nLast Level: {lastLevelText}\nData: {pack.DataFilePath}\nConfig: {configPathText}";
        }

        private void OnImportedLevelSelectionChanged()
        {
            if (_levelListBox.SelectedItem is not TileWorldImportedLevel level)
            {
                _selectedImportedLevel = null;
                _playImportedButton.IsEnabled = false;
                return;
            }

            _selectedImportedLevel = level;
            _playImportedButton.IsEnabled = level.CanPlayNatively && level.NativeLevel != null;

            var details = $"Level {level.Number}: {level.Name}\n" +
                $"Time: {level.TimeLimitSeconds}s\n" +
                $"Password: {(string.IsNullOrWhiteSpace(level.Password) ? "None" : level.Password)}\n" +
                $"Hint: {(string.IsNullOrWhiteSpace(level.Hint) ? "None" : level.Hint)}\n" +
                $"Native Play: {(level.CanPlayNatively ? "Yes" : "No")}";

            if (!level.CanPlayNatively && level.UnsupportedTiles.Count > 0)
            {
                details += "\nUnsupported: " + string.Join(", ", level.UnsupportedTiles);
            }

            _importDetailsText.Text = details;
        }

        private void PlaySelectedImportedLevel()
        {
            if (_selectedImportedLevel?.NativeLevel == null)
            {
                return;
            }

            _state.SetLevels(new[] { _selectedImportedLevel.NativeLevel }, 0, $"Loaded imported level {_selectedImportedLevel.Number}: {_selectedImportedLevel.Name}");
            RefreshUi();
        }

        public void Dispose()
        {
            AttachedToVisualTree -= OnAttachedToVisualTree;
        }

        public void FocusBoard()
        {
            Focus();
        }

        public bool HandleKey(Key key)
        {
            var handled = key switch
            {
                Key.Up => _state.TryMove(0, -1),
                Key.Down => _state.TryMove(0, 1),
                Key.Left => _state.TryMove(-1, 0),
                Key.Right => _state.TryMove(1, 0),
                Key.R => RestartLevel(),
                Key.N => _state.LoadNextLevel(),
                Key.PageUp => _state.LoadPreviousLevel(),
                _ => false
            };

            if (handled)
            {
                RefreshUi();
            }

            return handled;
        }

        public void ShowHelp()
        {
            var helpText = "Arrow keys move the runner around the board.\n\n" +
                "Collect every chip before using the exit tile.\n\n" +
                "Colored keys open matching doors, and sockets only open after every chip is collected.\n\n" +
                "Blocks can be pushed into empty floor and can bridge water.\n\n" +
                "Water and fire reset the board unless you collected the matching boots.\n\n" +
                "Imported pack support currently covers walls, chips, exits, sockets, colored keys and doors, blocks, water, fire, flippers, fire boots, and hint tiles.\n\n" +
                "The pack browser reads local Tile World DAT and DAC files, then enables native play only for compatible imported levels.\n\n" +
                "Hotkeys: R restarts, N advances after a clear, Page Up goes back, F1 opens this help.";

            var helpWindow = new Window
            {
                Title = "Tile World Help",
                Width = 440,
                Height = 360,
                Content = new Border
                {
                    Padding = new Thickness(18),
                    Child = new TextBlock
                    {
                        Text = helpText,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
            helpWindow.Show();
        }

        internal TileWorldState State => _state;

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Dispatcher.UIThread.Post(() => Focus());
        }

        private bool RestartLevel()
        {
            _state.RestartLevel();
            return true;
        }

        private void RefreshUi()
        {
            _levelText.Text = $"Board {_state.LevelIndex + 1}: {_state.CurrentLevel.Name}";
            _chipsText.Text = $"Chips: {_state.ChipsRemaining}";
            _keysText.Text = $"Keys B:{_state.BlueKeys} R:{_state.RedKeys} Y:{_state.YellowKeys} G:{_state.GreenKeys}";
            _movesText.Text = $"Moves: {_state.MoveCount}";
            _bootsText.Text = $"Boots: {BuildBootsText()}";
            _statusText.Text = _state.StatusMessage;
            _hintText.Text = $"Hint: {_state.CurrentLevel.Hint}";
            _previousButton.IsEnabled = _state.CanGoPrevious;
            _nextButton.IsEnabled = _state.CanGoNext;
            _board.InvalidateVisual();
        }

        private string BuildBootsText()
        {
            var boots = new List<string>();
            if (_state.HasWaterBoots)
            {
                boots.Add("Water");
            }

            if (_state.HasFireBoots)
            {
                boots.Add("Fire");
            }

            return boots.Count == 0 ? "None" : string.Join("/", boots);
        }
    }

    internal sealed class TileWorldBoard : Control
    {
        private static readonly SolidColorBrush VoidBrush = new(Color.FromRgb(7, 10, 15));
        private static readonly SolidColorBrush FloorBrush = new(Color.FromRgb(22, 26, 33));
        private static readonly SolidColorBrush WallBrush = new(Color.FromRgb(53, 77, 112));
        private static readonly SolidColorBrush ChipBrush = new(Color.FromRgb(241, 196, 15));
        private static readonly SolidColorBrush ExitClosedBrush = new(Color.FromRgb(101, 67, 33));
        private static readonly SolidColorBrush ExitOpenBrush = new(Color.FromRgb(46, 204, 113));
        private static readonly SolidColorBrush KeyBlueBrush = new(Color.FromRgb(88, 166, 255));
        private static readonly SolidColorBrush KeyRedBrush = new(Color.FromRgb(239, 68, 68));
        private static readonly SolidColorBrush KeyYellowBrush = new(Color.FromRgb(250, 204, 21));
        private static readonly SolidColorBrush KeyGreenBrush = new(Color.FromRgb(74, 222, 128));
        private static readonly SolidColorBrush DoorBrush = new(Color.FromRgb(47, 98, 180));
        private static readonly SolidColorBrush DoorRedBrush = new(Color.FromRgb(127, 29, 29));
        private static readonly SolidColorBrush DoorYellowBrush = new(Color.FromRgb(133, 77, 14));
        private static readonly SolidColorBrush DoorGreenBrush = new(Color.FromRgb(21, 128, 61));
        private static readonly SolidColorBrush SocketBrush = new(Color.FromRgb(99, 102, 241));
        private static readonly SolidColorBrush BlockBrush = new(Color.FromRgb(144, 148, 151));
        private static readonly SolidColorBrush WaterBrush = new(Color.FromRgb(0, 119, 182));
        private static readonly SolidColorBrush FireBrush = new(Color.FromRgb(244, 114, 37));
        private static readonly SolidColorBrush BootsWaterBrush = new(Color.FromRgb(96, 165, 250));
        private static readonly SolidColorBrush BootsFireBrush = new(Color.FromRgb(251, 146, 60));
        private static readonly SolidColorBrush HintBrush = new(Color.FromRgb(168, 85, 247));
        private static readonly SolidColorBrush PlayerBrush = new(Color.FromRgb(255, 246, 143));
        private static readonly SolidColorBrush PlayerAccentBrush = new(Color.FromRgb(255, 255, 255));
        private static readonly SolidColorBrush GridBrush = new(Color.FromArgb(80, 255, 255, 255));
        private static readonly SolidColorBrush OverlayBrush = new(Color.FromArgb(170, 0, 0, 0));
        private static readonly Pen GridPen = new(GridBrush, 1);
        private static readonly Pen WallPen = new(new SolidColorBrush(Color.FromRgb(183, 205, 238)), 1);
        private static readonly Pen DoorPen = new(new SolidColorBrush(Color.FromRgb(191, 219, 254)), 1);
        private static readonly Pen ExitPen = new(new SolidColorBrush(Color.FromRgb(225, 255, 238)), 1);

        private readonly TileWorldControl _owner;

        public TileWorldBoard(TileWorldControl owner)
        {
            _owner = owner;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var state = _owner.State;
            var bounds = Bounds;
            context.FillRectangle(VoidBrush, bounds);

            if (state.Width <= 0 || state.Height <= 0)
            {
                return;
            }

            var padded = new Rect(bounds.X + 6, bounds.Y + 6, Math.Max(0, bounds.Width - 12), Math.Max(0, bounds.Height - 12));
            var cellWidth = padded.Width / state.Width;
            var cellHeight = padded.Height / state.Height;
            var cellSize = Math.Floor(Math.Min(cellWidth, cellHeight));
            if (cellSize < 18)
            {
                cellSize = Math.Min(cellWidth, cellHeight);
            }

            var boardWidth = cellSize * state.Width;
            var boardHeight = cellSize * state.Height;
            var offsetX = padded.X + (padded.Width - boardWidth) / 2;
            var offsetY = padded.Y + (padded.Height - boardHeight) / 2;

            for (var y = 0; y < state.Height; y++)
            {
                for (var x = 0; x < state.Width; x++)
                {
                    var rect = new Rect(offsetX + (x * cellSize), offsetY + (y * cellSize), cellSize, cellSize);
                    DrawTile(context, rect, state.GetTile(x, y), state.ChipsRemaining == 0);
                    context.DrawRectangle(null, GridPen, rect);
                }
            }

            DrawPlayer(context, state.Player, offsetX, offsetY, cellSize);

            if (state.LevelComplete)
            {
                var overlay = new Rect(offsetX, offsetY + (boardHeight * 0.35), boardWidth, Math.Min(84, boardHeight * 0.3));
                context.FillRectangle(OverlayBrush, overlay);
                var message = state.LevelIndex == 2 ? "BOARD SET CLEAR" : "BOARD CLEAR";
                var subtitle = state.LevelIndex == 2 ? "Jezzball has company now." : "Press N for the next board.";
                DrawCenteredText(context, message, overlay, 26, Brushes.White);
                DrawCenteredText(context, subtitle, new Rect(overlay.X, overlay.Y + 34, overlay.Width, overlay.Height - 34), 14, new SolidColorBrush(Color.FromRgb(196, 221, 255)));
            }
        }

        private static void DrawTile(DrawingContext context, Rect rect, TileWorldTile tile, bool exitOpen)
        {
            switch (tile)
            {
                case TileWorldTile.Wall:
                    context.FillRectangle(WallBrush, rect);
                    context.DrawRectangle(null, WallPen, rect.Deflate(1));
                    break;
                case TileWorldTile.Chip:
                    context.FillRectangle(FloorBrush, rect);
                    DrawChip(context, rect);
                    break;
                case TileWorldTile.Exit:
                    context.FillRectangle(exitOpen ? ExitOpenBrush : ExitClosedBrush, rect);
                    context.DrawRectangle(null, ExitPen, rect.Deflate(2));
                    DrawExitCore(context, rect, exitOpen);
                    break;
                case TileWorldTile.Socket:
                    context.FillRectangle(SocketBrush, rect);
                    DrawSocket(context, rect, exitOpen);
                    break;
                case TileWorldTile.KeyBlue:
                    context.FillRectangle(FloorBrush, rect);
                    DrawKey(context, rect, KeyBlueBrush);
                    break;
                case TileWorldTile.KeyRed:
                    context.FillRectangle(FloorBrush, rect);
                    DrawKey(context, rect, KeyRedBrush);
                    break;
                case TileWorldTile.KeyYellow:
                    context.FillRectangle(FloorBrush, rect);
                    DrawKey(context, rect, KeyYellowBrush);
                    break;
                case TileWorldTile.KeyGreen:
                    context.FillRectangle(FloorBrush, rect);
                    DrawKey(context, rect, KeyGreenBrush);
                    break;
                case TileWorldTile.DoorBlue:
                    context.FillRectangle(DoorBrush, rect);
                    context.DrawRectangle(null, DoorPen, rect.Deflate(1));
                    DrawDoor(context, rect, KeyBlueBrush);
                    break;
                case TileWorldTile.DoorRed:
                    context.FillRectangle(DoorRedBrush, rect);
                    context.DrawRectangle(null, DoorPen, rect.Deflate(1));
                    DrawDoor(context, rect, KeyRedBrush);
                    break;
                case TileWorldTile.DoorYellow:
                    context.FillRectangle(DoorYellowBrush, rect);
                    context.DrawRectangle(null, DoorPen, rect.Deflate(1));
                    DrawDoor(context, rect, KeyYellowBrush);
                    break;
                case TileWorldTile.DoorGreen:
                    context.FillRectangle(DoorGreenBrush, rect);
                    context.DrawRectangle(null, DoorPen, rect.Deflate(1));
                    DrawDoor(context, rect, KeyGreenBrush);
                    break;
                case TileWorldTile.Block:
                    context.FillRectangle(FloorBrush, rect);
                    DrawBlock(context, rect);
                    break;
                case TileWorldTile.Water:
                    context.FillRectangle(WaterBrush, rect);
                    DrawWater(context, rect);
                    break;
                case TileWorldTile.Fire:
                    context.FillRectangle(FloorBrush, rect);
                    DrawFire(context, rect);
                    break;
                case TileWorldTile.BootsWater:
                    context.FillRectangle(FloorBrush, rect);
                    DrawBoots(context, rect, BootsWaterBrush);
                    break;
                case TileWorldTile.BootsFire:
                    context.FillRectangle(FloorBrush, rect);
                    DrawBoots(context, rect, BootsFireBrush);
                    break;
                case TileWorldTile.Hint:
                    context.FillRectangle(FloorBrush, rect);
                    DrawHint(context, rect);
                    break;
                default:
                    context.FillRectangle(FloorBrush, rect);
                    break;
            }
        }

        private static void DrawChip(DrawingContext context, Rect rect)
        {
            var center = rect.Center;
            var radius = Math.Max(4, rect.Width * 0.22);
            context.DrawEllipse(ChipBrush, null, center, radius, radius);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), null, new Point(center.X - radius * 0.3, center.Y - radius * 0.3), radius * 0.45, radius * 0.45);
        }

        private static void DrawExitCore(DrawingContext context, Rect rect, bool exitOpen)
        {
            var inset = rect.Deflate(rect.Width * 0.28);
            var coreBrush = exitOpen ? new SolidColorBrush(Color.FromRgb(230, 255, 240)) : new SolidColorBrush(Color.FromRgb(61, 36, 12));
            context.FillRectangle(coreBrush, inset);
            if (exitOpen)
            {
                var arrowPen = new Pen(new SolidColorBrush(Color.FromRgb(14, 116, 41)), 2);
                var midY = inset.Center.Y;
                context.DrawLine(arrowPen, new Point(inset.X + 3, midY), new Point(inset.Right - 4, midY));
                context.DrawLine(arrowPen, new Point(inset.Right - 8, midY - 4), new Point(inset.Right - 4, midY));
                context.DrawLine(arrowPen, new Point(inset.Right - 8, midY + 4), new Point(inset.Right - 4, midY));
            }
        }

        private static void DrawSocket(DrawingContext context, Rect rect, bool open)
        {
            var panel = rect.Deflate(rect.Width * 0.16);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(224, 231, 255)), 1.5), panel);
            var coreBrush = open ? ExitOpenBrush : new SolidColorBrush(Color.FromRgb(15, 23, 42));
            context.FillRectangle(coreBrush, panel.Deflate(rect.Width * 0.18));
        }

        private static void DrawKey(DrawingContext context, Rect rect, IBrush brush)
        {
            var ringRadius = rect.Width * 0.16;
            var ringCenter = new Point(rect.X + rect.Width * 0.36, rect.Y + rect.Height * 0.38);
            context.DrawEllipse(null, new Pen(brush, 2), ringCenter, ringRadius, ringRadius);
            var shaftPen = new Pen(brush, 3);
            var shaftStart = new Point(ringCenter.X + ringRadius, ringCenter.Y);
            var shaftEnd = new Point(rect.Right - rect.Width * 0.2, ringCenter.Y);
            context.DrawLine(shaftPen, shaftStart, shaftEnd);
            context.DrawLine(shaftPen, new Point(shaftEnd.X - rect.Width * 0.1, shaftEnd.Y), new Point(shaftEnd.X - rect.Width * 0.1, shaftEnd.Y + rect.Height * 0.18));
            context.DrawLine(shaftPen, new Point(shaftEnd.X - rect.Width * 0.2, shaftEnd.Y), new Point(shaftEnd.X - rect.Width * 0.2, shaftEnd.Y + rect.Height * 0.12));
        }

        private static void DrawDoor(DrawingContext context, Rect rect, IBrush accentBrush)
        {
            var panel = rect.Deflate(rect.Width * 0.18);
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(21, 55, 112)), panel);
            context.DrawEllipse(accentBrush, null, new Point(panel.Right - panel.Width * 0.22, panel.Center.Y), rect.Width * 0.05, rect.Width * 0.05);
        }

        private static void DrawBlock(DrawingContext context, Rect rect)
        {
            var block = rect.Deflate(rect.Width * 0.16);
            context.FillRectangle(BlockBrush, block);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(225, 229, 231)), 1), block);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), new Rect(block.X + 2, block.Y + 2, block.Width * 0.45, block.Height * 0.18));
        }

        private static void DrawWater(DrawingContext context, Rect rect)
        {
            var wavePen = new Pen(new SolidColorBrush(Color.FromArgb(180, 189, 240, 255)), 1.6);
            var y1 = rect.Y + rect.Height * 0.38;
            var y2 = rect.Y + rect.Height * 0.62;
            context.DrawLine(wavePen, new Point(rect.X + rect.Width * 0.15, y1), new Point(rect.Right - rect.Width * 0.15, y1));
            context.DrawLine(wavePen, new Point(rect.X + rect.Width * 0.1, y2), new Point(rect.Right - rect.Width * 0.1, y2));
        }

        private static void DrawFire(DrawingContext context, Rect rect)
        {
            var flame = new StreamGeometry();
            using (var geometry = flame.Open())
            {
                geometry.BeginFigure(new Point(rect.Center.X, rect.Y + rect.Height * 0.18), true);
                geometry.LineTo(new Point(rect.X + rect.Width * 0.7, rect.Center.Y));
                geometry.LineTo(new Point(rect.Center.X + rect.Width * 0.08, rect.Bottom - rect.Height * 0.16));
                geometry.LineTo(new Point(rect.X + rect.Width * 0.32, rect.Bottom - rect.Height * 0.08));
                geometry.LineTo(new Point(rect.X + rect.Width * 0.22, rect.Y + rect.Height * 0.56));
                geometry.EndFigure(true);
            }

            context.DrawGeometry(FireBrush, null, flame);
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 110)), null, new Point(rect.Center.X, rect.Y + rect.Height * 0.56), rect.Width * 0.11, rect.Height * 0.11);
        }

        private static void DrawBoots(DrawingContext context, Rect rect, IBrush brush)
        {
            var sole = rect.Deflate(rect.Width * 0.2);
            context.FillRectangle(brush, new Rect(sole.X, sole.Center.Y - sole.Height * 0.1, sole.Width * 0.72, sole.Height * 0.26));
            context.FillRectangle(brush, new Rect(sole.X + sole.Width * 0.18, sole.Y + sole.Height * 0.12, sole.Width * 0.32, sole.Height * 0.46));
        }

        private static void DrawHint(DrawingContext context, Rect rect)
        {
            var circleRect = rect.Deflate(rect.Width * 0.22);
            context.DrawEllipse(HintBrush, null, circleRect.Center, circleRect.Width / 2, circleRect.Height / 2);
            DrawCenteredText(context, "?", circleRect, Math.Max(10, rect.Width * 0.42), Brushes.White);
        }

        private static void DrawPlayer(DrawingContext context, TileWorldPoint player, double offsetX, double offsetY, double cellSize)
        {
            var center = new Point(offsetX + (player.X * cellSize) + (cellSize / 2), offsetY + (player.Y * cellSize) + (cellSize / 2));
            var radius = Math.Max(5, cellSize * 0.28);
            context.DrawEllipse(PlayerBrush, null, center, radius, radius);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), null, new Point(center.X - radius * 0.35, center.Y - radius * 0.35), radius * 0.42, radius * 0.42);
            var eyeRadius = Math.Max(1.2, radius * 0.1);
            context.DrawEllipse(PlayerAccentBrush, null, new Point(center.X - radius * 0.22, center.Y - radius * 0.1), eyeRadius, eyeRadius);
            context.DrawEllipse(PlayerAccentBrush, null, new Point(center.X + radius * 0.22, center.Y - radius * 0.1), eyeRadius, eyeRadius);
        }

        private static void DrawCenteredText(DrawingContext context, string text, Rect rect, double fontSize, IBrush brush)
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                fontSize,
                brush);

            var origin = new Point(
                rect.X + ((rect.Width - formattedText.Width) / 2),
                rect.Y + ((rect.Height - formattedText.Height) / 2));

            context.DrawText(formattedText, origin);
        }
    }
}

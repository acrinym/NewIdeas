using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Effects;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public sealed class GweledPlugin : IPlugin
    {
        private PluginWindowBase? _window;
        private GweledControl? _control;

        public string Name => "Gweled";
        public string Description => "Classic jewel-swapping puzzle play with normal, timed, and endless modes.";
        public Version Version => new(0, 1, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _control = new GweledControl();

            _window = new PluginWindowBase
            {
                Title = "Gweled",
                Width = 860,
                Height = 860,
                MinWidth = 680,
                MinHeight = 720,
                Content = _control,
                Plugin = this
            };
            _window.ApplyPluginThemeAndSkin(this);
            _window.KeyDown += OnWindowKeyDown;
            _window.Opened += OnWindowOpened;
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(GweledPlugin));
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

            if (e.Key == Key.R)
            {
                _control.StartNewGame();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space)
            {
                _control.TogglePause();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.H)
            {
                _control.ShowHintNow();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F1)
            {
                _control.ShowHelp();
                e.Handled = true;
            }
        }
    }

    internal enum GweledMode
    {
        Normal,
        Timed,
        Endless
    }

    internal readonly record struct GweledPoint(int X, int Y);

    internal readonly record struct GweledMove(GweledPoint From, GweledPoint To);

    internal readonly record struct GweledMatch(int StartX, int StartY, bool Horizontal, int Length);

    internal enum GweledInteractionResult
    {
        None,
        Selected,
        Deselected,
        Swapped,
        IllegalMove,
        Restarted
    }

    internal sealed class GweledGameState
    {
        public const int Width = 8;
        public const int Height = 8;
        public const int GemTypeCount = 7;
        private const int FirstBonusAt = 100;
        private const int BonusGemCount = 8;
        private const int TotalStepsForTimer = 60;
        private const double HintTimeoutSeconds = 15.0;
        private readonly Random _random = new();
        private readonly int[,] _board = new int[Width, Height];
        private readonly List<GweledPoint> _hintCells = new();
        private GweledPoint? _selected;
        private string _overlayMessage = string.Empty;
        private bool _overlaySticky;
        private double _overlayTimeRemaining;
        private double _hintIdleSeconds;
        private double _hintPulse;
        private double _timedAccumulator;
        private int _bonusMultiply = 3;

        public GweledGameState()
        {
            StartNewGame(GweledMode.Normal);
        }

        public GweledMode Mode { get; private set; }

        public long Score { get; private set; }

        public int Level { get; private set; }

        public int TotalGemsRemoved { get; private set; }

        public int PreviousBonusAt { get; private set; }

        public int NextBonusAt { get; private set; }

        public int TimerDrainPerSecond { get; private set; }

        public bool SoundsEnabled { get; set; } = true;

        public bool HintsEnabled { get; set; } = true;

        public bool IsPaused { get; private set; }

        public bool IsGameOver { get; private set; }

        public GweledPoint? Selected => _selected;

        public IReadOnlyList<GweledPoint> HintCells => _hintCells;

        public double HintPulse => _hintPulse;

        public string OverlayMessage => _overlayMessage;

        public int BonusDisplayMultiplier => Math.Max(1, _bonusMultiply >> 1);

        public int this[int x, int y] => _board[x, y];

        public void StartNewGame(GweledMode mode)
        {
            Mode = mode;
            Score = 0;
            Level = 1;
            TotalGemsRemoved = mode == GweledMode.Timed ? FirstBonusAt / 2 : 0;
            PreviousBonusAt = 0;
            NextBonusAt = FirstBonusAt;
            TimerDrainPerSecond = Math.Max(1, FirstBonusAt / TotalStepsForTimer);
            _bonusMultiply = 3;
            IsPaused = false;
            IsGameOver = false;
            _selected = null;
            _hintCells.Clear();
            _hintIdleSeconds = 0;
            _hintPulse = 0;
            _timedAccumulator = 0;
            ClearOverlay();
            FillFreshBoard();
            EnsureBoardHasMoves();
        }

        public void TogglePause()
        {
            if (IsGameOver)
            {
                return;
            }

            IsPaused = !IsPaused;
            if (IsPaused)
            {
                ShowOverlay("Paused", 0, true);
            }
            else if (_overlaySticky && string.Equals(_overlayMessage, "Paused", StringComparison.Ordinal))
            {
                ClearOverlay();
            }
        }

        public GweledInteractionResult HandleCellClick(int x, int y)
        {
            RegisterInteraction();

            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return GweledInteractionResult.None;
            }

            if (IsGameOver)
            {
                StartNewGame(Mode);
                return GweledInteractionResult.Restarted;
            }

            if (IsPaused)
            {
                return GweledInteractionResult.None;
            }

            var point = new GweledPoint(x, y);

            if (_selected == null)
            {
                _selected = point;
                return GweledInteractionResult.Selected;
            }

            if (_selected.Value == point)
            {
                _selected = null;
                return GweledInteractionResult.Deselected;
            }

            if (!AreAdjacent(_selected.Value, point))
            {
                _selected = point;
                return GweledInteractionResult.Selected;
            }

            Swap(_selected.Value, point);
            var matches = FindMatches();
            if (matches.Count == 0)
            {
                Swap(_selected.Value, point);
                _selected = null;
                ShowOverlay("Illegal move", 0.8, false);
                return GweledInteractionResult.IllegalMove;
            }

            _selected = null;
            ResolveMatches(matches);
            HandlePostTurnState();
            return GweledInteractionResult.Swapped;
        }

        public void ShowHint()
        {
            RegisterInteraction();
            if (TryFindHint(out var move))
            {
                _hintCells.Clear();
                _hintCells.Add(move.From);
                _hintCells.Add(move.To);
                ShowOverlay("Hint ready", 1.0, false);
            }
        }

        public void Update(double dt)
        {
            _hintPulse += dt;

            if (!_overlaySticky && _overlayTimeRemaining > 0)
            {
                _overlayTimeRemaining -= dt;
                if (_overlayTimeRemaining <= 0)
                {
                    ClearOverlay();
                }
            }

            if (IsPaused || IsGameOver)
            {
                return;
            }

            if (Mode == GweledMode.Timed)
            {
                _timedAccumulator += dt;
                while (_timedAccumulator >= 1.0)
                {
                    _timedAccumulator -= 1.0;
                    TotalGemsRemoved -= TimerDrainPerSecond;
                    if (TotalGemsRemoved <= PreviousBonusAt)
                    {
                        TotalGemsRemoved = PreviousBonusAt;
                        EndGame("Time's up!\nClick a gem to restart.");
                        break;
                    }
                }
            }

            if (HintsEnabled && _selected == null && _hintCells.Count == 0)
            {
                _hintIdleSeconds += dt;
                if (_hintIdleSeconds >= HintTimeoutSeconds)
                {
                    if (TryFindHint(out var move))
                    {
                        _hintCells.Add(move.From);
                        _hintCells.Add(move.To);
                    }
                    _hintIdleSeconds = 0;
                }
            }
        }

        public double GetProgressFraction()
        {
            if (Mode == GweledMode.Endless)
            {
                return 0;
            }

            var span = NextBonusAt - PreviousBonusAt;
            if (span <= 0)
            {
                return 0;
            }

            var value = (double)(TotalGemsRemoved - PreviousBonusAt) / span;
            return Math.Clamp(value, 0, 1);
        }

        public string GetProgressText()
        {
            return Mode switch
            {
                GweledMode.Endless => "Endless board",
                GweledMode.Timed => $"Level {Level}  Drift -{TimerDrainPerSecond}/s",
                _ => $"Level {Level}  Next bonus at {NextBonusAt}"
            };
        }

        public string GetModeLabel()
        {
            return Mode switch
            {
                GweledMode.Timed => "Timed",
                GweledMode.Endless => "Endless",
                _ => "Normal"
            };
        }

        private void ResolveMatches(List<GweledMatch> matches)
        {
            var scorePerMove = 0;
            while (matches.Count > 0)
            {
                var uniqueCells = new HashSet<GweledPoint>();
                foreach (var match in matches)
                {
                    var matchScore = GetMatchScore(match.Length, scorePerMove);
                    Score += matchScore;
                    scorePerMove = matchScore;
                    AddMatchCells(match, uniqueCells);
                }

                TotalGemsRemoved += uniqueCells.Count;
                foreach (var cell in uniqueCells)
                {
                    _board[cell.X, cell.Y] = -1;
                }

                CollapseAndRefill();
                matches = FindMatches();
            }
        }

        private void HandlePostTurnState()
        {
            while (Mode != GweledMode.Endless && TotalGemsRemoved >= NextBonusAt && !IsGameOver)
            {
                PreviousBonusAt = NextBonusAt;
                NextBonusAt *= 2;
                TimerDrainPerSecond = Math.Max(1, (NextBonusAt - PreviousBonusAt) / TotalStepsForTimer + 1);
                _bonusMultiply++;
                Level++;
                ShowOverlay($"Level {Level}\nBonus x{BonusDisplayMultiplier}", 1.4, false);

                TriggerBonusBlast();
                if (Mode == GweledMode.Timed)
                {
                    TotalGemsRemoved = (NextBonusAt + PreviousBonusAt) / 2;
                }
            }

            if (!HasMovesAvailable())
            {
                if (Mode == GweledMode.Normal)
                {
                    EndGame("No moves left!\nClick a gem to restart.");
                }
                else
                {
                    FillFreshBoard();
                    EnsureBoardHasMoves();
                    ShowOverlay("No moves left!\nBoard reshuffled.", 1.4, false);
                }
            }
        }

        private void TriggerBonusBlast()
        {
            var cells = new List<GweledPoint>();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    cells.Add(new GweledPoint(x, y));
                }
            }

            for (var i = cells.Count - 1; i > 0; i--)
            {
                var swapIndex = _random.Next(i + 1);
                (cells[i], cells[swapIndex]) = (cells[swapIndex], cells[i]);
            }

            var picked = cells.Take(BonusGemCount).ToList();
            foreach (var cell in picked)
            {
                _board[cell.X, cell.Y] = -1;
            }

            Score += picked.Count * 10L;
            TotalGemsRemoved += picked.Count;
            CollapseAndRefill();
            ResolveMatches(FindMatches());
        }

        private void EndGame(string message)
        {
            IsGameOver = true;
            IsPaused = false;
            _selected = null;
            _hintCells.Clear();
            ShowOverlay(message, 0, true);
        }

        private void RegisterInteraction()
        {
            _hintIdleSeconds = 0;
            _hintCells.Clear();
            if (!_overlaySticky)
            {
                ClearOverlay();
            }
        }

        private void FillFreshBoard()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    _board[x, y] = GetFreshGemForCell(x, y);
                }
            }
        }

        private void EnsureBoardHasMoves()
        {
            var attempts = 0;
            while (!HasMovesAvailable() && attempts < 32)
            {
                FillFreshBoard();
                attempts++;
            }
        }

        private int GetFreshGemForCell(int x, int y)
        {
            for (var attempts = 0; attempts < 16; attempts++)
            {
                var gem = _random.Next(GemTypeCount);
                if (!CreatesInitialMatch(x, y, gem))
                {
                    return gem;
                }
            }

            return _random.Next(GemTypeCount);
        }

        private bool CreatesInitialMatch(int x, int y, int gem)
        {
            if (x >= 2 && _board[x - 1, y] == gem && _board[x - 2, y] == gem)
            {
                return true;
            }

            if (y >= 2 && _board[x, y - 1] == gem && _board[x, y - 2] == gem)
            {
                return true;
            }

            return false;
        }

        private void CollapseAndRefill()
        {
            for (var x = 0; x < Width; x++)
            {
                var writeY = Height - 1;
                for (var readY = Height - 1; readY >= 0; readY--)
                {
                    if (_board[x, readY] >= 0)
                    {
                        _board[x, writeY] = _board[x, readY];
                        if (writeY != readY)
                        {
                            _board[x, readY] = -1;
                        }
                        writeY--;
                    }
                }

                while (writeY >= 0)
                {
                    _board[x, writeY] = _random.Next(GemTypeCount);
                    writeY--;
                }
            }
        }

        private List<GweledMatch> FindMatches()
        {
            var matches = new List<GweledMatch>();

            for (var y = 0; y < Height; y++)
            {
                var runStart = 0;
                while (runStart < Width)
                {
                    var gem = _board[runStart, y];
                    var runLength = 1;
                    while (runStart + runLength < Width && _board[runStart + runLength, y] == gem)
                    {
                        runLength++;
                    }

                    if (gem >= 0 && runLength >= 3)
                    {
                        matches.Add(new GweledMatch(runStart, y, true, runLength));
                    }

                    runStart += runLength;
                }
            }

            for (var x = 0; x < Width; x++)
            {
                var runStart = 0;
                while (runStart < Height)
                {
                    var gem = _board[x, runStart];
                    var runLength = 1;
                    while (runStart + runLength < Height && _board[x, runStart + runLength] == gem)
                    {
                        runLength++;
                    }

                    if (gem >= 0 && runLength >= 3)
                    {
                        matches.Add(new GweledMatch(x, runStart, false, runLength));
                    }

                    runStart += runLength;
                }
            }

            return matches;
        }

        private static void AddMatchCells(GweledMatch match, ISet<GweledPoint> cells)
        {
            for (var i = 0; i < match.Length; i++)
            {
                var x = match.Horizontal ? match.StartX + i : match.StartX;
                var y = match.Horizontal ? match.StartY : match.StartY + i;
                cells.Add(new GweledPoint(x, y));
            }
        }

        private int GetMatchScore(int length, int scorePerMove)
        {
            return 10 * BonusDisplayMultiplier * (length - 2) + scorePerMove;
        }

        private bool HasMovesAvailable()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var from = new GweledPoint(x, y);
                    var right = new GweledPoint(x + 1, y);
                    var down = new GweledPoint(x, y + 1);

                    if (x + 1 < Width && WouldCreateMatch(from, right))
                    {
                        return true;
                    }

                    if (y + 1 < Height && WouldCreateMatch(from, down))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryFindHint(out GweledMove move)
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var from = new GweledPoint(x, y);
                    var right = new GweledPoint(x + 1, y);
                    var down = new GweledPoint(x, y + 1);

                    if (x + 1 < Width && WouldCreateMatch(from, right))
                    {
                        move = new GweledMove(from, right);
                        return true;
                    }

                    if (y + 1 < Height && WouldCreateMatch(from, down))
                    {
                        move = new GweledMove(from, down);
                        return true;
                    }
                }
            }

            move = default;
            return false;
        }

        private bool WouldCreateMatch(GweledPoint a, GweledPoint b)
        {
            Swap(a, b);
            var hasMatch = FindMatches().Count > 0;
            Swap(a, b);
            return hasMatch;
        }

        private void Swap(GweledPoint a, GweledPoint b)
        {
            (_board[a.X, a.Y], _board[b.X, b.Y]) = (_board[b.X, b.Y], _board[a.X, a.Y]);
        }

        private static bool AreAdjacent(GweledPoint a, GweledPoint b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);
            return dx + dy == 1;
        }

        private void ShowOverlay(string message, double durationSeconds, bool sticky)
        {
            _overlayMessage = message;
            _overlaySticky = sticky;
            _overlayTimeRemaining = durationSeconds;
        }

        private void ClearOverlay()
        {
            _overlayMessage = string.Empty;
            _overlaySticky = false;
            _overlayTimeRemaining = 0;
        }
    }

    internal sealed class GweledControl : UserControl, IDisposable
    {
        private readonly GweledGameState _state = new();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch = new();
        private readonly GweledBoardView _board;
        private readonly TextBlock _modeText;
        private readonly TextBlock _scoreText;
        private readonly TextBlock _levelText;
        private readonly TextBlock _bonusText;
        private readonly TextBlock _progressText;
        private readonly ProgressBar _progressBar;
        private readonly Button _pauseButton;
        private readonly RadioButton _normalModeButton;
        private readonly RadioButton _timedModeButton;
        private readonly RadioButton _endlessModeButton;
        private readonly CheckBox _autoHintsCheckBox;
        private readonly CheckBox _soundsCheckBox;
        private readonly string? _clickSoundPath;
        private readonly string? _swapSoundPath;

        public GweledControl()
        {
            _clickSoundPath = ResolveSoundPath("click.ogg");
            _swapSoundPath = ResolveSoundPath("swap.ogg");

            _board = new GweledBoardView(_state, OnBoardCellPressed);
            _modeText = new TextBlock();
            _scoreText = new TextBlock();
            _levelText = new TextBlock();
            _bonusText = new TextBlock();
            _progressText = new TextBlock();
            _progressBar = new ProgressBar { Minimum = 0, Maximum = 1, Height = 18 };
            _pauseButton = new Button { Content = "Pause", MinWidth = 80 };
            _normalModeButton = new RadioButton { Content = "Normal", GroupName = "gweled-mode" };
            _timedModeButton = new RadioButton { Content = "Timed", GroupName = "gweled-mode" };
            _endlessModeButton = new RadioButton { Content = "Endless", GroupName = "gweled-mode" };
            _autoHintsCheckBox = new CheckBox { Content = "Auto Hints", IsChecked = true };
            _soundsCheckBox = new CheckBox { Content = "Sounds", IsChecked = true };

            var toolbar = BuildToolbar();
            var statusPanel = BuildStatusPanel();
            var footer = new TextBlock
            {
                Text = "Click gems to swap. R starts over, Space pauses, H shows a hint, and F1 opens help.",
                Margin = new Thickness(12, 8, 12, 12),
                Opacity = 0.72,
                TextWrapping = TextWrapping.Wrap
            };

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto")
            };

            Grid.SetRow(toolbar, 0);
            Grid.SetRow(statusPanel, 1);
            Grid.SetRow(_progressBar, 2);
            Grid.SetRow(_board, 3);
            Grid.SetRow(footer, 4);

            root.Children.Add(toolbar);
            root.Children.Add(statusPanel);
            root.Children.Add(_progressBar);
            root.Children.Add(_board);
            root.Children.Add(footer);

            Content = root;

            _pauseButton.Click += (_, _) => TogglePause();
            _normalModeButton.IsChecked = true;
            _normalModeButton.IsCheckedChanged += (_, _) => SetModeIfChecked(_normalModeButton, GweledMode.Normal);
            _timedModeButton.IsCheckedChanged += (_, _) => SetModeIfChecked(_timedModeButton, GweledMode.Timed);
            _endlessModeButton.IsCheckedChanged += (_, _) => SetModeIfChecked(_endlessModeButton, GweledMode.Endless);
            _autoHintsCheckBox.IsCheckedChanged += (_, _) => SetAutoHints(_autoHintsCheckBox.IsChecked == true);
            _soundsCheckBox.IsCheckedChanged += (_, _) => SetSoundsEnabled(_soundsCheckBox.IsChecked == true);

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, OnTick);
            _timer.Start();
            _stopwatch.Start();

            UpdateStatus();
        }

        public void FocusBoard()
        {
            _board.Focus();
        }

        public void StartNewGame()
        {
            _state.StartNewGame(_state.Mode);
            UpdateStatus();
            _board.InvalidateVisual();
        }

        public void TogglePause()
        {
            _state.TogglePause();
            UpdateStatus();
            _board.InvalidateVisual();
        }

        public void ShowHintNow()
        {
            _state.ShowHint();
            UpdateStatus();
            _board.InvalidateVisual();
        }

        public void ShowHelp()
        {
            var owner = VisualRoot as Window;
            var helpWindow = new Window
            {
                Title = "Gweled Help",
                Width = 500,
                Height = 420,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                Content = new ScrollViewer
                {
                    Content = new TextBox
                    {
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(12),
                        Text =
@"Gweled for Cycloside

OBJECTIVE
Swap adjacent gems to make lines of three or more.

MODES
- Normal: clear enough gems to level up, but no moves left ends the run.
- Timed: the level meter constantly drifts backwards. Reach the next level before it empties.
- Endless: no level pressure, and dead boards reshuffle automatically.

DETAILS
- Every level doubles the next target and improves the bonus multiplier.
- Idle auto-hints wake up after fifteen seconds unless you turn them off.
- Press H for a manual hint.
- Press Space to pause and R to start a fresh board.

SOURCE INTENT
This version follows the original Gweled mode structure and level loop, but it is implemented natively inside Cycloside for the retro desktop shell."
                    }
                }
            };

            if (owner != null)
            {
                helpWindow.Show(owner);
            }
            else
            {
                helpWindow.Show();
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _stopwatch.Stop();
            _timer.Tick -= OnTick;
        }

        private Control BuildToolbar()
        {
            var newButton = new Button { Content = "New Game", MinWidth = 100 };
            newButton.Click += (_, _) => StartNewGame();

            var hintButton = new Button { Content = "Hint", MinWidth = 80 };
            hintButton.Click += (_, _) => ShowHintNow();

            var helpButton = new Button { Content = "Help", MinWidth = 80 };
            helpButton.Click += (_, _) => ShowHelp();

            var panel = new WrapPanel
            {
                Margin = new Thickness(12, 12, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(newButton);
            panel.Children.Add(_pauseButton);
            panel.Children.Add(hintButton);
            panel.Children.Add(helpButton);
            panel.Children.Add(new TextBlock { Text = "   " });
            panel.Children.Add(_normalModeButton);
            panel.Children.Add(_timedModeButton);
            panel.Children.Add(_endlessModeButton);
            panel.Children.Add(new TextBlock { Text = "   " });
            panel.Children.Add(_autoHintsCheckBox);
            panel.Children.Add(_soundsCheckBox);
            return panel;
        }

        private Control BuildStatusPanel()
        {
            _modeText.FontWeight = FontWeight.SemiBold;
            _scoreText.FontWeight = FontWeight.SemiBold;
            _levelText.FontWeight = FontWeight.SemiBold;
            _bonusText.FontWeight = FontWeight.SemiBold;
            _progressText.Opacity = 0.8;

            var primaryRow = new WrapPanel
            {
                Margin = new Thickness(12, 0, 12, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            primaryRow.Children.Add(_modeText);
            primaryRow.Children.Add(new TextBlock { Text = "  |  " });
            primaryRow.Children.Add(_scoreText);
            primaryRow.Children.Add(new TextBlock { Text = "  |  " });
            primaryRow.Children.Add(_levelText);
            primaryRow.Children.Add(new TextBlock { Text = "  |  " });
            primaryRow.Children.Add(_bonusText);

            var container = new StackPanel
            {
                Margin = new Thickness(12, 0, 12, 8),
                Orientation = Orientation.Vertical
            };
            container.Children.Add(primaryRow);
            container.Children.Add(_progressText);
            return container;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var elapsed = _stopwatch.Elapsed;
            _stopwatch.Restart();
            _state.Update(elapsed.TotalSeconds);
            UpdateStatus();
            _board.InvalidateVisual();
        }

        private void OnBoardCellPressed(int x, int y)
        {
            var result = _state.HandleCellClick(x, y);
            switch (result)
            {
                case GweledInteractionResult.Selected:
                case GweledInteractionResult.Deselected:
                case GweledInteractionResult.IllegalMove:
                    PlaySound(_clickSoundPath);
                    break;
                case GweledInteractionResult.Swapped:
                    PlaySound(_swapSoundPath);
                    break;
            }

            UpdateStatus();
            _board.InvalidateVisual();
        }

        private void SetModeIfChecked(ToggleButton radioButton, GweledMode mode)
        {
            if (radioButton.IsChecked == true && _state.Mode != mode)
            {
                _state.StartNewGame(mode);
                UpdateStatus();
                _board.InvalidateVisual();
            }
        }

        private void SetAutoHints(bool enabled)
        {
            _state.HintsEnabled = enabled;
            UpdateStatus();
            _board.InvalidateVisual();
        }

        private void SetSoundsEnabled(bool enabled)
        {
            _state.SoundsEnabled = enabled;
        }

        private void UpdateStatus()
        {
            _modeText.Text = $"Mode: {_state.GetModeLabel()}";
            _scoreText.Text = $"Score: {_state.Score.ToString("N0", CultureInfo.InvariantCulture)}";
            _levelText.Text = $"Level: {_state.Level}";
            _bonusText.Text = $"Bonus: x{_state.BonusDisplayMultiplier}";
            _progressText.Text = _state.GetProgressText();
            _progressBar.IsVisible = _state.Mode != GweledMode.Endless;
            _progressBar.Value = _state.GetProgressFraction();
            _pauseButton.Content = _state.IsPaused ? "Resume" : "Pause";

            _normalModeButton.IsChecked = _state.Mode == GweledMode.Normal;
            _timedModeButton.IsChecked = _state.Mode == GweledMode.Timed;
            _endlessModeButton.IsChecked = _state.Mode == GweledMode.Endless;
        }

        private void PlaySound(string? path)
        {
            if (_state.SoundsEnabled && !string.IsNullOrWhiteSpace(path))
            {
                AudioService.Play(path);
            }
        }

        private static string? ResolveSoundPath(string fileName)
        {
            var roots = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Resources", "Gweled", "Sounds"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "TWorld",
                    "gweled-1.0-beta1",
                    "sounds")
            };

            foreach (var root in roots)
            {
                var fullPath = Path.Combine(root, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }
    }

    internal sealed class GweledBoardView : Control
    {
        private static readonly Color[] GemColors =
        {
            Color.FromRgb(255, 102, 179),
            Color.FromRgb(96, 210, 255),
            Color.FromRgb(255, 214, 92),
            Color.FromRgb(146, 255, 122),
            Color.FromRgb(255, 136, 74),
            Color.FromRgb(181, 128, 255),
            Color.FromRgb(255, 72, 72)
        };

        private readonly GweledGameState _state;
        private readonly Action<int, int> _onCellPressed;

        public GweledBoardView(GweledGameState state, Action<int, int> onCellPressed)
        {
            _state = state;
            _onCellPressed = onCellPressed;
            Focusable = true;
            PointerPressed += OnPointerPressed;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var boardRect = GetBoardRect(Bounds.Size);
            context.FillRectangle(
                new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.FromRgb(14, 18, 32), 0),
                        new GradientStop(Color.FromRgb(5, 7, 14), 1)
                    }
                },
                Bounds);

            var frameBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(27, 42, 67), 0),
                    new GradientStop(Color.FromRgb(8, 12, 24), 1)
                }
            };
            context.FillRectangle(frameBrush, boardRect.Inflate(12));
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(103, 166, 219), 0.55), 2), boardRect.Inflate(12));

            var cellSize = boardRect.Width / GweledGameState.Width;
            var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(122, 162, 212), 0.18), 1);

            for (var x = 0; x < GweledGameState.Width; x++)
            {
                for (var y = 0; y < GweledGameState.Height; y++)
                {
                    var cellRect = new Rect(
                        boardRect.X + x * cellSize,
                        boardRect.Y + y * cellSize,
                        cellSize,
                        cellSize);
                    var cellBackground = ((x + y) & 1) == 0
                        ? new SolidColorBrush(Color.FromRgb(18, 26, 40))
                        : new SolidColorBrush(Color.FromRgb(14, 22, 34));
                    context.FillRectangle(cellBackground, cellRect);
                    context.DrawRectangle(null, gridPen, cellRect);

                    DrawGem(context, cellRect.Deflate(cellSize * 0.16), _state[x, y], cellSize);
                    DrawHighlights(context, cellRect, x, y);
                }
            }

            if (!string.IsNullOrWhiteSpace(_state.OverlayMessage))
            {
                var overlayRect = boardRect.Deflate(cellSize * 0.6);
                context.FillRectangle(new SolidColorBrush(Colors.Black, 0.72), overlayRect);
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(255, 234, 136), 0.92), 2), overlayRect);
                var text = new FormattedText(
                    _state.OverlayMessage,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    Math.Max(22, cellSize * 0.36),
                    Brushes.White);
                context.DrawText(
                    text,
                    new Point(
                        overlayRect.X + (overlayRect.Width - text.Width) / 2,
                        overlayRect.Y + (overlayRect.Height - text.Height) / 2));
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Focus();
            var point = e.GetPosition(this);
            if (TryMapPointToCell(point, out var x, out var y))
            {
                _onCellPressed(x, y);
            }
        }

        private void DrawGem(DrawingContext context, Rect rect, int gemType, double cellSize)
        {
            if (gemType < 0 || gemType >= GemColors.Length)
            {
                return;
            }

            var color = GemColors[gemType];
            var shadowRect = new Rect(rect.X, rect.Y + cellSize * 0.05, rect.Width, rect.Height);
            context.DrawGeometry(new SolidColorBrush(Colors.Black, 0.35), null, CreateGemGeometry(gemType, shadowRect));

            var geometry = CreateGemGeometry(gemType, rect);
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.15, 0.1, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.85, 0.9, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Lighten(color, 0.35), 0),
                    new GradientStop(color, 0.45),
                    new GradientStop(Darken(color, 0.28), 1)
                }
            };

            context.DrawGeometry(brush, new Pen(new SolidColorBrush(Lighten(color, 0.52), 0.92), 2), geometry);

            var shineRect = new Rect(rect.X + rect.Width * 0.18, rect.Y + rect.Height * 0.12, rect.Width * 0.35, rect.Height * 0.24);
            context.DrawEllipse(new SolidColorBrush(Colors.White, 0.28), null, shineRect);
        }

        private void DrawHighlights(DrawingContext context, Rect cellRect, int cellX, int cellY)
        {
            var cell = new GweledPoint(cellX, cellY);

            if (_state.Selected == cell)
            {
                var highlight = cellRect.Deflate(4);
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(255, 245, 120)), 4), highlight);
            }

            if (_state.HintCells.Contains(cell))
            {
                var pulse = 0.45 + 0.35 * Math.Sin(_state.HintPulse * 5.0);
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(116, 236, 255), pulse), 4), cellRect.Deflate(8));
            }
        }

        private Rect GetBoardRect(Size bounds)
        {
            var available = Math.Min(bounds.Width - 32, bounds.Height - 32);
            var cellSize = Math.Floor(available / GweledGameState.Width);
            var boardSize = cellSize * GweledGameState.Width;
            var left = Math.Floor((bounds.Width - boardSize) / 2);
            var top = Math.Floor((bounds.Height - boardSize) / 2);
            return new Rect(left, top, boardSize, boardSize);
        }

        private bool TryMapPointToCell(Point point, out int x, out int y)
        {
            var boardRect = GetBoardRect(Bounds.Size);
            if (!boardRect.Contains(point))
            {
                x = -1;
                y = -1;
                return false;
            }

            var cellSize = boardRect.Width / GweledGameState.Width;
            x = (int)((point.X - boardRect.X) / cellSize);
            y = (int)((point.Y - boardRect.Y) / cellSize);
            return x >= 0 && y >= 0 && x < GweledGameState.Width && y < GweledGameState.Height;
        }

        private Geometry CreateGemGeometry(int gemType, Rect rect)
        {
            return gemType switch
            {
                0 => CreatePolygonGeometry(
                    new Point(rect.Center.X, rect.Top),
                    new Point(rect.Right, rect.Center.Y),
                    new Point(rect.Center.X, rect.Bottom),
                    new Point(rect.Left, rect.Center.Y)),
                1 => CreatePolygonGeometry(
                    new Point(rect.X + rect.Width * 0.22, rect.Top),
                    new Point(rect.X + rect.Width * 0.78, rect.Top),
                    new Point(rect.Right, rect.Y + rect.Height * 0.4),
                    new Point(rect.X + rect.Width * 0.78, rect.Bottom),
                    new Point(rect.X + rect.Width * 0.22, rect.Bottom),
                    new Point(rect.Left, rect.Y + rect.Height * 0.4)),
                2 => new RectangleGeometry(rect, 14, 14),
                3 => CreatePolygonGeometry(
                    new Point(rect.Center.X, rect.Top),
                    new Point(rect.Right, rect.Bottom),
                    new Point(rect.Left, rect.Bottom)),
                4 => new EllipseGeometry(rect),
                5 => CreateStarGeometry(rect),
                _ => CreatePolygonGeometry(
                    new Point(rect.Center.X, rect.Top),
                    new Point(rect.Right, rect.Y + rect.Height * 0.35),
                    new Point(rect.X + rect.Width * 0.82, rect.Bottom),
                    new Point(rect.X + rect.Width * 0.18, rect.Bottom),
                    new Point(rect.Left, rect.Y + rect.Height * 0.35))
            };
        }

        private static Geometry CreateStarGeometry(Rect rect)
        {
            var center = rect.Center;
            var outerRadius = rect.Width * 0.5;
            var innerRadius = outerRadius * 0.48;
            var points = new Point[10];
            for (var i = 0; i < 10; i++)
            {
                var angle = -Math.PI / 2 + i * Math.PI / 5;
                var radius = (i & 1) == 0 ? outerRadius : innerRadius;
                points[i] = new Point(
                    center.X + Math.Cos(angle) * radius,
                    center.Y + Math.Sin(angle) * radius);
            }

            return CreatePolygonGeometry(points);
        }

        private static Geometry CreatePolygonGeometry(params Point[] points)
        {
            var stream = new StreamGeometry();
            using (var context = stream.Open())
            {
                context.BeginFigure(points[0], true);
                for (var i = 1; i < points.Length; i++)
                {
                    context.LineTo(points[i]);
                }
                context.EndFigure(true);
            }

            return stream;
        }

        private static Color Lighten(Color color, double amount)
        {
            var r = color.R + (255 - color.R) * amount;
            var g = color.G + (255 - color.G) * amount;
            var b = color.B + (255 - color.B) * amount;
            return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }

        private static Color Darken(Color color, double amount)
        {
            var r = color.R * (1 - amount);
            var g = color.G * (1 - amount);
            var b = color.B * (1 - amount);
            return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }
    }
}

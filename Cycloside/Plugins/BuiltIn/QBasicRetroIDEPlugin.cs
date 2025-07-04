using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CliWrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    // A simple record to define a command for the new Command Palette
    public record IdeCommand(string Name, string Description, IAsyncRelayCommand Command);

    public partial class QBasicRetroIDEPlugin : ObservableObject, IPlugin
    {
        private Window? _window;
        private TextEditor? _editor;
        private TreeView? _projectTree;
        private TextBlock? _status;
        private string _qb64Path = "qb64pe"; // Updated default
        private Process? _qb64Process;
        private string? _currentFile;
        private string? _projectPath;
        private bool _isCompiling = false;
        private bool _hasUnsavedChanges = false;
        
        // List of commands for the Command Palette
        private List<IdeCommand> _ideCommands = new();

        public string Name => "QBasic Retro IDE";
        public string Description => "Edit and run .BAS files using QB64 Phoenix";
        public Version Version => new Version(0, 5, 0); // Version bump for new features
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _qb64Path = !string.IsNullOrWhiteSpace(SettingsManager.Settings.QB64Path)
                ? SettingsManager.Settings.QB64Path
                : @"C:\qb64pe\qb64pe.exe"; // A more sensible default
            
            _editor = new TextEditor
            {
                ShowLineNumbers = true,
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("VBNET") ?? HighlightingManager.Instance.GetDefinition("C#"),
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 128)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                IsReadOnly = false
            };
            
            // --- FIX: Force focus on click to solve the input issue ---
            _editor.PointerPressed += (s, e) => _editor?.Focus();
            
            _editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
            _editor.TextChanged += (_, _) =>
            {
                _hasUnsavedChanges = true;
                UpdateStatus();
            };

            _projectTree = new TreeView { Width = 200, Margin = new Thickness(2) };
            _projectTree.DoubleTapped += async (_, __) =>
            {
                if (_projectTree.SelectedItem is TreeViewItem { Tag: string path })
                {
                    await LoadFile(path);
                }
            };
            
            // Initialize commands *before* building the menu
            InitializeCommands();
            
            _status = new TextBlock { Foreground = Brushes.White, Margin = new Thickness(5, 0), VerticalAlignment = VerticalAlignment.Center };
            var menu = BuildMenu();
            var statusBar = new DockPanel { Height = 24, Background = Brushes.DarkSlateBlue };
            DockPanel.SetDock(statusBar, Dock.Bottom);
            statusBar.Children.Add(_status);
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var splitter = new GridSplitter { Width = 2, Background = Brushes.DarkSlateBlue, HorizontalAlignment = HorizontalAlignment.Right };
            grid.Children.Add(_projectTree);
            grid.Children.Add(splitter);
            grid.Children.Add(_editor);
            Grid.SetColumn(_projectTree, 0);
            Grid.SetColumn(splitter, 0);
            Grid.SetColumn(_editor, 1);

            var dock = new DockPanel();
            DockPanel.SetDock(menu, Dock.Top);
            dock.Children.Add(menu);
            dock.Children.Add(statusBar);
            dock.Children.Add(grid);
            
            _window = new Window
            {
                Title = "QBasic Retro IDE",
                WindowState = WindowState.Maximized,
                Content = dock,
                DataContext = this // Set DataContext for command bindings
            };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(QBasicRetroIDEPlugin));
            _window.KeyDown += Window_KeyDown;
            _window.Opened += (_, _) => _editor?.Focus();
            _window.Show();
            UpdateStatus();
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
            _editor = null;
            _projectTree = null;
            _status = null;
            try
            {
                if (_qb64Process != null && !_qb64Process.HasExited)
                {
                    _qb64Process.Kill();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to close QB64 process: {ex.Message}");
            }
            finally
            {
                _qb64Process = null;
            }
        }

        // Initialize all commands for binding and for the palette
        private void InitializeCommands()
        {
            _ideCommands = new List<IdeCommand>
            {
                new("New File", "Create a new empty file", NewFileCommand),
                new("Open File...", "Open a .BAS file from disk", OpenFileCommand),
                new("Save File", "Save the current file to disk", SaveFileCommand),
                new("Save File As...", "Save the current file to a new location", SaveFileAsCommand),
                new("Compile & Run", "Compile and run the current .BAS file using QB64PE", CompileAndRunCommand),
                new("Open Help", "Show the QBasic IDE help window", ShowHelpCommand),
                new("Open Command Palette", "Show the command palette", OpenCommandPaletteCommand)
            };
        }

        #region Commands
        
        [RelayCommand] private async Task NewFile()
        {
            if (_editor == null) return;
            if (_hasUnsavedChanges && !await ConfirmDiscard()) return;

            _editor.Text = string.Empty;
            _currentFile = null;
            UpdateStatus(false);
        }

        [RelayCommand] private async Task OpenFile()
        {
            if (_window == null || !await ConfirmDiscard()) return;
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open BAS File",
                FileTypeFilter = new[] { new FilePickerFileType("BAS Files") { Patterns = new[] { "*.bas" } } },
                SuggestedStartLocation = start
            });
            if (result.FirstOrDefault()?.TryGetLocalPath() is { } path)
            {
                await LoadFile(path);
            }
        }
        
        [RelayCommand] private Task SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                return SaveFileAs();
            }
            return WriteTextToFileAsync(_currentFile);
        }

        [RelayCommand] private async Task SaveFileAs()
        {
            if (_window == null || _editor == null) return;
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save BAS File As...",
                FileTypeChoices = new[] { new FilePickerFileType("BAS Files") { Patterns = new[] { "*.bas" } } },
                SuggestedFileName = Path.GetFileName(_currentFile) ?? "Untitled.bas",
                SuggestedStartLocation = start
             });

            if (result?.TryGetLocalPath() is { } path)
            {
                _currentFile = path;
                if (_window != null) _window.Title = $"QBasic Retro IDE - {Path.GetFileName(path)}";
                await WriteTextToFileAsync(path);
                UpdateProjectTree();
            }
        }
        
        [RelayCommand] private async Task CompileAndRun()
        {
            if (_isCompiling) return;
            _isCompiling = true;
            SetStatus("Compiling...");

            try
            {
                await SaveFile();
                if (string.IsNullOrEmpty(_currentFile)) return;
                if (!File.Exists(_qb64Path) && !IsCommandInPath(_qb64Path))
                {
                    SetStatus($"Error: QB64PE not found at '{_qb64Path}'. Please configure it in Tools -> Settings.");
                    return;
                }

                var result = await Cli.Wrap(_qb64Path)
                    .WithArguments($"-c \"{_currentFile}\"")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();
                
                if (result.ExitCode != 0)
                {
                    SetStatus($"Compilation failed. See QB64PE output for details.");
                    return;
                }

                SetStatus("Compilation finished. Running...");
                var exePath = Path.ChangeExtension(_currentFile, OperatingSystem.IsWindows() ? "exe" : null);
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    await Cli.Wrap(exePath).WithStandardOutputPipe(PipeTarget.ToDelegate(l => Dispatcher.UIThread.InvokeAsync(() => SetStatus(l))))
                                           .WithStandardErrorPipe(PipeTarget.ToDelegate(l => Dispatcher.UIThread.InvokeAsync(() => SetStatus($"ERROR: {l}"))))
                                           .ExecuteAsync();
                    SetStatus("Execution finished.");
                }
                else { SetStatus("Compilation failed: Executable not found."); }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                Logger.Log($"QB64 compile error: {ex.Message}");
            }
            finally
            {
                _isCompiling = false;
                UpdateStatus();
            }
        }
        
        [RelayCommand] private Task ShowHelp()
        {
            if (_window == null) return Task.CompletedTask;
            const string helpText = 
@"QBasic Retro IDE Help

Common Shortcuts:
- F5: Compile & Run
- Ctrl+S: Save
- Ctrl+O: Open
- Ctrl+N: New File
- Ctrl+Shift+P: Open Command Palette

Features:
- Edit .BAS files with basic syntax highlighting.
- Use the File menu to manage files and projects.
- Use the Run menu to compile and execute your code.
- Set the path to your qb64pe.exe in 'Tools' -> 'Settings...'.
- The Command Palette gives you quick access to all major actions.
";

            var helpWindow = new Window
            {
                Title = "QBasic IDE Help",
                Width = 450,
                Height = 350,
                Content = new TextBox
                {
                    Text = helpText,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10)
                }
            };
            ThemeManager.ApplyForPlugin(helpWindow, this);
            helpWindow.ShowDialog(_window);
            return Task.CompletedTask;
        }

        [RelayCommand] private async Task OpenCommandPalette()
        {
            if (_window == null) return;
            var paletteWindow = new CommandPaletteWindow(_ideCommands);
            var selectedCommand = await paletteWindow.ShowDialog<IdeCommand?>(_window);

            if (selectedCommand?.Command.CanExecute(null) ?? false)
            {
                await selectedCommand.Command.ExecuteAsync(null);
            }
        }
        
        #endregion

        #region UI Construction
        private Menu BuildMenu()
        {
            // --- Examples Sub-Menu ---
            var helloWorldExample = new MenuItem { Header = "Hello World" };
            helloWorldExample.Click += (s, e) => 
            {
                const string exampleCode = 
@"' Hello World Example for Cycloside QBasic IDE
PRINT ""Hello, Cycloside World!""
PRINT ""This is running from an in-app example.""

FOR i = 1 TO 5
    PRINT ""Loop iteration: ""; i
NEXT i
";
                _editor?.SetCurrentValue(TextBox.TextProperty, exampleCode);
                _hasUnsavedChanges = true;
            };
            var examplesMenu = new MenuItem { Header = "_Examples", ItemsSource = new[] { helloWorldExample } };

            // --- Main Menu ---
            var fileItems = new object[]
            {
                new MenuItem { Header = "_New", InputGesture = new KeyGesture(Key.N, KeyModifiers.Control), Command = NewFileCommand },
                new MenuItem { Header = "_Open...", InputGesture = new KeyGesture(Key.O, KeyModifiers.Control), Command = OpenFileCommand },
                new Separator(),
                new MenuItem { Header = "_Save", InputGesture = new KeyGesture(Key.S, KeyModifiers.Control), Command = SaveFileCommand },
                new MenuItem { Header = "Save _As...", Command = SaveFileAsCommand },
                new Separator(),
                examplesMenu,
                new Separator(),
                new MenuItem { Header = "E_xit", Command = new Cycloside.Services.RelayCommand(() => _window?.Close()) }
            };

            var runItems = new object[]
            {
                new MenuItem { Header = "_Compile & Run", InputGesture = new KeyGesture(Key.F5), Command = CompileAndRunCommand },
            };

            var helpItems = new object[]
            {
                new MenuItem { Header = "_QBasic Help", Command = ShowHelpCommand }
            };

            var toolsItems = new object[]
            {
                new MenuItem { Header = "_Command Palette...", InputGesture = new KeyGesture(Key.P, KeyModifiers.Control | KeyModifiers.Shift), Command = OpenCommandPaletteCommand },
                new MenuItem { Header = "_Settings...", Command = new Cycloside.Services.RelayCommand(OpenSettings) },
            };
            
            return new Menu
            {
                ItemsSource = new object[]
                {
                    new MenuItem { Header = "_File", ItemsSource = fileItems },
                    new MenuItem { Header = "_Run", ItemsSource = runItems },
                    new MenuItem { Header = "_Tools", ItemsSource = toolsItems },
                    new MenuItem { Header = "_Help", ItemsSource = helpItems }
                }
            };
        }
        #endregion

        #region File Operations
        private async Task LoadFile(string path)
        {
            if (_editor == null) return;
            try
            {
                SetStatus($"Loading {Path.GetFileName(path)}...");
                _currentFile = path;
                _editor.Text = await File.ReadAllTextAsync(path);
                _hasUnsavedChanges = false;
                if (_window != null) _window.Title = $"QBasic Retro IDE - {Path.GetFileName(path)}";
                UpdateStatus(false);
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading file: {ex.Message}");
            }
        }

        private async Task WriteTextToFileAsync(string path)
        {
            if (_editor == null) return;
            try
            {
                SetStatus($"Saving {path}...");
                await File.WriteAllTextAsync(path, _editor.Text);
                UpdateStatus(false); // No longer modified
                SetStatus($"Saved successfully.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving file: {ex.Message}");
            }
        }
        
        private void UpdateProjectTree()
        {
            if (_projectTree == null || string.IsNullOrWhiteSpace(_projectPath)) return;
            var rootNode = new TreeViewItem { Header = Path.GetFileName(_projectPath), IsExpanded = true };
            try
            {
                var items = Directory.GetFiles(_projectPath, "*.bas")
                    .Select(f => new TreeViewItem { Header = Path.GetFileName(f), Tag = f })
                    .ToList<object>();
                rootNode.ItemsSource = items;
                _projectTree.ItemsSource = new[] { rootNode };
            }
            catch (Exception ex)
            {
                SetStatus($"Error reading project directory: {ex.Message}");
            }
        }
        #endregion

        #region UI Logic and Event Handlers
        private void UpdateStatus(bool? modified = null)
        {
            if (_status == null || _editor == null || _isCompiling) return;
            if (modified.HasValue)
                _hasUnsavedChanges = modified.Value;
            var line = _editor.TextArea.Caret.Line;
            var col = _editor.TextArea.Caret.Column;
            var file = string.IsNullOrWhiteSpace(_currentFile) ? "Untitled" : Path.GetFileName(_currentFile);
            var modIndicator = _hasUnsavedChanges ? "*" : "";

            _status.Text = $"Ln {line}, Col {col}  |  {file}{modIndicator}";
        }

        private void SetStatus(string message)
        {
            if (_status != null) Dispatcher.UIThread.InvokeAsync(() => _status.Text = message);
        }

        private async void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check for Command Palette hotkey
            if (e.Key == Key.P && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                await OpenCommandPalette();
                e.Handled = true;
                return;
            }

            // Standard F5 handling
            if (e.Key == Key.F5 && !_isCompiling)
            {
               await CompileAndRun();
               e.Handled = true;
            }
        }
        
        private void OpenSettings()
        {
            if (_window == null || _editor == null) return;
            var settingsWindow = new IdeSettingsWindow(_qb64Path, _editor.FontSize);
            settingsWindow.ShowDialog(_window);

            if (settingsWindow.Result)
            {
                _qb64Path = settingsWindow.QB64Path;
                _editor.FontSize = settingsWindow.FontSize;
                SettingsManager.Settings.QB64Path = _qb64Path;
                SettingsManager.Save();
            }
        }

        private async Task<bool> ConfirmDiscard()
        {
            if (_window == null) return false;
            var confirm = new ConfirmationWindow("Unsaved Changes", "Discard current changes?");
            return await confirm.ShowDialog<bool>(_window);
        }
        
        private static bool IsCommandInPath(string command)
        {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
            if (paths == null) return false;
            var extensions = OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? new[] { ".exe" } : new[] { "" };
            return paths.SelectMany(p => extensions.Select(e => Path.Combine(p, command + e))).Any(File.Exists);
        }
        #endregion

        #region Helper Windows
        // This is a new helper window for the Command Palette feature
        private class CommandPaletteWindow : Window
        {
            private readonly ListBox _listBox;
            private readonly TextBox _searchBox;
            private readonly List<IdeCommand> _allCommands;

            public CommandPaletteWindow(List<IdeCommand> commands)
            {
                _allCommands = commands;

                Title = "Command Palette";
                Width = 400;
                SizeToContent = SizeToContent.Height;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                CanResize = false;

                var panel = new DockPanel { Margin = new Thickness(5) };
                
                _searchBox = new TextBox { Watermark = "Type a command..." };
                _searchBox.TextChanged += (s, e) => FilterList();
                _searchBox.KeyDown += (s, e) => 
                {
                    if (e.Key == Key.Enter) { SelectAndClose(); e.Handled = true; }
                    if (e.Key == Key.Escape) { Close(null); e.Handled = true; }
                };
                
                _listBox = new ListBox();
                _listBox.DoubleTapped += (s, e) => SelectAndClose();

                DockPanel.SetDock(_searchBox, Dock.Top);

                panel.Children.Add(_searchBox);
                panel.Children.Add(_listBox);

                Content = panel;

                FilterList(); // Initial population
                this.Opened += (s, e) => _searchBox.Focus();
            }

            private void FilterList()
            {
                var searchText = _searchBox.Text ?? "";
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _listBox.ItemsSource = _allCommands;
                }
                else
                {
                    _listBox.ItemsSource = _allCommands
                        .Where(c => c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                _listBox.SelectedIndex = 0;
            }

            private void SelectAndClose()
            {
                if (_listBox.SelectedItem is IdeCommand selected)
                {
                    Close(selected);
                }
            }
        }

        // Settings window remains largely the same
        private class IdeSettingsWindow : Window
        {
            private readonly TextBox _pathBox;
            private readonly TextBox _fontSizeBox;
            public bool Result { get; private set; } = false;
            public IdeSettingsWindow(string path, double fontSize)
            {
                Title = "IDE Settings";
                Width = 400;
                SizeToContent = SizeToContent.Height;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var panel = new StackPanel { Margin = new Thickness(10), Spacing = 5 };
                panel.Children.Add(new TextBlock { Text = "QB64PE Executable Path:" });
                var pathPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
                _pathBox = new TextBox { Text = path, Width = 250 };
                var browseButton = new Button { Content = "Browse..." };
                browseButton.Click += async (_, _) =>
                {
                    var start = await DialogHelper.GetDefaultStartLocationAsync(StorageProvider);
                    var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select QB64PE Executable",
                        SuggestedStartLocation = start
                    });
                    if (result.FirstOrDefault()?.TryGetLocalPath() is { } p)
                    {
                        _pathBox.Text = p;
                    }
                };
                pathPanel.Children.Add(_pathBox);
                pathPanel.Children.Add(browseButton);
                panel.Children.Add(pathPanel);
                panel.Children.Add(new TextBlock { Text = "Font Size:" });
                _fontSizeBox = new TextBox { Text = fontSize.ToString() };
                panel.Children.Add(_fontSizeBox);
                var ok = new Button { Content = "Save", IsDefault = true };
                ok.Click += (_, _) => { Result = true; Close(); };
                var cancel = new Button { Content = "Cancel", IsCancel = true };
                cancel.Click += (_, _) => Close();
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 5 };
                buttonPanel.Children.Add(ok);
                buttonPanel.Children.Add(cancel);
                panel.Children.Add(buttonPanel);
                Content = panel;
            }

            public string QB64Path => _pathBox.Text ?? "qb64pe";
            public new double FontSize => double.TryParse(_fontSizeBox.Text, out var f) && f > 0 ? f : 14;
        }

        private class ConfirmationWindow : Window
        {
            public ConfirmationWindow(string title, string message)
            {
                Title = title;
                Width = 350;
                SizeToContent = SizeToContent.Height;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                var messageBlock = new TextBlock { Text = message, Margin = new Thickness(15), TextWrapping = TextWrapping.Wrap };
                var yesButton = new Button { Content = "Yes", IsDefault = true, Margin = new Thickness(5) };
                yesButton.Click += (_, _) => Close(true);
                var noButton = new Button { Content = "No", IsCancel = true, Margin = new Thickness(5) };
                noButton.Click += (_, _) => Close(false);
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                buttonPanel.Children.Add(yesButton);
                buttonPanel.Children.Add(noButton);
                var mainPanel = new StackPanel { Spacing = 10 };
                mainPanel.Children.Add(messageBlock);
                mainPanel.Children.Add(buttonPanel);
                Content = mainPanel;
            }
        }
        #endregion
    }
}
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class QBasicRetroIDEPlugin : ObservableObject, IPlugin
    {
        private Window? _window;
        private TextEditor? _editor;
        private TreeView? _projectTree;
        private TextBlock? _status;
        private string _qb64Path = "qb64";
        private string? _currentFile;
        private string? _projectPath;
        private bool _isCompiling = false;
        private bool _hasUnsavedChanges = false;

        public string Name => "QBasic Retro IDE";
        public string Description => "Edit and run .BAS files using QB64 Phoenix";
        public Version Version => new Version(0, 3, 1);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _qb64Path = SettingsManager.Settings.ComponentSkins.TryGetValue("QB64Path", out var list) && list.Count > 0 && !string.IsNullOrWhiteSpace(list[0])
                ? list[0]
                : "qb64";

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
            _editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
            _editor.TextChanged += (_, _)
                =>
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
                Content = dock
            };

            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(QBasicRetroIDEPlugin));
            _window.KeyDown += Window_KeyDown;
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
        }

        // Commands exposed for UI bindings
        [RelayCommand]
        private async Task Save() => await SaveFile();

        [RelayCommand]
        private async Task Compile() => await CompileAndRun();

        #region UI Construction
        private Menu BuildMenu()
        {
            var newItem = new MenuItem { Header = "_New", InputGesture = new KeyGesture(Key.N, KeyModifiers.Control) };
            var openItem = new MenuItem { Header = "_Open...", InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) };
            var openProjectItem = new MenuItem { Header = "Open _Project..." };
            var saveItem = new MenuItem
            {
                Header = "_Save",
                InputGesture = new KeyGesture(Key.S, KeyModifiers.Control),
                Command = SaveCommand
            };
            var saveAsItem = new MenuItem { Header = "Save _As..." };
            var exitItem = new MenuItem { Header = "E_xit" };

            var fileItems = new object[]
            {
                newItem, openItem, openProjectItem,
                new Separator(),
                saveItem, saveAsItem,
                new Separator(),
                exitItem
            };

            newItem.Click += async (s, e) => await NewFile();
            openItem.Click += async (s, e) => await OpenFile();
            openProjectItem.Click += async (s, e) => await OpenProject();
            saveAsItem.Click += async (s, e) => await SaveFileAs();
            exitItem.Click += (s, e) => _window?.Close();

            var undoItem = new MenuItem { Header = "_Undo" };
            var redoItem = new MenuItem { Header = "_Redo" };
            var cutItem = new MenuItem { Header = "Cu_t" };
            var copyItem = new MenuItem { Header = "_Copy" };
            var pasteItem = new MenuItem { Header = "_Paste" };

            var editItems = new object[]
            {
                undoItem, redoItem, new Separator(),
                cutItem, copyItem, pasteItem
            };

            undoItem.Click += (s, e) => _editor?.Undo();
            redoItem.Click += (s, e) => _editor?.Redo();
            cutItem.Click += (s, e) => _editor?.Cut();
            copyItem.Click += (s, e) => _editor?.Copy();
            pasteItem.Click += (s, e) => _editor?.Paste();

            var searchItems = new[] { new MenuItem { Header = "_Find..." }, new MenuItem { Header = "_Replace..." } };
            searchItems[0].Click += async (s, e) => await Find();
            searchItems[1].Click += async (s, e) => await Replace();

            var runItems = new[]
            {
                new MenuItem
                {
                    Header = "_Compile & Run",
                    InputGesture = new KeyGesture(Key.F5),
                    Command = CompileCommand
                },
                new MenuItem { Header = "Run _Executable" }
            };
            runItems[1].Click += async (s, e) => await RunExecutable();

            var settingsItem = new MenuItem { Header = "_Settings..." };
            settingsItem.Click += (s, e) => OpenSettings();

            var helpItem = new MenuItem { Header = "_About" };
            helpItem.Click += (s, e) => ShowHelp();

            return new Menu
            {
                ItemsSource = new object[]
                {
                    new MenuItem { Header = "_File", ItemsSource = fileItems },
                    new MenuItem { Header = "_Edit", ItemsSource = editItems },
                    new MenuItem { Header = "_Search", ItemsSource = searchItems },
                    new MenuItem { Header = "_Run", ItemsSource = runItems },
                    new MenuItem { Header = "T_ools", ItemsSource = new [] { settingsItem } },
                    new MenuItem { Header = "_Help", ItemsSource = new [] { helpItem } }
                }
            };
        }
        #endregion

        #region File Operations
        private async Task NewFile()
        {
            if (_editor == null) return;

            if (_hasUnsavedChanges && _window != null)
            {
                var confirm = new ConfirmationWindow("Unsaved Changes",
                    "Discard current changes?");
                var result = await confirm.ShowDialog<bool>(_window);
                if (!result) return;
            }

            _editor.Text = string.Empty;
            _currentFile = null;
            UpdateStatus(false);
        }

        private async Task OpenProject()
        {
            if (_window == null) return;
            var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Open QBasic Project Folder" });
            var selectedFolder = result.FirstOrDefault();
            if (selectedFolder != null && selectedFolder.TryGetLocalPath() is { } path)
            {
                _projectPath = path;
                UpdateProjectTree();
            }
        }

        private async Task OpenFile()
        {
            if (_window == null) return;
            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open BAS File",
                FileTypeFilter = new[] { new FilePickerFileType("BAS Files") { Patterns = new[] { "*.bas" } } }
            });

            if (result.FirstOrDefault()?.TryGetLocalPath() is { } path)
            {
                if (_hasUnsavedChanges)
                {
                    var confirm = new ConfirmationWindow("Unsaved Changes", "Discard current changes?");
                    var cont = await confirm.ShowDialog<bool>(_window);
                    if (!cont) return;
                }

                await LoadFile(path);
            }
        }

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

        private async Task SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                await SaveFileAs();
                return;
            }
            if (_editor == null) return;

            try
            {
                SetStatus($"Saving {_currentFile}...");
                await File.WriteAllTextAsync(_currentFile, _editor.Text);
                UpdateStatus(false); // No longer modified
                SetStatus($"Saved successfully.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving file: {ex.Message}");
            }
        }

        private async Task SaveFileAs()
        {
            if (_window == null || _editor == null) return;
            var result = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save BAS File As...",
                FileTypeChoices = new[] { new FilePickerFileType("BAS Files") { Patterns = new[] { "*.bas" } } },
                SuggestedFileName = Path.GetFileName(_currentFile) ?? "Untitled.bas"
            });

            if (result?.TryGetLocalPath() is { } path)
            {
                _currentFile = path;
                if (_window != null) _window.Title = $"QBasic Retro IDE - {Path.GetFileName(path)}";
                await SaveFile();
                UpdateProjectTree();
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

        #region Compilation and Running
        private async Task CompileAndRun()
        {
            if (_isCompiling) return;

            _isCompiling = true;
            SetStatus("Compiling...");

            try
            {
                if (string.IsNullOrEmpty(_currentFile))
                {
                    await SaveFileAs(); // Force user to save first
                    if (string.IsNullOrEmpty(_currentFile)) return; // User cancelled
                }
                else
                {
                    await SaveFile();
                }

                // Check again in case saving failed or was cancelled
                if (string.IsNullOrEmpty(_currentFile)) return;

                await Cli.Wrap(_qb64Path)
                    .WithArguments($"\"{_currentFile}\"")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();

                SetStatus("Compilation finished. Running...");

                var exePath = Path.ChangeExtension(_currentFile, OperatingSystem.IsWindows() ? "exe" : null);
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    await Cli.Wrap(exePath).ExecuteAsync();
                    SetStatus("Execution finished.");
                }
                else
                {
                    SetStatus("Compilation failed: Executable not found.");
                }
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

        private async Task RunExecutable()
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                SetStatus("No file is open to run its executable.");
                return;
            }

            var exePath = Path.ChangeExtension(_currentFile, OperatingSystem.IsWindows() ? "exe" : null);
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                SetStatus($"Running {Path.GetFileName(exePath)}...");
                try
                {
                    await Cli.Wrap(exePath).ExecuteAsync();
                    SetStatus("Execution finished.");
                }
                catch (Exception ex)
                {
                    SetStatus($"Error running executable: {ex.Message}");
                }
            }
            else
            {
                SetStatus("Executable not found. Compile the file first (F5).");
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
            if (_status == null) return;
            // Use dispatcher to ensure UI update is on the correct thread
            Dispatcher.UIThread.InvokeAsync(() => _status.Text = message);
        }

        private async void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            // Use a try-catch block to prevent crashes from unhandled exceptions in async void
            try
            {
                // Key gestures are now handled by the MenuItems, this is a fallback/override
                if (e.Key == Key.F5)
                {
                    await CompileAndRun();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Critical Error: {ex.Message}");
            }
        }

        private async Task Find()
        {
            if (_window == null || _editor == null) return;
            var text = await ShowInputDialog("Find", "Text to find:");
            if (!string.IsNullOrEmpty(text))
            {
                // This is a very basic find, a real implementation would be more complex
                var index = _editor.Text.IndexOf(text, _editor.SelectionStart + _editor.SelectionLength, StringComparison.OrdinalIgnoreCase);
                if (index == -1) index = _editor.Text.IndexOf(text, 0, StringComparison.OrdinalIgnoreCase); // Wrap around

                if (index >= 0)
                {
                    _editor.Select(index, text.Length);
                    _editor.TextArea.Caret.BringCaretToView();
                }
                else
                {
                    SetStatus("Text not found.");
                }
            }
        }

        private async Task Replace()
        {
            if (_window == null || _editor == null) return;
            var findText = await ShowInputDialog("Replace", "Text to find:");
            if (string.IsNullOrEmpty(findText)) return;

            var replaceText = await ShowInputDialog("Replace With", "Replace with:");
            if (replaceText == null) return; // User cancelled

            _editor.Text = _editor.Text.Replace(findText, replaceText, StringComparison.OrdinalIgnoreCase);
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
                SettingsManager.Settings.ComponentSkins["QB64Path"] = new List<string> { _qb64Path };
                SettingsManager.Save();
            }
        }

        private void ShowHelp()
        {
            if (_window == null) return;
            // A proper about window would be better here
            var aboutWindow = new Window
            {
                Title = "About QBasic Retro IDE",
                Width = 400,
                Height = 200,
                Content = new TextBlock
                {
                    Text = "QBasic Retro IDE Plugin\n\nPowered by QB64 Phoenix Edition\nhttps://github.com/QB64-Phoenix-Edition/QB64pe",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            if (_window != null)
            {
                aboutWindow.ShowDialog(_window);
            }
        }

        private async Task<string?> ShowInputDialog(string title, string prompt)
        {
            var inputWindow = new InputWindow(title, prompt);
            if (_window != null)
            {
                return await inputWindow.ShowDialog<string?>(_window);
            }
            return null;
        }
        #endregion

        #region Helper Windows
        private class InputWindow : Window
        {
            private readonly TextBox _box = new();
            public InputWindow(string title, string prompt)
            {
                Title = title;
                Width = 300;
                SizeToContent = SizeToContent.Height;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var panel = new StackPanel { Margin = new Thickness(10), Spacing = 8 };
                panel.Children.Add(new TextBlock { Text = prompt });
                panel.Children.Add(_box);

                var ok = new Button { Content = "OK", IsDefault = true };
                ok.Click += (_, _) => Close(_box.Text);
                var cancel = new Button { Content = "Cancel", IsCancel = true };
                cancel.Click += (_, _) => Close(null);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 5 };
                buttonPanel.Children.Add(ok);
                buttonPanel.Children.Add(cancel);
                panel.Children.Add(buttonPanel);
                Content = panel;

                _box.AttachedToVisualTree += (s, e) => _box.Focus();
            }
        }

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
                panel.Children.Add(new TextBlock { Text = "QB64 Executable Path:" });
                _pathBox = new TextBox { Text = path };
                panel.Children.Add(_pathBox);
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

            public string QB64Path => _pathBox.Text ?? "qb64";
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

                var messageBlock = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(15),
                    TextWrapping = TextWrapping.Wrap
                };

                var yesButton = new Button { Content = "Yes", IsDefault = true, Margin = new Thickness(5) };
                yesButton.Click += (_, _) => Close(true);
                var noButton = new Button { Content = "No", IsCancel = true, Margin = new Thickness(5) };
                noButton.Click += (_, _) => Close(false);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
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
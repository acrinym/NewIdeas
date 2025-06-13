using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using Avalonia.Platform.Storage;
using CliWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

public class QBasicRetroIDEPlugin : IPlugin
{
    private Window? _window;
    private TextEditor? _editor;
    private TreeView? _projectTree;
    private TextBlock? _status;
    private string _qb64Path = "qb64";
    private string? _currentFile;
    private string? _projectPath;

    public static async Task RunCli(string file, string qb64Path = "qb64")
    {
        try
        {
            await Cli.Wrap(qb64Path).WithArguments($"\"{file}\"")
                .WithValidation(CommandResultValidation.None).ExecuteAsync();
            var exe = Path.ChangeExtension(file, OperatingSystem.IsWindows() ? "exe" : string.Empty);
            if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                await Cli.Wrap(exe).ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"QB64 CLI error: {ex.Message}");
        }
    }

    public string Name => "QBasic Retro IDE";
    public string Description => "Edit and run .BAS files using QB64 Phoenix";
    public Version Version => new(0,2,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _qb64Path = SettingsManager.Settings.ComponentThemes.TryGetValue("QB64Path", out var p) && !string.IsNullOrWhiteSpace(p)
            ? p : "qb64";

        _editor = new TextEditor
        {
            ShowLineNumbers = true,
            SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("VBNET") ?? HighlightingManager.Instance.GetDefinition("C#"),
            Background = new SolidColorBrush(Color.FromRgb(0, 0, 128)),
            Foreground = Brushes.Yellow,
            FontFamily = new FontFamily("Consolas"),
        };
        _editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
        _editor.TextChanged += (_, _) => UpdateStatus();

        _projectTree = new TreeView { Width = 180 };
        _projectTree.DoubleTapped += async (_, __) =>
        {
            if (_projectTree.SelectedItem is TreeViewItem item && item.Tag is string path)
                await LoadFile(path);
        };

        _status = new TextBlock { Foreground = Brushes.White, Margin = new Thickness(4, 0) };

        var menu = BuildMenu();

        var statusBar = new DockPanel { Height = 24, Background = Brushes.Navy };
        statusBar.Children.Add(_status);
        DockPanel.SetDock(statusBar, Dock.Bottom);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Children.Add(_projectTree);
        Grid.SetColumn(_projectTree, 0);
        grid.Children.Add(_editor);
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
        ThemeManager.ApplyFromSettings(_window, "Plugins");
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
    }

    private Menu BuildMenu()
    {
        var newItem = new MenuItem { Header = "_New" };
        newItem.Click += (_, _) => NewFile();
        var openItem = new MenuItem { Header = "_Open" };
        openItem.Click += async (_, _) => await OpenFile();
        var saveItem = new MenuItem { Header = "_Save" };
        saveItem.Click += async (_, _) => await SaveFile();
        var saveAsItem = new MenuItem { Header = "Save _As" };
        saveAsItem.Click += async (_, _) => await SaveFileAs();
        var projectItem = new MenuItem { Header = "Open _Project" };
        projectItem.Click += async (_, _) => await OpenProject();
        var exitItem = new MenuItem { Header = "E_xit" };
        exitItem.Click += (_, _) => _window?.Close();

        var fileMenu = new MenuItem { Header = "_File", ItemsSource = new object[] { newItem, openItem, saveItem, saveAsItem, new Separator(), projectItem, new Separator(), exitItem } };

        var undo = new MenuItem { Header = "_Undo" };
        undo.Click += (_, _) => _editor?.Undo();
        var redo = new MenuItem { Header = "_Redo" };
        redo.Click += (_, _) => _editor?.Redo();
        var cut = new MenuItem { Header = "Cu_t" };
        cut.Click += (_, _) => _editor?.Cut();
        var copy = new MenuItem { Header = "_Copy" };
        copy.Click += (_, _) => _editor?.Copy();
        var paste = new MenuItem { Header = "_Paste" };
        paste.Click += (_, _) => _editor?.Paste();

        var editMenu = new MenuItem { Header = "_Edit", ItemsSource = new object[] { undo, redo, new Separator(), cut, copy, paste } };

        var find = new MenuItem { Header = "_Find" };
        find.Click += async (_, _) => await Find();
        var replace = new MenuItem { Header = "_Replace" };
        replace.Click += async (_, _) => await Replace();

        var searchMenu = new MenuItem { Header = "_Search", ItemsSource = new object[] { find, replace } };

        var compile = new MenuItem { Header = "_Compile && Run" };
        compile.Click += async (_, _) => await CompileRun();
        var runExe = new MenuItem { Header = "_Run Executable" };
        runExe.Click += async (_, _) => await RunExecutable();
        var runMenu = new MenuItem { Header = "_Run", ItemsSource = new object[] { compile, runExe } };

        var settings = new MenuItem { Header = "_Settings" };
        settings.Click += (_, _) => OpenSettings();

        var helpItem = new MenuItem { Header = "_Help" };
        helpItem.Click += (_, _) => ShowHelp();
        var helpMenu = new MenuItem { Header = "_Help", ItemsSource = new object[] { helpItem } };

        return new Menu { ItemsSource = new object[] { fileMenu, editMenu, searchMenu, runMenu, settings, helpMenu } };
    }

    private async Task OpenProject()
    {
        if (_window == null) return;
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        var folder = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            _projectPath = folder;
            UpdateProjectTree();
        }
    }

    private void UpdateProjectTree()
    {
        if (_projectTree == null)
            return;
        if (string.IsNullOrWhiteSpace(_projectPath))
        {
            _projectTree.ItemsSource = null;
            return;
        }
        var root = new TreeViewItem { Header = Path.GetFileName(_projectPath), IsExpanded = true };
        var items = Directory.GetFiles(_projectPath, "*.bas").Select(f => new TreeViewItem { Header = Path.GetFileName(f), Tag = f }).ToList<object>();
        root.ItemsSource = items;
        _projectTree.ItemsSource = new[] { root };
    }

    private async Task LoadFile(string path)
    {
        if (_editor == null) return;
        _currentFile = path;
        _editor.Text = await File.ReadAllTextAsync(path);
        UpdateStatus();
    }

    private async Task SaveFile()
    {
        if (_currentFile == null)
        {
            await SaveFileAs();
            return;
        }
        if (_editor != null)
            await File.WriteAllTextAsync(_currentFile, _editor.Text);
    }

    private async Task SaveFileAs()
    {
        if (_window == null) return;
        var result = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = Path.GetFileName(_currentFile) ?? "program.bas",
            FileTypeChoices = new[] { new FilePickerFileType("BAS") { Patterns = new[] { "*.bas" } } }
        });
        var path = result?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path) && _editor != null)
        {
            _currentFile = path;
            await File.WriteAllTextAsync(path, _editor.Text);
            UpdateProjectTree();
        }
    }

    private async Task CompileRun()
    {
        if (_window == null || _editor == null) return;
        var source = _currentFile ?? Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bas");
        if (_currentFile == null)
            await File.WriteAllTextAsync(source, _editor.Text);
        try
        {
            await Cli.Wrap(_qb64Path).WithArguments($"\"{source}\"").WithValidation(CommandResultValidation.None).ExecuteAsync();
            var exe = Path.ChangeExtension(source, OperatingSystem.IsWindows() ? "exe" : "");
            if (File.Exists(exe))
                await Cli.Wrap(exe).ExecuteAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"QB64 compile error: {ex.Message}");
        }
    }

    private async Task RunExecutable()
    {
        if (_window == null) return;
        var exe = _currentFile == null
            ? null
            : Path.ChangeExtension(_currentFile, OperatingSystem.IsWindows() ? "exe" : "");
        if (exe != null && File.Exists(exe))
        {
            await Cli.Wrap(exe).ExecuteAsync();
        }
        else
        {
            await CompileRun();
        }
    }

    private void NewFile()
    {
        if (_editor == null) return;
        _editor.Text = string.Empty;
        _currentFile = null;
        UpdateStatus();
    }

    private async Task OpenFile()
    {
        if (_window == null) return;
        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("BAS") { Patterns = new[] { "*.bas" } } }
        });
        var file = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(file))
            await LoadFile(file);
    }

    private async Task Find()
    {
        if (_window == null || _editor == null) return;
        var dlg = new InputWindow("Find", "Text to find:");
        var text = await dlg.ShowDialog<string?>(_window);
        if (!string.IsNullOrEmpty(text))
        {
            var index = _editor.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                _editor.SelectionStart = index;
                _editor.SelectionLength = text.Length;
                _editor.TextArea.Caret.Offset = index + text.Length;
                _editor.TextArea.Caret.BringCaretToView();
            }
        }
    }

    private async Task Replace()
    {
        if (_window == null || _editor == null) return;
        var findDlg = new InputWindow("Find", "Find:");
        var find = await findDlg.ShowDialog<string?>(_window);
        if (string.IsNullOrEmpty(find)) return;
        var replaceDlg = new InputWindow("Replace", "Replace with:");
        var repl = await replaceDlg.ShowDialog<string?>(_window);
        if (repl == null) return;
        _editor.Text = _editor.Text.Replace(find, repl, StringComparison.OrdinalIgnoreCase);
    }

    private void OpenSettings()
    {
        if (_window == null) return;
        var win = new IdeSettingsWindow(_qb64Path, _editor?.FontSize ?? 14);
        var res = win.ShowDialog<bool>(_window).Result;
        if (res)
        {
            _qb64Path = win.QB64Path;
            if (_editor != null) _editor.FontSize = win.FontSize;
            SettingsManager.Settings.ComponentThemes["QB64Path"] = _qb64Path;
            SettingsManager.Save();
        }
    }

    private void ShowHelp()
    {
        if (_window != null)
        {
            Logger.Log("QB64 Phoenix Edition - https://github.com/QB64-Phoenix-Edition/QB64pe");
        }
    }

    private void UpdateStatus()
    {
        if (_status == null || _editor == null) return;
        var line = _editor.TextArea.Caret.Line;
        var col = _editor.TextArea.Caret.Column;
        var file = string.IsNullOrWhiteSpace(_currentFile) ? "Untitled" : Path.GetFileName(_currentFile);
        _status.Text = $"Ln {line} Col {col} - {file}";
    }

    private async void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            await CompileRun();
            e.Handled = true;
        }
        else if (e.Key == Key.O && e.KeyModifiers == KeyModifiers.Control)
        {
            await OpenFile();
            e.Handled = true;
        }
        else if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
        {
            await SaveFile();
            e.Handled = true;
        }
        else if (e.Key == Key.N && e.KeyModifiers == KeyModifiers.Control)
        {
            NewFile();
            e.Handled = true;
        }
    }

    private class InputWindow : Window
    {
        private readonly TextBox _box = new();
        public InputWindow(string title, string prompt)
        {
            Title = title;
            Width = 300;
            Height = 120;
            var panel = new StackPanel { Margin = new Thickness(8) };
            panel.Children.Add(new TextBlock { Text = prompt });
            panel.Children.Add(_box);
            var ok = new Button { Content = "OK", IsDefault = true };
            ok.Click += (_, _) => Close(_box.Text);
            var cancel = new Button { Content = "Cancel", IsCancel = true };
            cancel.Click += (_, _) => Close(null);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btns.Children.Add(ok);
            btns.Children.Add(cancel);
            panel.Children.Add(btns);
            Content = panel;
        }
    }

    private class IdeSettingsWindow : Window
    {
        private readonly TextBox _pathBox;
        private readonly TextBox _fontSizeBox;
        public IdeSettingsWindow(string path, double fontSize)
        {
            Title = "IDE Settings";
            Width = 400;
            Height = 150;
            var panel = new StackPanel { Margin = new Thickness(8) };
            panel.Children.Add(new TextBlock { Text = "QB64 Path:" });
            _pathBox = new TextBox { Text = path };
            panel.Children.Add(_pathBox);
            panel.Children.Add(new TextBlock { Text = "Font Size:" });
            _fontSizeBox = new TextBox { Text = fontSize.ToString() };
            panel.Children.Add(_fontSizeBox);
            var ok = new Button { Content = "OK", IsDefault = true };
            ok.Click += (_, _) => Close(true);
            var cancel = new Button { Content = "Cancel", IsCancel = true };
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btns.Children.Add(ok);
            btns.Children.Add(cancel);
            panel.Children.Add(btns);
            Content = panel;
        }

        public string QB64Path => _pathBox.Text;
        public double FontSize => double.TryParse(_fontSizeBox.Text, out var f) ? f : 14;
    }
}

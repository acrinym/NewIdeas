using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside;

public partial class SkinThemeEditorWindow : Window
{
    private const string ThemeScopeLabel = "Theme Packs";
    private const string SkinScopeLabel = "Skin Packs";

    private readonly Dictionary<string, string> _filePaths = new(StringComparer.OrdinalIgnoreCase);

    private ComboBox? _scopeBox;
    private ComboBox? _fileBox;
    private TextBlock? _selectedPathBlock;
    private TextEditor? _editor;
    private SkinPreviewWindow? _previewWindow;

    public SkinThemeEditorWindow()
    {
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(SkinThemeEditorWindow));

        _scopeBox = this.FindControl<ComboBox>("ScopeBox");
        _fileBox = this.FindControl<ComboBox>("FileBox");
        _selectedPathBlock = this.FindControl<TextBlock>("SelectedPathBlock");
        _editor = this.FindControl<TextEditor>("Editor");

        if (_editor != null)
        {
            _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        if (_scopeBox != null)
        {
            _scopeBox.ItemsSource = new[] { ThemeScopeLabel, SkinScopeLabel };
            _scopeBox.SelectedItem = ThemeScopeLabel;
            _scopeBox.SelectionChanged += ScopeChanged;
        }

        if (_fileBox != null)
        {
            _fileBox.SelectionChanged += FileChanged;
        }

        BuildFileList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ScopeChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        BuildFileList();
    }

    private void FileChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        LoadFile(null, null);
    }

    private void BuildFileList()
    {
        if (_fileBox == null)
        {
            return;
        }

        _filePaths.Clear();

        var scopeLabel = _scopeBox?.SelectedItem as string ?? ThemeScopeLabel;
        var rootDirectory = string.Equals(scopeLabel, SkinScopeLabel, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(AppContext.BaseDirectory, "Skins")
            : Path.Combine(AppContext.BaseDirectory, "Themes");

        if (!Directory.Exists(rootDirectory))
        {
            Directory.CreateDirectory(rootDirectory);
        }

        foreach (var entry in GetScopedFiles(rootDirectory))
        {
            _filePaths[entry.Key] = entry.Value;
        }

        _fileBox.ItemsSource = _filePaths.Keys.ToArray();
        _fileBox.SelectedItem = _filePaths.Keys.FirstOrDefault();

        if (_fileBox.SelectedItem == null)
        {
            if (_editor != null)
            {
                _editor.Text = string.Empty;
            }

            if (_selectedPathBlock != null)
            {
                _selectedPathBlock.Text = "No theme or skin files found in the selected scope.";
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> GetScopedFiles(string rootDirectory)
    {
        var extensionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".axaml",
            ".json"
        };

        var filePaths = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories)
            .Where(path => extensionSet.Contains(Path.GetExtension(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in filePaths)
        {
            var relativePath = Path.GetRelativePath(rootDirectory, filePath)
                .Replace('\\', '/');
            yield return new KeyValuePair<string, string>(relativePath, filePath);
        }
    }

    private string? GetSelectedPath()
    {
        if (_fileBox?.SelectedItem is not string displayPath)
        {
            return null;
        }

        return _filePaths.TryGetValue(displayPath, out var fullPath) ? fullPath : null;
    }

    private void LoadFile(object? sender, RoutedEventArgs? e)
    {
        _ = sender;
        _ = e;

        var path = GetSelectedPath();
        if (path == null || _editor == null)
        {
            return;
        }

        try
        {
            _editor.SyntaxHighlighting = string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)
                ? HighlightingManager.Instance.GetDefinition("JavaScript")
                : HighlightingManager.Instance.GetDefinition("XML");

            _editor.Text = File.ReadAllText(path);

            if (_selectedPathBlock != null)
            {
                _selectedPathBlock.Text = path;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"SkinThemeEditorWindow: failed to load '{path}': {ex.Message}");
            _editor.Text = ex.Message;

            if (_selectedPathBlock != null)
            {
                _selectedPathBlock.Text = path;
            }
        }
    }

    private void SaveFile(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var path = GetSelectedPath();
        if (path == null || _editor == null)
        {
            return;
        }

        try
        {
            File.WriteAllText(path, _editor.Text ?? string.Empty);
            Logger.Log($"SkinThemeEditorWindow: saved '{path}'");
        }
        catch (Exception ex)
        {
            Logger.Log($"SkinThemeEditorWindow: failed to save '{path}': {ex.Message}");
        }
    }

    private void PreviewSkin(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var path = GetSelectedPath();
        if (path == null || _editor == null)
        {
            return;
        }

        if (!string.Equals(Path.GetExtension(path), ".axaml", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Log("SkinThemeEditorWindow: preview is only available for .axaml files");
            return;
        }

        _previewWindow ??= new SkinPreviewWindow();
        _previewWindow.LoadPreview(_editor.Text ?? string.Empty);
        _previewWindow.Show();
        _previewWindow.Activate();
    }
}

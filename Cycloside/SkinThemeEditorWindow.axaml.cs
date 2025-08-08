using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using System;
using System.IO;
using System.Linq;
using Cycloside.Services;

namespace Cycloside;

public partial class SkinThemeEditorWindow : Window
{
    private ComboBox? _fileBox;
    private TextEditor? _editor;

    // FIX: The path now correctly points to the "Global" subdirectory where themes are stored.
    private string _themeDir = Path.Combine(AppContext.BaseDirectory, "Themes", "Global");

    public SkinThemeEditorWindow()
    {
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(SkinThemeEditorWindow));

        _fileBox = this.FindControl<ComboBox>("FileBox");
        _editor = this.FindControl<TextEditor>("Editor");

        if (_editor != null)
        {
            _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        BuildFileList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildFileList()
    {
        if (_fileBox == null) return;

        _fileBox.Items.Clear();
        if (!Directory.Exists(_themeDir))
        {
            // If the directory doesn't exist, create it. This prevents a crash on first run.
            try
            {
                Directory.CreateDirectory(_themeDir);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to create theme directory at '{_themeDir}': {ex.Message}");
                return;
            }
        }

        try
        {
            foreach (var file in Directory.GetFiles(_themeDir, "*.axaml"))
            {
                _fileBox.Items.Add(Path.GetFileName(file));
            }

            if (_fileBox.ItemCount > 0)
            {
                _fileBox.SelectedIndex = 0;
                // Automatically load the first file when the window opens.
                LoadFile(null, null);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to list theme files in '{_themeDir}': {ex.Message}");
        }
    }

    private string? GetSelectedPath()
    {
        if (_fileBox?.SelectedItem is not string fileName)
        {
            return null;
        }
        return Path.Combine(_themeDir, fileName);
    }

    private void LoadFile(object? sender, RoutedEventArgs? e)
    {
        var path = GetSelectedPath();
        if (path != null && File.Exists(path) && _editor != null)
        {
            try
            {
                _editor.Text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to read theme file '{path}': {ex.Message}");
                if (_editor != null) _editor.Text = $"Error: Could not load file.\n\n{ex.Message}";
            }
        }
    }

    private void SaveFile(object? sender, RoutedEventArgs e)
    {
        var path = GetSelectedPath();
        if (path != null && _editor != null)
        {
            try
            {
                File.WriteAllText(path, _editor.Text ?? string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save theme file '{path}': {ex.Message}");
            }
        }
    }

    private SkinPreviewWindow? _previewWindow;

    private void PreviewSkin(object? sender, RoutedEventArgs e)
    {
        if (_editor == null) return;

        _previewWindow ??= new SkinPreviewWindow();
        _previewWindow.LoadPreview(_editor.Text ?? string.Empty);
        _previewWindow.Show();
        _previewWindow.Activate();
    }
}

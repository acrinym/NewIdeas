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

/// <summary>
/// A completely rebuilt, stable window for editing theme files.
/// The crashing preview functionality has been removed in favor of stability.
/// </summary>
public partial class SkinThemeEditorWindow : Window
{
    private ComboBox? _fileBox;
    private TextEditor? _editor;
    private string _themeDir = Path.Combine(AppContext.BaseDirectory, "Themes");

    public SkinThemeEditorWindow()
    {
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(SkinThemeEditorWindow));
        
        // Find controls
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
            Directory.CreateDirectory(_themeDir);
            return;
        }

        foreach (var file in Directory.GetFiles(_themeDir, "*.axaml"))
        {
            _fileBox.Items.Add(Path.GetFileName(file));
        }

        if (_fileBox.ItemCount > 0)
        {
            _fileBox.SelectedIndex = 0;
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

    private void LoadFile(object? sender, RoutedEventArgs e)
    {
        var path = GetSelectedPath();
        if (path != null && File.Exists(path) && _editor != null)
        {
            _editor.Text = File.ReadAllText(path);
        }
    }

    private void SaveFile(object? sender, RoutedEventArgs e)
    {
        var path = GetSelectedPath();
        if (path != null && _editor != null)
        {
            File.WriteAllText(path, _editor.Text ?? string.Empty);
        }
    }
}

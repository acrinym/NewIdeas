using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Styling;
using System;
using System.IO;
using System.Linq;

namespace Cycloside;

public partial class SkinThemeEditorWindow : Window
{
    public SkinThemeEditorWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, "Plugins");
        CursorManager.ApplyFromSettings(this, "Plugins");
        SkinManager.LoadForWindow(this);
        // WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(SkinThemeEditorWindow));
        BuildFileList();
        BuildCursorList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    private void BuildFileList()
    {
        TypeBox.SelectionChanged += (_, _) => RefreshFiles();
        TypeBox.SelectedIndex = 0;
        RefreshFiles();
    }

    private void RefreshFiles()
    {
        FileBox.Items.Clear();
        var isTheme = TypeBox.SelectedIndex == 0;
        var dir = isTheme ? Path.Combine(AppContext.BaseDirectory, "Themes/Global") : Path.Combine(AppContext.BaseDirectory, "Skins");
        if (!Directory.Exists(dir))
            return;
        foreach (var file in Directory.GetFiles(dir, "*.axaml"))
            FileBox.Items.Add(Path.GetFileNameWithoutExtension(file));
        if (FileBox.ItemCount > 0)
            FileBox.SelectedIndex = 0;
    }

    private void BuildCursorList()
    {
        foreach (var name in Enum.GetNames(typeof(StandardCursorType)))
            CursorBox.Items.Add(name);
        CursorBox.SelectionChanged += (_, _) =>
        {
            if (CursorBox.SelectedItem is string n)
                CursorManager.ApplyCursor(CursorPreview, n);
        };
        CursorBox.SelectedIndex = 0;
    }

    private string? GetSelectedPath()
    {
        var isTheme = TypeBox.SelectedIndex == 0;
        var name = FileBox.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var dir = isTheme ? Path.Combine(AppContext.BaseDirectory, "Themes/Global") : Path.Combine(AppContext.BaseDirectory, "Skins");
        return Path.Combine(dir, name + ".axaml");
    }

    private void LoadFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var path = GetSelectedPath();
        if (path != null && File.Exists(path))
            Editor.Text = File.ReadAllText(path);
    }

    private void SaveFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var path = GetSelectedPath();
        if (path != null)
            File.WriteAllText(path, Editor.Text ?? string.Empty);
    }

    private void Preview(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var xaml = Editor.Text ?? string.Empty;
        var win = new PreviewWindow(xaml);
        win.Show();
    }
}

public class PreviewWindow : Window
{
    public PreviewWindow(string xaml)
    {
        Width = 300;
        Height = 200;
        Title = "Preview";
        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 4 };
        panel.Children.Add(new Button { Content = "Button" });
        panel.Children.Add(new TextBox { Text = "Sample" });
        Content = panel;
        // Live preview disabled when runtime loader unavailable
        // WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(PreviewWindow));
    }
}

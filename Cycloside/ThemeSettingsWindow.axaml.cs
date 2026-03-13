using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Cycloside.Plugins;
using Cycloside.Services;
using Avalonia.Layout;
using Avalonia.Media;

namespace Cycloside;

public partial class ThemeSettingsWindow : Window
{
    private readonly PluginManager _manager;
    private readonly string[] _themes;

    private readonly Dictionary<string, ComboBox> _componentComboBoxes = new();
    private ComboBox? _globalThemeBox;

    // FIX: Add a parameterless constructor for XAML designer support.
    // This resolves the AVLN3001 build warning.
    public ThemeSettingsWindow() : this(new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify))
    {
        // This constructor is used by the Avalonia designer and XAML loader.
        // It calls the main constructor with a temporary PluginManager instance.
    }

    public ThemeSettingsWindow(PluginManager manager)
    {
        _manager = manager;

        var globalDir = Path.Combine(AppContext.BaseDirectory, "Themes", "Global");
        var globalThemes = Directory.Exists(globalDir)
            ? Directory.GetFiles(globalDir, "*.axaml")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(s => s != null && !s.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .Select(s => s!)
                .ToArray()
            : Array.Empty<string>();
        var themePacks = ThemeManager.GetAvailableThemes().ToArray();
        _themes = themePacks.Length > 0
            ? themePacks.Union(globalThemes).OrderBy(t => t).ToArray()
            : globalThemes;

        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        BuildList();
        RefreshManifestForSelectedTheme();
        ThemeManager.ThemeChanged += OnThemeChanged;
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(ThemeSettingsWindow));
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        RefreshManifestForSelectedTheme();
    }

    private void RefreshManifestDisplay()
    {
        var content = this.FindControl<StackPanel>("ManifestContent");
        var panel = this.FindControl<Border>("ManifestPanel");
        if (content is null || panel is null) return;

        content.Children.Clear();
        var manifest = ThemeManager.CurrentManifest;
        if (manifest == null)
        {
            content.Children.Add(new TextBlock { Text = "No theme manifest (legacy theme)", TextWrapping = TextWrapping.Wrap, Foreground = Avalonia.Media.Brushes.Gray });
            return;
        }

        if (!string.IsNullOrEmpty(manifest.Name))
            content.Children.Add(new TextBlock { Text = $"Name: {manifest.Name}", FontWeight = FontWeight.SemiBold });
        if (!string.IsNullOrEmpty(manifest.Version))
            content.Children.Add(new TextBlock { Text = $"Version: {manifest.Version}" });
        if (!string.IsNullOrEmpty(manifest.Author))
            content.Children.Add(new TextBlock { Text = $"Author: {manifest.Author}" });
        if (!string.IsNullOrEmpty(manifest.Description))
        {
            var desc = new TextBlock { Text = manifest.Description, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 };
            content.Children.Add(new TextBlock { Text = "Description:", FontWeight = FontWeight.SemiBold });
            content.Children.Add(desc);
        }
        if (manifest.Tags?.Count > 0)
            content.Children.Add(new TextBlock { Text = $"Tags: {string.Join(", ", manifest.Tags)}", TextWrapping = TextWrapping.Wrap });
    }

    private void RefreshManifestForSelectedTheme()
    {
        var selected = _globalThemeBox?.SelectedItem as string;
        if (string.IsNullOrEmpty(selected)) return;
        var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes", selected);
        var manifest = Directory.Exists(themeDir) ? ThemeManifest.Load(themeDir) : null;
        var content = this.FindControl<StackPanel>("ManifestContent");
        if (content is null) return;
        content.Children.Clear();
        if (manifest == null)
        {
            content.Children.Add(new TextBlock { Text = "No theme manifest (legacy theme)", TextWrapping = TextWrapping.Wrap, Foreground = Avalonia.Media.Brushes.Gray });
            return;
        }
        if (!string.IsNullOrEmpty(manifest.Name))
            content.Children.Add(new TextBlock { Text = $"Name: {manifest.Name}", FontWeight = FontWeight.SemiBold });
        if (!string.IsNullOrEmpty(manifest.Version))
            content.Children.Add(new TextBlock { Text = $"Version: {manifest.Version}" });
        if (!string.IsNullOrEmpty(manifest.Author))
            content.Children.Add(new TextBlock { Text = $"Author: {manifest.Author}" });
        if (!string.IsNullOrEmpty(manifest.Description))
        {
            content.Children.Add(new TextBlock { Text = "Description:", FontWeight = FontWeight.SemiBold });
            content.Children.Add(new TextBlock { Text = manifest.Description, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 });
        }
        if (manifest.Tags?.Count > 0)
            content.Children.Add(new TextBlock { Text = $"Tags: {string.Join(", ", manifest.Tags)}", TextWrapping = TextWrapping.Wrap });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildList()
    {
        var panel = this.FindControl<StackPanel>("ThemePanel");
        if (panel is null) return;
        panel.Children.Clear();

        // --- Global Theme Setting ---
        panel.Children.Add(new TextBlock { Text = "Global Application Theme", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 4) });
        _globalThemeBox = new ComboBox { ItemsSource = _themes, SelectedItem = SettingsManager.Settings.GlobalTheme };
        _globalThemeBox.SelectionChanged += (_, _) => RefreshManifestForSelectedTheme();
        panel.Children.Add(_globalThemeBox);
        panel.Children.Add(new Separator { Margin = new Thickness(0, 10) });

        // --- Per-Component Theme Settings ---
        panel.Children.Add(new TextBlock { Text = "Component-Specific Themes (Overrides Global)", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 4) });
        var components = new List<string> { "MainWindow" };
        components.AddRange(_manager.Plugins.Select(p => p.Name));

        foreach (var comp in components.Distinct().OrderBy(c => c))
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            row.Children.Add(new TextBlock { Text = comp, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });

            var box = new ComboBox { ItemsSource = _themes.Prepend("(Global Theme)").ToList() };
            Grid.SetColumn(box, 1);
            row.Children.Add(box);

            if (SettingsManager.Settings.ComponentThemes.TryGetValue(comp, out var themeName))
            {
                box.SelectedItem = themeName;
            }
            else
            {
                box.SelectedIndex = 0;
            }

            panel.Children.Add(row);
            _componentComboBoxes[comp] = box;
        }
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_globalThemeBox?.SelectedItem is string globalTheme)
        {
            SettingsManager.Settings.GlobalTheme = globalTheme;
            ThemeManager.LoadGlobalTheme(globalTheme);
        }

        var map = SettingsManager.Settings.ComponentThemes;
        map.Clear();
        foreach (var (comp, box) in _componentComboBoxes)
        {
            if (box.SelectedItem is string themeName && box.SelectedIndex != 0)
            {
                map[comp] = themeName;
            }
        }

        SettingsManager.Save();

        var msg = new MessageWindow("Settings Saved", "Theme settings have been saved. Some changes may require an application restart to fully apply.");
        msg.ShowDialog(this);

        Close();
    }

    private class MessageWindow : Window
    {
        public MessageWindow(string title, string message)
        {
            Title = title;
            Width = 350;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var msg = new TextBlock
            {
                Text = message,
                Margin = new Thickness(15),
                TextWrapping = TextWrapping.Wrap
            };

            var ok = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(5) };
            ok.Click += (_, _) => Close();

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(msg);
            panel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { ok }
            });
            Content = panel;
        }
    }
}

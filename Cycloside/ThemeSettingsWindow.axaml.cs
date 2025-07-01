using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside;

public partial class ThemeSettingsWindow : Window
{
    private readonly PluginManager _manager;
    private readonly string[] _themes;
    
    // This dictionary now holds controls for both the global theme aand component-specific themes.
    private readonly Dictionary<string, ComboBox> _componentComboBoxes = new();
    private ComboBox? _globalThemeBox;

    public ThemeSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        
        var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes");
        _themes = Directory.Exists(themeDir)
            ? Directory.GetFiles(themeDir, "*.axaml")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(s => s != null)
                .Select(s => s!)
                .ToArray()
            : Array.Empty<string>();

        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        BuildList();
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(ThemeSettingsWindow));
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
        panel.Children.Add(new TextBlock { Text = "Global Application Theme", FontWeight = FontWeight.Bold, Margin = new Thickness(0,0,0,4) });
        _globalThemeBox = new ComboBox { ItemsSource = _themes, SelectedItem = SettingsManager.Settings.GlobalTheme };
        panel.Children.Add(_globalThemeBox);
        panel.Children.Add(new Separator{ Margin = new Thickness(0, 10)});

        // --- Per-Component Theme Settings ---
        panel.Children.Add(new TextBlock { Text = "Component-Specific Themes (Overrides Global)", FontWeight = FontWeight.Bold, Margin = new Thickness(0,0,0,4) });
        var components = new List<string> { "MainWindow" }; // Start with the main window
        components.AddRange(_manager.Plugins.Select(p => p.Name));

        foreach (var comp in components.Distinct().OrderBy(c => c))
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            row.Children.Add(new TextBlock { Text = comp, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            
            var box = new ComboBox { ItemsSource = _themes.Prepend("(Global Theme)").ToList() };
            Grid.SetColumn(box, 1);
            row.Children.Add(box);
            
            // Load saved setting for this component
            if (SettingsManager.Settings.ComponentThemes.TryGetValue(comp, out var themeName))
            {
                box.SelectedItem = themeName;
            }
            else
            {
                box.SelectedIndex = 0; // Default to "(Global Theme)"
            }

            panel.Children.Add(row);
            _componentComboBoxes[comp] = box;
        }
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Save Global Theme
        if (_globalThemeBox?.SelectedItem is string globalTheme)
        {
            SettingsManager.Settings.GlobalTheme = globalTheme;
            ThemeManager.LoadGlobalTheme(globalTheme); // Apply immediately
        }

        // Save Component Themes
        var map = SettingsManager.Settings.ComponentThemes;
        map.Clear();
        foreach (var (comp, box) in _componentComboBoxes)
        {
            if (box.SelectedItem is string themeName && box.SelectedIndex != 0) // Index 0 is "(Global Theme)"
            {
                map[comp] = themeName;
            }
        }
        
        SettingsManager.Save();
        
        // Inform the user a restart might be needed for all changes to apply
        var msg = new MessageWindow("Settings Saved", "Theme settings have been saved. Some changes may require an application restart to fully apply.");
        msg.ShowDialog(this);
        
        Close();
    }
}

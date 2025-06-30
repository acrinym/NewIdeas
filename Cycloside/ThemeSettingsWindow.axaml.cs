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
    private readonly Dictionary<string, (CheckBox cb, ComboBox box)> _controls = new();

    public ThemeSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        
        // Dynamically load theme names from the Themes/Global directory.
        // This is the more robust approach from the 'main' branch.
        _themes = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Themes/Global"))
            ? Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Themes/Global"), "*.axaml")
                .Select(f => Path.GetFileNameWithoutExtension(f) ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
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
        if (panel is null)
            return;
        panel.Children.Clear();

        // Dynamically build the list of components starting with the main app,
        // then adding all loaded plugins.
        var components = new List<string> { "Cycloside" };
        components.AddRange(_manager.Plugins.Select(p => p.Name));

        foreach (var comp in components.Distinct())
        {
            var row = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var cb = new CheckBox
            {
                Content = comp,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var box = new ComboBox { SelectedIndex = 0, Margin = new Thickness(4, 0, 0, 0) };

            foreach (var th in _themes)
                box.Items.Add(th);

            row.Children.Add(cb);
            row.Children.Add(box);
            panel.Children.Add(row);
            _controls[comp] = (cb, box);

            // Load the currently saved settings for this component.
            if (SettingsManager.Settings.ComponentSkins.TryGetValue(comp, out var skins) && skins.Count > 0)
            {
                cb.IsChecked = true;
                box.SelectedItem = skins[0];
            }
        }
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var map = SettingsManager.Settings.ComponentSkins;
        map.Clear();
        foreach (var (comp, pair) in _controls)
        {
            if (pair.cb.IsChecked == true)
                map[comp] = new List<string>
                {
                    pair.box.SelectedItem?.ToString() ?? _themes.FirstOrDefault() ?? "Default" // Fallback to "Default"
                };
        }
        SettingsManager.Save();
        Close();
    }
}

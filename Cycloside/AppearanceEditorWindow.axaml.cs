using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside;

public partial class AppearanceEditorWindow : Window
{
    private readonly Dictionary<string, ComboBox> _map = new();

    public AppearanceEditorWindow()
    {
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
        BuildThemeList();
        BuildComponentList();
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(AppearanceEditorWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildThemeList()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Themes", "Global");
        var box = this.FindControl<ComboBox>("ThemeBox");
        foreach (var file in Directory.GetFiles(dir, "*.axaml"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            box.Items.Add(name);
            if (SettingsManager.Settings.GlobalTheme == name)
                box.SelectedItem = name;
        }
        if (box.SelectedIndex < 0 && box.ItemCount > 0)
            box.SelectedIndex = 0;
    }

    private void BuildComponentList()
    {
        var panel = this.FindControl<StackPanel>("ComponentPanel");
        panel.Children.Clear();
        var comps = SettingsManager.Settings.ComponentSkins.Keys.ToList();
        if (!comps.Contains("Cycloside")) comps.Insert(0, "Cycloside");
        foreach (var comp in comps)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,4) };
            row.Children.Add(new TextBlock { Text = comp, Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            var box = new ComboBox { Width = 120 };
            foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Skins"), "*.axaml"))
                box.Items.Add(Path.GetFileNameWithoutExtension(file));
            if (SettingsManager.Settings.ComponentSkins.TryGetValue(comp, out var skins) && skins.Count > 0)
                box.SelectedItem = skins[0];
            row.Children.Add(box);
            panel.Children.Add(row);
            _map[comp] = box;
        }
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var box = this.FindControl<ComboBox>("ThemeBox");
        if (box.SelectedItem is string theme)
            SettingsManager.Settings.GlobalTheme = theme;
        var map = SettingsManager.Settings.ComponentSkins;
        map.Clear();
        foreach (var (comp, combo) in _map)
        {
            if (combo.SelectedItem is string skin)
                map[comp] = new List<string> { skin };
        }
        SettingsManager.Save();
        Close();
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;

namespace Cycloside;

public partial class ThemeSettingsWindow : Window
{
    private readonly string[] _components = new[] { "Cycloside", "TextEditor", "MediaPlayer", "Plugins" };
    private readonly string[] _themes = new[] { "MintGreen", "Matrix", "Orange", "ConsoleGreen" };
    private readonly Dictionary<string, (CheckBox cb, ComboBox box)> _controls = new();

    public ThemeSettingsWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, "Plugins");
        CursorManager.ApplyFromSettings(this, "Plugins");
        BuildList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildList()
    {
        var panel = this.FindControl<StackPanel>("ThemePanel");
        panel.Children.Clear();
        foreach (var comp in _components)
        {
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(0,0,0,4) };
            var cb = new CheckBox { Content = comp, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            var box = new ComboBox { SelectedIndex = 0, Margin = new Thickness(4,0,0,0) };
            foreach (var th in _themes)
                box.Items.Add(th);
            row.Children.Add(cb);
            row.Children.Add(box);
            panel.Children.Add(row);
            _controls[comp] = (cb, box);
            if (SettingsManager.Settings.ComponentThemes.TryGetValue(comp, out var theme))
            {
                cb.IsChecked = true;
                box.SelectedItem = theme;
            }
        }
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var map = SettingsManager.Settings.ComponentThemes;
        map.Clear();
        foreach (var (comp, pair) in _controls)
        {
            if (pair.cb.IsChecked == true)
                map[comp] = pair.box.SelectedItem?.ToString() ?? _themes[0];
        }
        SettingsManager.Save();
        Close();
    }
}


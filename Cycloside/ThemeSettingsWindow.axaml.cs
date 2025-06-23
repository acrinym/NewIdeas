using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using Cycloside.Services;

namespace Cycloside;

public partial class ThemeSettingsWindow : Window
{
    private readonly string[] _components = new[] { "Cycloside", "TextEditor", "MediaPlayer", "Plugins" };
    private readonly string[] _themes = new[] { "MintGreen", "Matrix", "Orange", "ConsoleGreen", "MonochromeOrange", "DeepBlue" };
    private readonly Dictionary<string, (CheckBox cb, ComboBox box)> _controls = new();

    public ThemeSettingsWindow()
    {
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
        foreach (var comp in _components)
        {
var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
var cb = new CheckBox { Content = comp, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
var box = new ComboBox { SelectedIndex = 0, Margin = new Thickness(4, 0, 0, 0) };

            foreach (var th in _themes)
                box.Items.Add(th);
            row.Children.Add(cb);
            row.Children.Add(box);
            panel.Children.Add(row);
            _controls[comp] = (cb, box);
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
                map[comp] = new List<string> { pair.box.SelectedItem?.ToString() ?? _themes[0] };
        }
        SettingsManager.Save();
        Close();
    }
}


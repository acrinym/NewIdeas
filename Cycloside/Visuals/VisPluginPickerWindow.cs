using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.Generic;
using System.Linq;
using Cycloside.Services;

namespace Cycloside.Visuals;

public class VisPluginPickerWindow : Window
{
    private readonly VisPluginManager _manager;
    private readonly ListBox _list = new();

    public VisPluginPickerWindow(VisPluginManager manager)
    {
        _manager = manager;
        Title = "Select Visualization";
        Width = 300;
        Height = 200;

        var start = new Button { Content = "Start", HorizontalAlignment = HorizontalAlignment.Right };
        start.Click += (_, _) => StartSelected();
        _list.DoubleTapped += (_, _) => StartSelected();
        _list.ItemsSource = manager.Plugins.Select(p => p.Description).ToList();

        var panel = new StackPanel { Margin = new Thickness(10) };
        panel.Children.Add(_list);
        panel.Children.Add(start);
        Content = panel;

        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(VisPluginPickerWindow));
    }

    private void StartSelected()
    {
        if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _manager.Plugins.Count)
        {
            var plugin = _manager.Plugins[_list.SelectedIndex];
            _manager.StartPlugin(plugin);
            Close();
        }
    }
}

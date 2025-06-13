using Avalonia.Controls;
using System;
using System.Collections;
using System.Collections.ObjectModel;

namespace Cycloside.Plugins.BuiltIn;

public class EnvironmentEditorPlugin : IPlugin
{
    private Window? _window;
    private DataGrid? _grid;
    private ObservableCollection<EnvItem> _items = new();

    public string Name => "Environment Editor";
    public string Description => "Edit environment variables";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            _items.Add(new EnvItem { Key = de.Key.ToString()!, Value = de.Value?.ToString() ?? string.Empty });

        _grid = new DataGrid
        {
            ItemsSource = _items,
            AutoGenerateColumns = true
        };
        var saveButton = new Button { Content = "Save" };
        saveButton.Click += (_, __) =>
        {
            foreach (var item in _items)
                Environment.SetEnvironmentVariable(item.Key, item.Value);
        };

        var panel = new StackPanel();
        panel.Children.Add(_grid);
        panel.Children.Add(saveButton);

        _window = new Window
        {
            Title = "Environment Variables",
            Width = 600,
            Height = 400,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(EnvironmentEditorPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        _grid = null;
        _items.Clear();
    }

    private class EnvItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

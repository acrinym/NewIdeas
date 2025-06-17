using Avalonia.Controls;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class LogViewerPlugin : IPlugin
{
    private Window? _window;
    private TextBox? _box;
    private FileSystemWatcher? _watcher;
    private string? _file;
    private string? _filter;

    public string Name => "Log Viewer";
    public string Description => "Tail and filter log files";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _box = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            Height = 300
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_box, ScrollBarVisibility.Auto);

        var filterBox = new TextBox { Watermark = "Filter" };
        filterBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                _filter = filterBox.Text; // safest, direct value
                Reload();
            }
        };

        var openButton = new Button { Content = "Open Log" };
        openButton.Click += async (_, __) =>
        {
            var dlg = new OpenFileDialog();
            var files = await dlg.ShowAsync(_window!);
            if (files is { Length: > 0 } && File.Exists(files[0]))
            {
                _file = files[0];
                StartTailing(_file);
            }
        };

        var panel = new StackPanel();
        panel.Children.Add(openButton);
        panel.Children.Add(filterBox);
        panel.Children.Add(_box);

        _window = new Window
        {
            Title = "Log Viewer",
            Width = 600,
            Height = 400,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(LogViewerPlugin));
        _window.Show();
    }

    private void StartTailing(string file)
    {
        _box!.Text = File.ReadAllText(file);
        _watcher?.Dispose();
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(file)!, Path.GetFileName(file))
        {
            EnableRaisingEvents = true
        };
        _watcher.Changed += (_, __) => Reload();
    }

    private void Reload()
    {
        if (_file != null && File.Exists(_file))
        {
            var lines = File.ReadAllLines(_file);
            if (!string.IsNullOrWhiteSpace(_filter))
                lines = lines.Where(l => l.Contains(_filter!, StringComparison.OrdinalIgnoreCase)).ToArray();
            _box!.Text = string.Join(Environment.NewLine, lines);
        }
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
        _window?.Close();
        _window = null;
        _box = null;
    }
}

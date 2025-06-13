using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class FileWatcherPlugin : IPlugin
{
    private Window? _window;
    private TextBox? _log;
    private FileSystemWatcher? _watcher;

    public string Name => "File Watcher";
    public string Description => "Watch a folder for changes";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public async void Start()
    {
        var selectButton = new Button { Content = "Select Folder" };
        _log = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            Height = 300
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_log, ScrollBarVisibility.Auto);
        selectButton.Click += async (_, __) =>
        {
            if (_window == null) return;
            var folders = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            var path = folders.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
                StartWatching(path);
        };
        var panel = new StackPanel();
        panel.Children.Add(selectButton);
        panel.Children.Add(_log);

        _window = new Window
        {
            Title = "File Watcher",
            Width = 400,
            Height = 350,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(FileWatcherPlugin));
        _window.Show();
    }

    private void StartWatching(string path)
    {
        _watcher?.Dispose();
        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _watcher.Created += (_, e) => Log($"Created: {e.FullPath}");
        _watcher.Deleted += (_, e) => Log($"Deleted: {e.FullPath}");
        _watcher.Changed += (_, e) => Log($"Changed: {e.FullPath}");
        _watcher.Renamed += (_, e) => Log($"Renamed: {e.OldFullPath} -> {e.FullPath}");
        Log($"Watching {path}");
    }

    private void Log(string msg)
    {
        _log!.Text += msg + Environment.NewLine;
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
        _window?.Close();
        _window = null;
        _log = null;
    }
}

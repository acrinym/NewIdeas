using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using Cycloside;

namespace Cycloside.Plugins.BuiltIn;

/// <summary>
/// Simple plugin that changes the desktop wallpaper. All platform specific
/// behavior is implemented in <see cref="WallpaperHelper"/> so this plugin
/// only delegates to that helper.
/// </summary>
public class WallpaperPlugin : IPlugin
{
    private Window? _window;
    private Action<object?>? _wallpaperHandler;

    public string Name => "Wallpaper Changer";
    public string Description => "Change desktop wallpaper";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        var button = new Button { Content = "Select Wallpaper" };
        button.Click += async (_, _) =>
        {
            if (_window == null) return;
            var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                FileTypeFilter = new[] { new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.png", "*.bmp" } } }
            });
            var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (!string.IsNullOrEmpty(path))
                SetWallpaper(path);
        };
        _wallpaperHandler = o =>
        {
            if (o is string path && File.Exists(path))
                SetWallpaper(path);
        };
        PluginBus.Subscribe("wallpaper:set", _wallpaperHandler);
        _window = new Window
        {
            Title = "Wallpaper",
            Width = 200,
            Height = 80,
            Content = button
        };
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(WallpaperPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        if(_wallpaperHandler != null)
        {
            PluginBus.Unsubscribe("wallpaper:set", _wallpaperHandler);
            _wallpaperHandler = null;
        }
    }

    /// <summary>
    /// Changes the desktop wallpaper by delegating to <see cref="WallpaperHelper"/>.
    /// The helper contains platform-specific logic for Windows, macOS and common
    /// Linux desktop environments.
    /// </summary>
    private void SetWallpaper(string path)
    {
        WallpaperHelper.SetWallpaper(path);
    }
}

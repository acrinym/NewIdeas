using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cycloside.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Simple plugin that changes the desktop wallpaper. All platform-specific
    /// behavior is implemented in <see cref="WallpaperHelper"/> so this plugin
    /// only delegates to that helper.
    /// </summary>
    public class WallpaperPlugin : IPlugin
    {
        private WallpaperWindow? _window;
        private TextBlock? _statusBlock;
        private Action<object?>? _wallpaperHandler;

        public string Name => "Wallpaper Changer";
        public string Description => "Change desktop wallpaper";
        public Version Version => new Version(0, 2, 0); // Incremented for improvements
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.DesktopCustomization;

        public void Start()
        {
            _window = new WallpaperWindow();
            _statusBlock = _window.FindControl<TextBlock>("StatusBlock");
            var selectButton = _window.FindControl<Button>("SelectButton");

            if (selectButton != null)
                selectButton.Click += async (_, _) => await SelectAndSetWallpaperAsync();

            // --- Set up PluginBus handler ---
            _wallpaperHandler = payload =>
            {
                if (payload is string path && !string.IsNullOrEmpty(path))
                {
                    SetWallpaper(path);
                }
            };
            PluginBus.Subscribe("wallpaper:set", _wallpaperHandler);
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(WallpaperPlugin));
            _window.Show();
        }

        /// <summary>
        /// Opens a file picker and sets the wallpaper if a valid image is chosen.
        /// </summary>
        private async Task SelectAndSetWallpaperAsync()
        {
            if (_window == null) return;

            // Use the modern, recommended StorageProvider API to open a file picker.
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Wallpaper Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" }
                    }
                },
                SuggestedStartLocation = start
            });

            var selectedFile = result.FirstOrDefault();
            if (selectedFile?.TryGetLocalPath() is { } path)
            {
                SetWallpaper(path);
            }
        }

        /// <summary>
        /// Changes the desktop wallpaper by delegating to <see cref="WallpaperHelper"/>
        /// and provides user feedback.
        /// </summary>
        private void SetWallpaper(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                SetStatus("Error: Invalid file path.");
                return;
            }

            try
            {
                SetStatus($"Setting wallpaper to {Path.GetFileName(path)}...");
                // The WallpaperHelper contains the actual platform-specific logic.
                WallpaperHelper.SetWallpaper(path);
                SetStatus("Wallpaper changed successfully!");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                // Optionally log the full error for debugging
                Logger.Log($"WallpaperPlugin failed: {ex}");
            }
        }

        /// <summary>
        /// Updates the status message in a thread-safe way.
        /// </summary>
        private void SetStatus(string message)
        {
            if (_statusBlock == null) return;
            // Ensure UI updates are always on the UI thread.
            Dispatcher.UIThread.InvokeAsync(() => _statusBlock.Text = message);
        }


        public void Stop()
        {
            _window?.Close();
            _window = null;
            _statusBlock = null;

            if (_wallpaperHandler != null)
            {
                PluginBus.Unsubscribe("wallpaper:set", _wallpaperHandler);
                _wallpaperHandler = null;
            }
        }
    }
}
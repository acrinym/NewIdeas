using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
        private Window? _window;
        private TextBlock? _statusBlock;
        private Action<object?>? _wallpaperHandler;

        public string Name => "Wallpaper Changer";
        public string Description => "Change desktop wallpaper";
        public Version Version => new Version(0, 2, 0); // Incremented for improvements
        public Widgets.IWidget? Widget => null;

        public void Start()
        {
            // --- Create UI Controls ---
            var selectButton = new Button 
            { 
                Content = "Select Wallpaper Image",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            selectButton.Click += async (s, e) => await SelectAndSetWallpaperAsync();

            _statusBlock = new TextBlock
            {
                Text = "Ready to select an image.",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5)
            };

            // --- Assemble UI Layout ---
            var mainPanel = new DockPanel();
            DockPanel.SetDock(_statusBlock, Dock.Bottom);
            mainPanel.Children.Add(_statusBlock);
            mainPanel.Children.Add(selectButton); // Fills the remaining center space

            // --- Set up PluginBus handler ---
            _wallpaperHandler = (payload) =>
            {
                if (payload is string path && !string.IsNullOrEmpty(path))
                {
                    SetWallpaper(path);
                }
            };
            PluginBus.Subscribe("wallpaper:set", _wallpaperHandler);

            // --- Create and Show Window ---
            _window = new Window
            {
                Title = "Wallpaper Changer",
                Width = 250,
                Height = 120,
                Content = mainPanel
            };
            
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
                }
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

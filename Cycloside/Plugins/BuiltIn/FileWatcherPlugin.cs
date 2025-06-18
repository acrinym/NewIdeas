using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class FileWatcherPlugin : IPlugin
    {
        private Window? _window;
        private TextBox? _log;
        private Button? _selectFolderButton;
        private FileSystemWatcher? _watcher;

        public string Name => "File Watcher";
        public string Description => "Watch a folder for changes";
        public Version Version => new Version(0, 2, 0);
        public Widgets.IWidget? Widget => null;

        public void Start()
        {
            // --- Create UI Controls ---
            _selectFolderButton = new Button
            {
                Content = "Select Folder to Watch",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(5)
            };
            _selectFolderButton.Click += async (s, e) => await SelectAndWatchDirectoryAsync();

            var clearLogButton = new Button
            {
                Content = "Clear Log",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(5, 0, 5, 5)
            };
            clearLogButton.Click += (s, e) => { if (_log != null) _log.Text = string.Empty; };

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            buttonPanel.Children.Add(_selectFolderButton);
            buttonPanel.Children.Add(clearLogButton);

            _log = new TextBox
            {
                AcceptsReturn = true,
                IsReadOnly = true,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap, // Better for file paths
                Margin = new Avalonia.Thickness(5)
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(_log, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(_log, ScrollBarVisibility.Auto);

            // --- Assemble UI Layout ---
            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Top);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(_log); // The TextBox will fill the remaining space

            // --- Create and Show Window ---
            _window = new Window
            {
                Title = "File Watcher",
                Width = 550,
                Height = 450,
                Content = mainPanel
            };

            // Apply theming and effects
            ThemeManager.ApplyFromSettings(_window, "Plugins");
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(FileWatcherPlugin));
            _window.Show();
        }

        /// <summary>
        /// Handles the folder selection and initiates the watcher.
        /// </summary>
        private async Task SelectAndWatchDirectoryAsync()
        {
            if (_window == null) return;

            // Use the modern, recommended StorageProvider API to open a folder picker.
            var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a folder to watch",
                AllowMultiple = false
            });

            var selectedFolder = result.FirstOrDefault();
            var path = selectedFolder?.TryGetLocalPath();

            if (!string.IsNullOrWhiteSpace(path))
            {
                StartWatching(path);
            }
        }

        /// <summary>
        /// Sets up the FileSystemWatcher for the specified path.
        /// </summary>
        private void StartWatching(string path)
        {
            // Dispose of any existing watcher before creating a new one.
            _watcher?.Dispose();

            try
            {
                _watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true,
                    // NotifyFilters can be adjusted to watch for specific changes
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true
                };

                // Hook into the events
                _watcher.Created += (s, e) => Log($"[CREATED] {e.Name}");
                _watcher.Deleted += (s, e) => Log($"[DELETED] {e.Name}");
                _watcher.Changed += (s, e) => Log($"[CHANGED] {e.Name}");
                _watcher.Renamed += (s, e) => Log($"[RENAMED] {e.OldName} -> {e.Name}");
                _watcher.Error += (s, e) => Log($"[ERROR] Watcher error: {e.GetException().Message}");

                Log($"Now watching: {path}");
                if (_selectFolderButton != null) _selectFolderButton.Content = "Change Watched Folder";
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Could not start watcher on '{path}'. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs a message to the TextBox in a thread-safe way.
        /// FileSystemWatcher events fire on background threads, so UI updates must be dispatched
        /// back to the UI thread to prevent the application from crashing.
        /// </summary>
        private void Log(string msg)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_log == null) return;

                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _log.Text += $"[{timestamp}] {msg}{Environment.NewLine}";
                _log.CaretIndex = _log.Text.Length; // Auto-scroll to the end
            });
        }

        public void Stop()
        {
            _watcher?.Dispose();
            _watcher = null;
            _window?.Close();
            _window = null;
            _log = null;
            _selectFolderButton = null;
        }
    }
}
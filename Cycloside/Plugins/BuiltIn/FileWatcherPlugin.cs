using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public class FileWatcherPlugin : IPlugin
    {
        private FileWatcherWindow? _window;
        private TextBox? _log;
        private Button? _selectFolderButton;
        private FileSystemWatcher? _watcher;

        public string Name => "File Watcher";
        public string Description => "Watch a folder for changes";
        public Version Version => new Version(0, 2, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new FileWatcherWindow();
            _selectFolderButton = _window.FindControl<Button>("SelectFolderButton");
            var clearLogButton = _window.FindControl<Button>("ClearLogButton");
            var saveLogButton = _window.FindControl<Button>("SaveLogButton");
            _log = _window.FindControl<TextBox>("LogBox");

            _selectFolderButton?.AddHandler(Button.ClickEvent, async (s, e) => await SelectAndWatchDirectoryAsync());
            clearLogButton?.AddHandler(Button.ClickEvent, (s, e) => { if (_log != null) _log.Text = string.Empty; });
            saveLogButton?.AddHandler(Button.ClickEvent, async (s, e) => await SaveLogAsync());

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
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a folder to watch",
                AllowMultiple = false,
                SuggestedStartLocation = start
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

        /// <summary>
        /// Saves the current log text to a user-selected file.
        /// </summary>
        private async Task SaveLogAsync()
        {
            if (_window == null || _log == null) return;

            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Log As...",
                SuggestedFileName = $"watch_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } } },
                SuggestedStartLocation = start
            });

            if (file?.Path.LocalPath != null)
            {
                try
                {
                    await File.WriteAllTextAsync(file.Path.LocalPath, _log.Text);
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] Could not save file: {ex.Message}");
                }
            }
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
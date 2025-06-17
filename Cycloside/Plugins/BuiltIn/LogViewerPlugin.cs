using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class LogViewerPlugin : IPlugin
    {
        private Window? _window;
        private TextBox? _logBox;
        private FileSystemWatcher? _watcher;
        private string? _currentFilePath;
        private long _lastReadPosition = 0;

        // In-memory cache for performance
        private readonly List<string> _allLines = new List<string>();
        private string _currentFilter = string.Empty;

        public string Name => "Log Viewer";
        public string Description => "Tail and filter log files in real-time";
        public Version Version => new Version(0, 2, 0); // Incremented version for improvements
        public Widgets.IWidget? Widget => null;

        public void Start()
        {
            // --- Create UI Controls ---
            var openButton = new Button { Content = "Open Log File" };
            openButton.Click += async (s, e) => await SelectAndLoadFileAsync();

            var filterBox = new TextBox { Watermark = "Filter (case-insensitive)" };
            filterBox.TextChanged += (s, e) =>
            {
                _currentFilter = filterBox.Text ?? string.Empty;
                UpdateDisplayedLog();
            };

            var optionsPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Avalonia.Thickness(5) };
            var autoScrollCheck = new CheckBox { Content = "Auto-Scroll", IsChecked = true, Margin = new Avalonia.Thickness(5, 0) };
            var wrapLinesCheck = new CheckBox { Content = "Wrap Lines", IsChecked = false, Margin = new Avalonia.Thickness(5, 0) };
            wrapLinesCheck.IsCheckedChanged += (s, e) =>
            {
                if (_logBox != null)
                {
                    _logBox.TextWrapping = (e.IsChecked ?? false)
                        ? Avalonia.Media.TextWrapping.Wrap
                        : Avalonia.Media.TextWrapping.NoWrap;
                }
            };
            optionsPanel.Children.Add(autoScrollCheck);
            optionsPanel.Children.Add(wrapLinesCheck);

            _logBox = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                Margin = new Avalonia.Thickness(5)
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(_logBox, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(_logBox, ScrollBarVisibility.Auto);

            // --- Assemble UI Layout ---
            var topPanel = new DockPanel { Margin = new Avalonia.Thickness(5) };
            DockPanel.SetDock(openButton, Dock.Left);
            topPanel.Children.Add(openButton);
            topPanel.Children.Add(filterBox); // Fills remaining space

            var mainPanel = new DockPanel();
            DockPanel.SetDock(topPanel, Dock.Top);
            DockPanel.SetDock(optionsPanel, Dock.Top);
            mainPanel.Children.Add(topPanel);
            mainPanel.Children.Add(optionsPanel);
            mainPanel.Children.Add(_logBox); // Fills bottom

            // --- Create and Show Window ---
            _window = new Window
            {
                Title = "Log Viewer",
                Width = 700,
                Height = 500,
                Content = mainPanel
            };

            _logBox.TextChanged += (s, e) =>
            {
                if (autoScrollCheck.IsChecked == true)
                {
                    _logBox.CaretIndex = _logBox.Text?.Length ?? 0;
                }
            };

            ThemeManager.ApplyFromSettings(_window, "Plugins");
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(LogViewerPlugin));
            _window.Show();
        }

        private async Task SelectAndLoadFileAsync()
        {
            if (_window == null) return;

            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a log file to view",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.TextAll }
            });

            var path = result.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                _currentFilePath = path;
                await LoadInitialFileAsync();
                StartWatching();
            }
        }

        /// <summary>
        /// Reads the entire file content on a background thread to keep the UI responsive.
        /// </summary>
        private async Task LoadInitialFileAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || _logBox == null) return;

            _logBox.Text = $"Loading '{Path.GetFileName(_currentFilePath)}'...";
            _allLines.Clear();
            _lastReadPosition = 0;

            await Task.Run(() =>
            {
                try
                {
                    // Use a stream to handle large files gracefully.
                    using var fs = new FileStream(_currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.UTF8);

                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        _allLines.Add(line);
                    }
                    _lastReadPosition = fs.Position;
                }
                catch (Exception ex)
                {
                    // Dispatch error message back to the UI thread.
                    LogOnUIThread($"Error loading file: {ex.Message}");
                }
            });

            UpdateDisplayedLog();
        }

        /// <summary>
        /// Sets up the FileSystemWatcher to tail the current file for changes.
        /// </summary>
        private void StartWatching()
        {
            _watcher?.Dispose();
            if (string.IsNullOrEmpty(_currentFilePath)) return;

            var directory = Path.GetDirectoryName(_currentFilePath);
            var fileName = Path.GetFileName(_currentFilePath);

            if (directory == null || fileName == null) return;

            try
            {
                _watcher = new FileSystemWatcher(directory, fileName)
                {
                    EnableRaisingEvents = true
                };
                _watcher.Changed += async (s, e) => await OnFileChangedAsync();
            }
            catch (Exception ex)
            {
                LogOnUIThread($"[ERROR] Could not start watcher. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// When the file changes, reads only the new content from the last known position.
        /// </summary>
        private async Task OnFileChangedAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath)) return;

            await Task.Run(() =>
            {
                try
                {
                    using var fs = new FileStream(_currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (fs.Length < _lastReadPosition) // File was likely truncated or rewritten
                    {
                        _lastReadPosition = 0;
                        _allLines.Clear();
                    }

                    fs.Seek(_lastReadPosition, SeekOrigin.Begin);
                    using var sr = new StreamReader(fs, Encoding.UTF8);

                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        _allLines.Add(line);
                    }
                    _lastReadPosition = fs.Position;
                }
                catch (IOException) { /* Ignore read errors if file is in use, will retry on next change */ }
                catch (Exception ex)
                {
                    LogOnUIThread($"[ERROR] reading file change: {ex.Message}");
                }
            });

            UpdateDisplayedLog();
        }

        /// <summary>
        /// Applies the current filter to the in-memory lines and updates the TextBox.
        /// This method is thread-safe.
        /// </summary>
        private void UpdateDisplayedLog()
        {
            var filteredLines = string.IsNullOrWhiteSpace(_currentFilter)
                ? _allLines
                : _allLines.Where(l => l.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase));

            var textToShow = string.Join(Environment.NewLine, filteredLines);

            // Dispatch the final text update to the UI thread.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_logBox != null)
                {
                    _logBox.Text = textToShow;
                }
            });
        }

        private void LogOnUIThread(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_logBox != null)
                {
                    var fullMessage = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                    _logBox.AppendText(fullMessage);
                }
            });
        }

        public void Stop()
        {
            _watcher?.Dispose();
            _watcher = null;
            _window?.Close();
            _window = null;
            _logBox = null;
        }
    }
}
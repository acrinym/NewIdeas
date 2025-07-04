using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class LogViewerPlugin : IPlugin, IDisposable
    {
        private LogViewerWindow? _window;
        private TextBox? _logBox;
        private CheckBox? _autoScrollCheck;
        private FileSystemWatcher? _watcher;
        private string? _currentFilePath;
        private long _lastReadPosition = 0;
        private readonly List<string> _allLines = new List<string>();
        private string _currentFilter = string.Empty;

        private readonly SemaphoreSlim _fileReadLock = new SemaphoreSlim(1, 1);

        // --- NEW: A property to hold a filter term on startup ---
        public string InitialFilter { get; set; } = string.Empty;
        
        public string Name => "Log Viewer";
        public string Description => "Tail and filter log files in real-time. Now with auto-loading and saving!";
        public Version Version => new Version(0, 6, 0); // Incremented for new features
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;
        
        private string GetLogDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cycloside", "logs");
            }
            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "Cycloside");
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cycloside", "logs");
        }

        public void Start()
        {
            _window = new LogViewerWindow();
            
            var openButton = new Button { Content = "Open Log File" };
            openButton.Click += async (s, e) => await SelectAndLoadFileAsync();

            var saveButton = new Button { Content = "Save Log As...", Margin = new Avalonia.Thickness(5,0) };
            saveButton.Click += async (s, e) => await SaveLogAsync();

            var filterBox = _window.FindControl<TextBox>("FilterBox");
            if (filterBox != null)
            {
                filterBox.Watermark = "Filter (case-insensitive)";
                filterBox.TextChanged += (s, e) =>
                {
                    _currentFilter = filterBox.Text ?? string.Empty;
                    UpdateDisplayedLog();
                };

                // **MODIFIED: Apply the initial filter when the window starts**
                if (!string.IsNullOrWhiteSpace(InitialFilter))
                {
                    filterBox.Text = InitialFilter;
                    InitialFilter = string.Empty; // Reset so it only applies once
                }
            }

            var optionsPanel = _window.FindControl<StackPanel>("OptionsPanel") ?? new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Avalonia.Thickness(5) };
            _autoScrollCheck = new CheckBox { Content = "Auto-Scroll", IsChecked = true, Margin = new Avalonia.Thickness(5, 0) };
            var wrapLinesCheck = new CheckBox { Content = "Wrap Lines", IsChecked = false, Margin = new Avalonia.Thickness(5, 0) };
            wrapLinesCheck.IsCheckedChanged += (_, _) =>
            {
                if (_logBox != null)
                {
                    _logBox.TextWrapping = wrapLinesCheck.IsChecked == true
                        ? Avalonia.Media.TextWrapping.Wrap
                        : Avalonia.Media.TextWrapping.NoWrap;
                }
            };
            optionsPanel.Children.Add(_autoScrollCheck);
            optionsPanel.Children.Add(wrapLinesCheck);

            _logBox = _window.FindControl<TextBox>("LogBox");
            if (_logBox != null)
            {
                _logBox.IsReadOnly = true;
                _logBox.AcceptsReturn = true;
                _logBox.TextWrapping = Avalonia.Media.TextWrapping.NoWrap;
                _logBox.FontFamily = "Cascadia Code,Consolas,Menlo,monospace";
                _logBox.Margin = new Avalonia.Thickness(5);
                ScrollViewer.SetHorizontalScrollBarVisibility(_logBox, ScrollBarVisibility.Auto);
                ScrollViewer.SetVerticalScrollBarVisibility(_logBox, ScrollBarVisibility.Auto);
                _logBox.TextChanged += OnLogBoxTextChanged;
            }

            var topPanel = _window.FindControl<DockPanel>("TopPanel");
            var buttonPanel = _window.FindControl<StackPanel>("ButtonPanel");
            var mainOptions = _window.FindControl<StackPanel>("OptionsPanel");

            buttonPanel?.Children.Add(openButton);
            buttonPanel?.Children.Add(saveButton);
            if (optionsPanel != null && mainOptions != null)
            {
                foreach (var child in optionsPanel.Children)
                {
                    mainOptions.Children.Add(child);
                }
            }

            _window.Show();
            Dispatcher.UIThread.InvokeAsync(AttemptToLoadDefaultLogAsync);
        }
        
        private void OnLogBoxTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_autoScrollCheck?.IsChecked == true && _logBox != null)
            {
                _logBox.CaretIndex = _logBox.Text?.Length ?? 0;
            }
        }
        
        private async Task AttemptToLoadDefaultLogAsync()
        {
            var logDir = GetLogDirectory();
            if (!Directory.Exists(logDir))
            {
                LogOnUIThread($"[INFO] Log directory not found at '{logDir}'. Please open a file manually.");
                return;
            }

            var latestLogFile = new DirectoryInfo(logDir)
                .GetFiles("*.log")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
            
            if (latestLogFile != null)
            {
                await LoadFile(latestLogFile.FullName);
            }
            else
            {
                LogOnUIThread($"[INFO] No .log files found in '{logDir}'. Please open a file manually.");
            }
        }

        private async Task LoadFile(string path)
        {
            _watcher?.Dispose();
            _currentFilePath = path;
            if (!string.IsNullOrWhiteSpace(_currentFilePath) && File.Exists(_currentFilePath))
            {
                await LoadInitialFileAsync();
                StartWatching();
            }
        }

        private async Task SelectAndLoadFileAsync()
        {
            if (_window == null) return;
            var logDir = GetLogDirectory();
            var startLocation = await _window.StorageProvider.TryGetFolderFromPathAsync(logDir);

            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a log file to view",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Log Files") { Patterns = new[] { "*.log", "*.txt" } }, FilePickerFileTypes.All },
                SuggestedStartLocation = startLocation
            });
            var file = result.FirstOrDefault();
            if (file?.Path.LocalPath != null)
            {
                await LoadFile(file.Path.LocalPath);
            }
        }

        private async Task SaveLogAsync()
        {
            if (_window == null || _logBox == null) return;
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Log As...",
                SuggestedFileName = $"log_snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } } },
                SuggestedStartLocation = start
            });
            
            if (file?.Path.LocalPath != null)
            {
                try
                {
                    var textToSave = _logBox.Text;
                    await File.WriteAllTextAsync(file.Path.LocalPath, textToSave);
                }
                catch (Exception ex)
                {
                    LogOnUIThread($"[ERROR] Could not save file: {ex.Message}");
                }
            }
        }

        private async Task LoadInitialFileAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || _logBox == null) return;
            await _fileReadLock.WaitAsync();
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => _logBox.Text = $"Loading '{Path.GetFileName(_currentFilePath)}'...");
                _allLines.Clear();
                _lastReadPosition = 0;

                await Task.Run(() =>
                {
                    try
                    {
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
                        LogOnUIThread($"[ERROR] Error loading file: {ex.Message}");
                    }
                });
                UpdateDisplayedLog();
            }
            finally
            {
                _fileReadLock.Release();
            }
        }

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
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _watcher.Changed += async (s, e) => await OnFileChangedAsync();
            }
            catch (Exception ex)
            {
                LogOnUIThread($"[ERROR] Could not start watcher. Reason: {ex.Message}");
            }
        }

        private async Task OnFileChangedAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !_fileReadLock.Wait(0)) return;
            try
            {
                List<string> newLines = new List<string>();
                await Task.Run(() =>
                {
                    try
                    {
                        using var fs = new FileStream(_currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        if (fs.Length < _lastReadPosition)
                        {
                            _allLines.Clear();
                            _lastReadPosition = 0;
                        }

                        fs.Seek(_lastReadPosition, SeekOrigin.Begin);
                        using var sr = new StreamReader(fs, Encoding.UTF8);
                        string? line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            newLines.Add(line);
                        }
                        _lastReadPosition = fs.Position;
                    }
                    catch (IOException) { /* Ignore read errors */ }
                    catch (Exception ex)
                    {
                        newLines.Add($"[ERROR] reading file change: {ex.Message}");
                    }
                });
                
                if (newLines.Any())
                {
                    _allLines.AddRange(newLines);
                    UpdateDisplayedLog();
                }
            }
            finally
            {
                _fileReadLock.Release();
            }
        }

        private void UpdateDisplayedLog()
        {
            var sb = new StringBuilder();
            IEnumerable<string> filteredLines = _allLines;

            if (!string.IsNullOrWhiteSpace(_currentFilter))
            {
                filteredLines = _allLines.Where(l => l.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var line in filteredLines)
            {
                sb.AppendLine(line);
            }

            var textToShow = sb.ToString();
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
            var fullMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _allLines.Add(fullMessage);
            UpdateDisplayedLog();
        }

        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _fileReadLock.Dispose();
            _window?.Close();
            _window = null;
            _watcher = null;
            _logBox = null;
            GC.SuppressFinalize(this);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class TextEditorPlugin : IPlugin
    {
        private Window? _window;
        private TextBox? _editorBox;
        private TextBlock? _statusBlock;
        private string? _currentFilePath;

        public string Name => "Text Editor";
        public string Description => "Simple Markdown/text editor";
        public Version Version => new Version(0, 2, 0); // Incremented for improvements
        public Widgets.IWidget? Widget => null;

        public void Start()
        {
            // --- Create UI Controls ---
            var newButton = new Button { Content = "New" };
            newButton.Click += (s, e) => NewFile();

            var openButton = new Button { Content = "Open..." };
            openButton.Click += async (s, e) => await OpenFileAsync();

            var saveButton = new Button { Content = "Save" };
            saveButton.Click += async (s, e) => await SaveFileAsync();

            var saveAsButton = new Button { Content = "Save As..." };
            saveAsButton.Click += async (s, e) => await SaveFileAsAsync();

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(5)
            };
            buttonPanel.Children.Add(newButton);
            buttonPanel.Children.Add(openButton);
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(saveAsButton);

            _editorBox = new TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                FontFamily = new FontFamily("monospace"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 0, 5, 5)
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(_editorBox, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(_editorBox, ScrollBarVisibility.Auto);

            _statusBlock = new TextBlock
            {
                Text = "Ready",
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };

            var statusBar = new Border
            {
                Background = Brushes.CornflowerBlue,
                Child = _statusBlock,
                Height = 24
            };

            // --- Assemble UI Layout using a DockPanel ---
            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Top);
            DockPanel.SetDock(statusBar, Dock.Bottom);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(statusBar);
            mainPanel.Children.Add(_editorBox); // Fills the remaining space

            _window = new Window
            {
                Title = "Cycloside Editor",
                Width = 700,
                Height = 550,
                Content = mainPanel
            };

            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TextEditorPlugin));
            _window.Show();
        }

        private void NewFile()
        {
            if (_editorBox == null) return;
            // TODO: Add a check for unsaved changes before clearing
            _editorBox.Text = string.Empty;
            _currentFilePath = null;
            SetStatus("New file created.");
            UpdateWindowTitle();
        }

        private async Task OpenFileAsync()
        {
            if (_window == null) return;

            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.TextAll }
            });

            var selectedFile = result.FirstOrDefault();
            if (selectedFile?.TryGetLocalPath() is { } path)
            {
                SetStatus($"Opening {selectedFile.Name}...");
                // Perform file reading on a background thread to prevent UI freezing
                string content = await Task.Run(() => File.ReadAllTextAsync(path));

                if (_editorBox != null)
                {
                    _editorBox.Text = content;
                    _currentFilePath = path;
                    SetStatus($"Successfully opened {selectedFile.Name}.");
                    UpdateWindowTitle(selectedFile.Name);
                }
            }
        }

        private async Task SaveFileAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                // If the file has never been saved, this is a "Save As" operation
                await SaveFileAsAsync();
            }
            else
            {
                await WriteTextToFileAsync(_currentFilePath);
            }
        }

        private async Task SaveFileAsAsync()
        {
            if (_window == null) return;

            var result = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Text File As...",
                SuggestedFileName = Path.GetFileName(_currentFilePath) ?? "Untitled.txt",
                FileTypeChoices = new[] { FilePickerFileTypes.TextAll }
            });

            if (result?.TryGetLocalPath() is { } path)
            {
                _currentFilePath = path;
                await WriteTextToFileAsync(path);
                UpdateWindowTitle(result.Name);
            }
        }

        /// <summary>
        /// Helper method to write content to a file on a background thread.
        /// </summary>
        private async Task WriteTextToFileAsync(string path)
        {
            if (_editorBox == null) return;

            SetStatus($"Saving {Path.GetFileName(path)}...");
            try
            {
                string content = _editorBox.Text ?? string.Empty;
                // Perform file writing on a background thread to prevent UI freezing
                await Task.Run(() => File.WriteAllTextAsync(path, content));
                SetStatus($"File saved successfully.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving file: {ex.Message}");
            }
        }

        private void SetStatus(string message)
        {
            if (_statusBlock == null) return;
            // Use dispatcher to ensure UI update is on the correct thread
            Dispatcher.UIThread.InvokeAsync(() => _statusBlock.Text = message);
        }

        private void UpdateWindowTitle(string? fileName = null)
        {
            if (_window == null) return;

            _window.Title = string.IsNullOrEmpty(fileName)
                ? "Cycloside Editor - Untitled"
                : $"Cycloside Editor - {fileName}";
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
            _editorBox = null;
            _statusBlock = null;
        }
    }
}
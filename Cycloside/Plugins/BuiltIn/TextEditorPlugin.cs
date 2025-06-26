using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cycloside; // Assuming ConfirmationWindow and IPlugin are in this namespace or a sub-namespace
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class TextEditorPlugin : IPlugin
    {
        // --- Fields ---
        private TextEditorWindow? _window;
        private TextBox? _editorBox;
        private TextBlock? _statusBlock;
        private string? _currentFilePath;
        private string _lastSavedText = string.Empty;

        // --- IPlugin Properties ---
        public string Name => "Text Editor";
        public string Description => "A simple Markdown and text editor.";
        public Version Version => new Version(0, 4, 0); // Incremented for refactoring and optimization
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Plugin Lifecycle Methods ---
        public void Start()
        {
            _window = new TextEditorWindow();
            var buttonPanel = _window.FindControl<StackPanel>("ButtonPanel");
            _editorBox = _window.FindControl<TextBox>("EditorBox");
            _statusBlock = _window.FindControl<TextBlock>("StatusBlock");

            var newButton = new Button { Content = "New" };
            newButton.Click += async (s, e) => await NewFileAsync();
            var openButton = new Button { Content = "Open..." };
            openButton.Click += async (s, e) => await OpenFileAsync();
            var saveButton = new Button { Content = "Save" };
            saveButton.Click += async (s, e) => await SaveFileAsync();
            var saveAsButton = new Button { Content = "Save As..." };
            saveAsButton.Click += async (s, e) => await SaveFileAsAsync();

            if (buttonPanel != null)
            {
                buttonPanel.Children.Add(newButton);
                buttonPanel.Children.Add(openButton);
                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(saveAsButton);
            }

            if (_editorBox != null)
            {
                _editorBox.AcceptsReturn = true;
                _editorBox.AcceptsTab = true;
                _editorBox.FontFamily = new FontFamily("monospace");
                _editorBox.TextWrapping = TextWrapping.Wrap;
                _editorBox.Margin = new Thickness(5, 0, 5, 5);
                ScrollViewer.SetHorizontalScrollBarVisibility(_editorBox, ScrollBarVisibility.Auto);
                ScrollViewer.SetVerticalScrollBarVisibility(_editorBox, ScrollBarVisibility.Auto);
            }

            if (_statusBlock != null)
            {
                _statusBlock.Text = "Ready";
                _statusBlock.Margin = new Thickness(5);
                _statusBlock.VerticalAlignment = VerticalAlignment.Center;
            }

            _window.Show();
            UpdateWindowTitle(); // Set initial title
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
            _editorBox = null;
            _statusBlock = null;
        }

        // --- File Operation Logic ---

        private async Task NewFileAsync()
        {
            if (!await CanProceedWithUnsavedChanges()) return;

            if (_editorBox != null)
            {
                _editorBox.Text = string.Empty;
            }
            _currentFilePath = null;
            _lastSavedText = string.Empty;
            SetStatus("New file created.");
            UpdateWindowTitle();
        }

        private async Task OpenFileAsync()
        {
            if (_window == null || _editorBox == null || !await CanProceedWithUnsavedChanges()) return;

            var openResult = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.All }
            });

            var selectedFile = openResult?.FirstOrDefault();
            if (selectedFile?.TryGetLocalPath() is { } path)
            {
                SetStatus($"Opening {selectedFile.Name}...");
                try
                {
                    // Directly await the async file operation. No need for Task.Run.
                    string content = await File.ReadAllTextAsync(path);

                    _editorBox.Text = content;
                    _currentFilePath = path;
                    _lastSavedText = content;
                    SetStatus($"Successfully opened {selectedFile.Name}.");
                    UpdateWindowTitle(selectedFile.Name);
                }
                catch (Exception ex)
                {
                    SetStatus($"Error opening file: {ex.Message}");
                }
            }
        }

        private async Task SaveFileAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
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

            var saveResult = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Text File As...",
                SuggestedFileName = Path.GetFileName(_currentFilePath) ?? "Untitled.txt",
                FileTypeChoices = new[] { FilePickerFileTypes.All }
            });

            if (saveResult?.TryGetLocalPath() is { } path)
            {
                _currentFilePath = path;
                await WriteTextToFileAsync(path);
                UpdateWindowTitle(saveResult.Name);
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Writes the current content of the editor box to a specified file path asynchronously.
        /// </summary>
        private async Task WriteTextToFileAsync(string path)
        {
            if (_editorBox == null) return;

            SetStatus($"Saving {Path.GetFileName(path)}...");
            try
            {
                string content = _editorBox.Text ?? string.Empty;
                // Directly await the async file operation. This is more efficient than Task.Run.
                await File.WriteAllTextAsync(path, content);
                _lastSavedText = content;
                SetStatus("File saved successfully.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving file: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for unsaved changes and prompts the user to confirm before proceeding.
        /// Returns false if the user cancels the operation.
        /// </summary>
        private async Task<bool> CanProceedWithUnsavedChanges()
        {
            if (_window == null || _editorBox == null || _editorBox.Text == _lastSavedText)
            {
                return true; // Nothing to save or no window, so proceed.
            }

            // There are unsaved changes, so we must ask the user.
            var confirm = new ConfirmationWindow("Unsaved Changes", "Discard unsaved changes?");
            return await confirm.ShowDialog<bool>(_window);
        }


        /// <summary>
        /// Updates the status bar with a message. Ensures the update happens on the UI thread.
        /// </summary>
        private void SetStatus(string message)
        {
            // Use dispatcher to ensure UI update is on the correct thread.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_statusBlock != null)
                {
                    _statusBlock.Text = message;
                }
            });
        }

        /// <summary>
        /// Updates the main window title with the currently open file name.
        /// </summary>
        private void UpdateWindowTitle(string? fileName = null)
        {
            if (_window == null) return;

            string newTitle = string.IsNullOrEmpty(fileName)
                ? "Cycloside Editor - Untitled"
                : $"Cycloside Editor - {fileName}";

            Dispatcher.UIThread.InvokeAsync(() => _window.Title = newTitle);
        }
    }
}
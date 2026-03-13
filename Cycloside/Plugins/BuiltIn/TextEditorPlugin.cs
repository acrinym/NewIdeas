using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cycloside.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Acts as the ViewModel for the Text Editor window.
    /// Manages the editor's state and file operations.
    /// </summary>
    public partial class TextEditorPlugin : ObservableObject, IPlugin
    {
        // --- State Fields ---
        private TextEditorWindow? _window;
        private string? _currentFilePath;
        private string _lastSavedText = string.Empty;
        private Action<object?>? _openHandler;

        // --- IPlugin Properties ---
        public string Name => "Text Editor";
        public string Description => "A simple Markdown and text editor.";
        public Version Version => new Version(1, 0, 0); // Version reset for new architecture
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Observable Properties for UI Binding ---
        [ObservableProperty]
        private string _editorContent = string.Empty;

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private string _windowTitle = "Cycloside Editor - Untitled";

        // --- Plugin Lifecycle Methods ---
        public void Start()
        {
            // The Start method is now incredibly simple:
            // 1. Create the View (the Window)
            // 2. Set its DataContext to this class (the ViewModel)
            // 3. Show it.
            _window = new TextEditorWindow
            {
                DataContext = this
            };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TextEditorPlugin));
            _window.Show();

            _openHandler = async payload =>
            {
                if (payload is string p)
                    await LoadFileExternal(p);
            };
            PluginBus.Subscribe("texteditor:open", _openHandler);
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
            if (_openHandler != null)
            {
                PluginBus.Unsubscribe("texteditor:open", _openHandler);
                _openHandler = null;
            }
        }

        // --- Commands for UI Binding ---

        [RelayCommand]
        private async Task NewFile()
        {
            if (!await CanProceedWithUnsavedChanges()) return;

            EditorContent = string.Empty;
            _currentFilePath = null;
            _lastSavedText = string.Empty;
            StatusText = "New file created.";
            UpdateWindowTitle();
        }

        [RelayCommand]
        private async Task OpenFile()
        {
            if (_window is null || !await CanProceedWithUnsavedChanges()) return;

            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var openResult = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.All },
                SuggestedStartLocation = start
            });

            if (openResult?.FirstOrDefault()?.TryGetLocalPath() is { } path)
            {
                StatusText = $"Opening {Path.GetFileName(path)}...";
                try
                {
                    EditorContent = await File.ReadAllTextAsync(path);
                    _currentFilePath = path;
                    _lastSavedText = EditorContent;
                    StatusText = $"Successfully opened {Path.GetFileName(path)}.";
                    UpdateWindowTitle(Path.GetFileName(path));
                }
                catch (Exception ex)
                {
                    StatusText = $"Error opening file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveFileAs();
            }
            else
            {
                await WriteTextToFileAsync(_currentFilePath);
            }
        }

        [RelayCommand]
        private async Task SaveFileAs()
        {
            if (_window is null) return;

            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var saveResult = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Text File As...",
                SuggestedFileName = Path.GetFileName(_currentFilePath) ?? "Untitled.txt",
                FileTypeChoices = new[] { FilePickerFileTypes.All },
                SuggestedStartLocation = start
            });

            if (saveResult?.TryGetLocalPath() is { } path)
            {
                _currentFilePath = path;
                await WriteTextToFileAsync(path);
                UpdateWindowTitle(saveResult.Name);
            }
        }

        // --- Helper Methods ---

        private async Task WriteTextToFileAsync(string path)
        {
            StatusText = $"Saving {Path.GetFileName(path)}...";
            try
            {
                await File.WriteAllTextAsync(path, EditorContent);
                _lastSavedText = EditorContent;
                StatusText = "File saved successfully.";
            }
            catch (Exception ex)
            {
                StatusText = $"Error saving file: {ex.Message}";
            }
        }

        private async Task<bool> CanProceedWithUnsavedChanges()
        {
            if (_window is null || EditorContent == _lastSavedText) return true;

            var confirm = new ConfirmationWindow("Unsaved Changes", "Discard unsaved changes?");
            return await confirm.ShowDialog<bool>(_window);
        }

        private void UpdateWindowTitle(string? fileName = null)
        {
            WindowTitle = string.IsNullOrEmpty(fileName)
                ? "Cycloside Editor - Untitled"
                : $"Cycloside Editor - {fileName}";
        }

        private async Task LoadFileExternal(string path)
        {
            if (_window == null) return;
            if (!await CanProceedWithUnsavedChanges()) return;
            if (!File.Exists(path)) return;

            try
            {
                EditorContent = await File.ReadAllTextAsync(path);
                _currentFilePath = path;
                _lastSavedText = EditorContent;
                UpdateWindowTitle(Path.GetFileName(path));
                StatusText = $"Opened {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error opening file: {ex.Message}";
            }
        }
    }
}
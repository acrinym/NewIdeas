using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Cycloside.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Acts as the ViewModel for the Clipboard Manager window.
    /// It polls the clipboard and manages the history collection for the UI to display.
    /// </summary>
    public partial class ClipboardManagerPlugin : ObservableObject, IPlugin, IDisposable
    {
        private const int MaxHistoryCount = 50; // Increased limit

        private ClipboardManagerWindow? _window;
        private DispatcherTimer? _timer;
        private object? _lastSeenContent;

        // --- IPlugin Properties ---
        public string Name => "Clipboard Manager";
        public string Description => "Stores and manages clipboard history.";
        public Version Version => new(0, 3, 0); // Version bump for new features
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Observable Properties for UI Binding ---
        public ObservableCollection<ClipboardItem> History { get; } = new();

        [ObservableProperty]
        private ClipboardItem? _selectedEntry;

        partial void OnSelectedEntryChanged(ClipboardItem? value)
        {
            CopySelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        // --- Plugin Lifecycle & Disposal ---
        public void Start()
        {
            Directory.CreateDirectory(ImageCacheDir);
            LoadHistory();
            _window = new ClipboardManagerWindow { DataContext = this };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ClipboardManagerPlugin));
            _window.Show();

            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, CheckClipboard);
            _timer.Start();
        }

        public void Stop()
        {
            SaveHistory();
            _timer?.Stop();
            _window?.Close();
        }

        public void Dispose() => Stop();

        // --- Commands for UI Binding ---
        [RelayCommand]
        private async Task EntrySelected(ClipboardItem? selectedItem)
        {
            if (selectedItem == null) return;

            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                if (selectedItem.ContentType == ClipboardContentType.Text && selectedItem.Content is string text)
                {
                    await clipboard.SetTextAsync(text);
                }
                else if (selectedItem.ContentType == ClipboardContentType.Image && selectedItem.Content is Bitmap image)
                {
                    // To copy an image to the clipboard, we need to put it in a data object.
                    var dataObject = new DataObject();
                    dataObject.Set("Bitmap", image);
                    await clipboard.SetDataObjectAsync(dataObject);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task CopySelected()
        {
            await EntrySelected(SelectedEntry);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void DeleteSelected()
        {
            if (SelectedEntry == null) return;
            History.Remove(SelectedEntry);
            SelectedEntry = null;
        }

        private bool HasSelection() => SelectedEntry != null;

        // --- Private Logic ---

        private string ImageCacheDir => Path.Combine(AppContext.BaseDirectory, "data", "clipboard_images");

        private void SaveHistory()
        {
            var historyToSave = History.Select(item =>
            {
                if (item.ContentType == ClipboardContentType.Image && item is ImageClipboardItem imageItem)
                {
                    // For images, we only save the path. The image is already saved when it's first copied.
                    return new ClipboardItem(ClipboardContentType.Image, imageItem.ImagePath);
                }
                return item;
            }).ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(historyToSave);
            SettingsManager.SetPluginSetting("ClipboardManager", "History", json);
        }

        private void LoadHistory()
        {
            var json = SettingsManager.GetPluginSetting("ClipboardManager", "History", string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var history = System.Text.Json.JsonSerializer.Deserialize<List<ClipboardItem>>(json);
                if (history == null) return;

                foreach (var item in history)
                {
                    if (item.ContentType == ClipboardContentType.Image && item.Content is string imagePath)
                    {
                        if (File.Exists(imagePath))
                        {
                            var bitmap = new Bitmap(imagePath);
                            History.Add(new ImageClipboardItem(bitmap, imagePath));
                        }
                    }
                    else if (item.ContentType == ClipboardContentType.Text && item.Content is string text)
                    {
                        History.Add(new TextClipboardItem(text));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load clipboard history: {ex.Message}");
            }
        }

        private async void CheckClipboard(object? sender, EventArgs e)
        {
            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard == null) return;

            // Check for text
            var currentText = await clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(currentText) && currentText != (string?)_lastSeenContent)
            {
                _lastSeenContent = currentText;
                AddItem(new TextClipboardItem(currentText));
                return; // Prioritize text over image if both are present
            }

            // Check for images
            var formats = await clipboard.GetFormatsAsync();
            if (formats.Contains("Bitmap"))
            {
                var imageObject = await clipboard.GetDataAsync("Bitmap");
                if (imageObject is Bitmap image && image != _lastSeenContent)
                {
                    _lastSeenContent = image;
                    var imagePath = Path.Combine(ImageCacheDir, $"{Guid.NewGuid()}.png");
                    image.Save(imagePath);
                    AddItem(new ImageClipboardItem(image, imagePath));
                }
            }
        }

        private void AddItem(ClipboardItem item)
        {
            // Simple check for text duplicates.
            if (item.ContentType == ClipboardContentType.Text)
            {
                var existing = History.FirstOrDefault(h => h.ContentType == ClipboardContentType.Text && (string)h.Content == (string)item.Content);
                if (existing != null)
                {
                    History.Remove(existing);
                }
            }
            else if (item.ContentType == ClipboardContentType.Image)
            {
                // For images, we'll do a simple reference check for now.
                var existing = History.FirstOrDefault(h => h.ContentType == ClipboardContentType.Image && h.Content == item.Content);
                if (existing != null)
                {
                    History.Remove(existing);
                }
            }

            History.Insert(0, item);

            if (History.Count > MaxHistoryCount)
            {
                History.RemoveAt(History.Count - 1);
            }
        }
    }
}

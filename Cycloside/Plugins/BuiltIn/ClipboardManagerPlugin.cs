using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Acts as the ViewModel for the Clipboard Manager window.
    /// It polls the clipboard and manages the history collection for the UI to display.
    /// </summary>
    public partial class ClipboardManagerPlugin : ObservableObject, IPlugin, IDisposable
    {
        private const int MaxHistoryCount = 25;

        private ClipboardManagerWindow? _window;
        private DispatcherTimer? _timer;
        private string? _lastSeenText;

        // --- IPlugin Properties ---
        public string Name => "Clipboard Manager";
        public string Description => "Stores and manages clipboard history.";
        public Version Version => new(0, 2, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.Utilities;

        // --- Observable Properties for UI Binding ---

        /// <summary>
        /// A collection of clipboard history items. The View will bind its ListBox directly to this.
        /// Because it's an ObservableCollection, the UI will update automatically.
        /// </summary>
        public ObservableCollection<string> History { get; } = new();

        [ObservableProperty]
        private string? _selectedEntry;

        partial void OnSelectedEntryChanged(string? value)
        {
            CopySelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        // --- Plugin Lifecycle & Disposal ---

        public void Start()
        {
            // The ViewModel's job is to create its View and set the DataContext.
            _window = new ClipboardManagerWindow { DataContext = this };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ClipboardManagerPlugin));
            _window.Show();

            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, CheckClipboard);
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _window?.Close();
        }

        public void Dispose() => Stop();

        // --- Commands for UI Binding ---

        /// <summary>
        /// This command is executed when an item in the ListBox is double-tapped.
        /// The 'selectedItem' is passed from the View via CommandParameter.
        /// </summary>
        [RelayCommand]
        private async Task EntrySelected(string? selectedText)
        {
            if (string.IsNullOrEmpty(selectedText)) return;

            // Get the application's clipboard in a robust way.
            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(selectedText);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task CopySelected()
        {
            if (string.IsNullOrEmpty(SelectedEntry)) return;

            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(SelectedEntry);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void DeleteSelected()
        {
            if (string.IsNullOrEmpty(SelectedEntry)) return;
            History.Remove(SelectedEntry);
            SelectedEntry = null;
        }

        private bool HasSelection() => !string.IsNullOrEmpty(SelectedEntry);

        // --- Private Logic ---

        /// <summary>
        /// Polls the system clipboard for changes.
        /// </summary>
        private async void CheckClipboard(object? sender, EventArgs e)
        {
            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard == null) return;

            var currentText = await clipboard.GetTextAsync();
            if (string.IsNullOrEmpty(currentText) || currentText == _lastSeenText)
            {
                return;
            }

            // A new, unique item is found.
            _lastSeenText = currentText;

            // This logic ensures no duplicates and adds the new item to the top.
            History.Remove(currentText);
            History.Insert(0, currentText);

            if (History.Count > MaxHistoryCount)
            {
                History.RemoveAt(History.Count - 1);
            }
        }
    }
}

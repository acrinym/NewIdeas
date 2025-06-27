using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cycloside.Services;
using System;
using System.Collections.ObjectModel; // Changed from System.Collections.Generic
using System.Linq;

namespace Cycloside.Plugins.BuiltIn
{
    public class ClipboardManagerPlugin : IPlugin
    {
        private const int MaxHistoryCount = 25; // Extracted for easy configuration

        private readonly ObservableCollection<string> _history = new();
        private ClipboardManagerWindow? _window;
        private ListBox? _historyListBox;
        private DispatcherTimer? _timer;
        private string? _lastSeenText;

        public string Name => "Clipboard Manager";
        public string Description => "Stores and manages clipboard history.";
        public Version Version => new(0, 2, 0); // Incremented for major refactor
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new ClipboardManagerWindow();
            _historyListBox = _window.FindControl<ListBox>("HistoryList");

            if (_historyListBox != null)
            {
                // Set the ItemsSource once to our ObservableCollection.
                // The UI will now update automatically when the collection changes.
                _historyListBox.ItemsSource = _history;
                _historyListBox.DoubleTapped += HistoryListBox_DoubleTapped;
            }

            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ClipboardManagerPlugin));
            _window.Show();
            
            // Set up a timer to poll for clipboard changes.
            // Note: Polling is a simple, cross-platform approach. A more advanced
            // solution would use platform-specific hooks, but is more complex.
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, CheckClipboard);
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer = null;

            if (_window != null)
            {
                if (_historyListBox != null)
                {
                    // Detach the event handler to prevent memory leaks.
                    _historyListBox.DoubleTapped -= HistoryListBox_DoubleTapped;
                }
                _window.Close();
            }

            // Clear fields to release references
            _window = null;
            _historyListBox = null;
            _history.Clear();
        }

        /// <summary>
        /// Checks the system clipboard and updates the history if new text is found.
        /// </summary>
        private async void CheckClipboard(object? sender, EventArgs e)
        {
            var clipboard = _window?.Clipboard;
            if (clipboard == null) return;

            var currentText = await clipboard.GetTextAsync();
            if (string.IsNullOrEmpty(currentText) || currentText == _lastSeenText)
            {
                return;
            }

            // A new, unique item is found. Add it to the history.
            _lastSeenText = currentText;

            // Remove the item if it already exists to avoid duplicates and move it to the top.
            _history.Remove(currentText);
            _history.Insert(0, currentText); // Add new items to the top of the list.

            // Trim the collection if it exceeds the maximum size.
            if (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(MaxHistoryCount);
            }
        }

        /// <summary>
        /// Handles the DoubleTapped event on the history ListBox.
        /// Sets the selected item's text back to the system clipboard.
        /// </summary>
        private async void HistoryListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_historyListBox?.SelectedItem is string selectedText && _window?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(selectedText);
                
                // Optional: Give user feedback that the item was copied.
                // For example, by briefly changing the window title or a status bar.
            }
        }
    }
}
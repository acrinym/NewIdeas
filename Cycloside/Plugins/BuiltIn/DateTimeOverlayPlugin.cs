using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using Avalonia.Threading;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class DateTimeOverlayPlugin : ObservableObject, IPlugin
    {
        // --- Fields ---
        private DateTimeOverlayWindow? _window;
        private DispatcherTimer? _timer;
        
        // --- Configuration for new features ---
        private readonly List<string> _formats = new() { "G", "g", "yyyy-MM-dd HH:mm:ss", "T", "t" };
        private int _currentFormatIndex = 0;

        // --- IPlugin Properties ---
        public string Name => "Date/Time Overlay";
        public string Description => "A movable overlay that displays the current date and time.";
        public Version Version => new(1, 1, 0); // Incremented for new features
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Observable Properties for UI Binding ---
        [ObservableProperty]
        private string _timeText = string.Empty;

        [ObservableProperty]
        private bool _isLocked = false;

        // --- Plugin Lifecycle ---
        public void Start()
        {
            _window = new DateTimeOverlayWindow { DataContext = this };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(DateTimeOverlayPlugin));
            
            // Set up the timer
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_,_) => UpdateTime());
            
            UpdateTime(); // Update immediately on start
            _timer.Start();
            _window.Show();
        }

        public void Stop()
        {
            _timer?.Stop();
            _window?.Close();
            _window = null;
        }

        // --- Commands for the Context Menu ---
        [RelayCommand]
        private void CycleFormat()
        {
            _currentFormatIndex = (_currentFormatIndex + 1) % _formats.Count;
            UpdateTime(); // Update text with new format
        }

        [RelayCommand]
        private void Close() => Stop();

        // --- Private Helper Methods ---
        private void UpdateTime()
        {
            var now = DateTime.Now;
            TimeText = now.ToString(_formats[_currentFormatIndex]);
            PluginBus.Publish("clock:tick", now);
        }
    }
}
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel; // Switched to ObservableCollection
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class MP3PlayerPlugin : ObservableObject, IPlugin, IDisposable
    {
        // --- Fields ---
        private readonly DispatcherTimer _progressTimer;
        private int _currentIndex = -1;
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioReader;
        private float _volumeBeforeMute;

        // --- IPlugin Properties ---
        public string Name => "MP3 Player";
        public string Description => "Play MP3 files with a simple playlist.";
        public Version Version => new(1, 4, 0); // Incremented for new features
        public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget(this);
        public bool ForceDefaultTheme => false;

        // --- Public Properties & Collections for UI Binding ---
        public ObservableCollection<string> Playlist { get; } = new();

        [ObservableProperty]
        private string? _currentTrackName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStopped))]
        private bool _isPlaying;
        public bool IsStopped => !IsPlaying;

        [ObservableProperty]
        private TimeSpan _currentTime;

        [ObservableProperty]
        private TimeSpan _totalTime;

        [ObservableProperty]
        private string? _errorMessage;
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ToggleMuteCommand))]
        private float _volume = 1.0f; // Default to 100% volume

        [ObservableProperty]
        private bool _isMuted;

        public MP3PlayerPlugin()
        {
            _progressTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, OnTimerTick) { IsEnabled = false };
        }

        // --- IPlugin Lifecycle & Disposal ---
        void IPlugin.Stop() => Dispose();

        public void Dispose()
        {
            _progressTimer.Stop();
            CleanupPlayback();
            GC.SuppressFinalize(this);
        }

        // --- Commands for UI Binding ---
        [RelayCommand]
        private async Task AddFiles()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;

            var openResult = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select MP3 Files",
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("MP3 Files") { Patterns = new[] { "*.mp3" } } }
            });

            if (openResult is null) return;

            var validFiles = openResult.Select(f => f.TryGetLocalPath()).OfType<string>();
            foreach (var file in validFiles.Where(File.Exists))
            {
                if (!Playlist.Contains(file)) Playlist.Add(file);
            }

            // If nothing was playing and we added songs, start playing the first new one.
            if (!IsPlaying && Playlist.Any())
            {
                _currentIndex = 0;
                UpdateCurrentTrackInfo();
                Play();
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            if (_wavePlayer is null && _currentIndex != -1)
            {
                if (!InitializeReader(Playlist[_currentIndex]))
                {
                    Next();
                    return;
                }
            }
            _wavePlayer?.Play();
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Pause() => _wavePlayer?.Pause();

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Stop() => CleanupPlayback();

        [RelayCommand(CanExecute = nameof(HasNext))]
        private void Next() => SkipToTrack(_currentIndex + 1);

        [RelayCommand(CanExecute = nameof(HasPrevious))]
        private void Previous() => SkipToTrack(_currentIndex - 1);

        [RelayCommand]
        private void Seek(TimeSpan position)
        {
            if (_audioReader is not null) _audioReader.CurrentTime = position;
        }

        [RelayCommand(CanExecute = nameof(CanMute))]
        private void ToggleMute()
        {
            IsMuted = !IsMuted;
            Volume = IsMuted ? 0f : _volumeBeforeMute;
        }

        // --- Command CanExecute Conditions ---
        private bool CanPlay() => !IsPlaying && Playlist.Any();
        private bool HasNext() => _currentIndex < Playlist.Count - 1;
        private bool HasPrevious() => _currentIndex > 0;
        private bool CanMute() => _wavePlayer != null;

        // --- Private Helper Methods ---
        private void SkipToTrack(int index)
        {
            if (index < 0 || index >= Playlist.Count) return;
            var wasPlaying = IsPlaying;
            CleanupPlayback();
            _currentIndex = index;
            UpdateCurrentTrackInfo();
            if (wasPlaying) Play();
        }

        private bool InitializeReader(string filePath)
        {
            ErrorMessage = null; // Clear previous errors
            try
            {
                _audioReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent { Volume = Volume }; // Apply current volume
                _wavePlayer.Init(_audioReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;

                TotalTime = _audioReader.TotalTime;
                CurrentTime = TimeSpan.Zero;
                return true;
            }
            catch (Exception ex)
            {
                var friendlyError = $"Failed to load: {Path.GetFileName(filePath)}";
                Console.WriteLine($"[ERROR] {friendlyError} | Details: {ex.Message}");
                ErrorMessage = friendlyError; // Set property for UI to display
                CleanupPlayback();
                return false;
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            IsPlaying = false;
            _progressTimer.Stop();

            // Only auto-advance if playback finished naturally (not stopped by user)
            if (e.Exception is null && _audioReader is not null && _audioReader.Position >= _audioReader.Length)
            {
                if (HasNext()) Next();
                else CleanupPlayback(); // Last song finished
            }
        }
        
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_audioReader is not null && IsPlaying) CurrentTime = _audioReader.CurrentTime;
        }

        private void CleanupPlayback()
        {
            _progressTimer.Stop();
            if (_wavePlayer != null)
            {
                _wavePlayer.PlaybackStopped -= OnPlaybackStopped;
                _wavePlayer.Stop();
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }
            if (_audioReader != null)
            {
                _audioReader.Dispose();
                _audioReader = null;
            }
            IsPlaying = false;
            CurrentTime = TimeSpan.Zero;
            TotalTime = TimeSpan.Zero;
        }

        private void UpdateCurrentTrackInfo()
        {
            CurrentTrackName = _currentIndex != -1 ? Path.GetFileNameWithoutExtension(Playlist[_currentIndex]) : "No track loaded";
        }
        
        // --- Property Change Handlers ---
        partial void OnVolumeChanged(float value)
        {
            if (_wavePlayer != null) _wavePlayer.Volume = value;
            if (value > 0) IsMuted = false;
        }
        
        partial void OnIsPlayingChanged(bool value)
        {
            if (value) _progressTimer.Start();
            else _progressTimer.Stop();
            
            // Re-evaluate the CanExecute status of commands that depend on this state
            PlayCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
    }
}
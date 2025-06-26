using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class MP3PlayerPlugin : ObservableObject, IPlugin, IDisposable
    {
        // --- Fields ---
        private readonly List<string> _playlist = new();
        private readonly DispatcherTimer _progressTimer;
        private int _currentIndex = -1;
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioReader;

        // --- IPlugin Properties ---
        public string Name => "MP3 Player";
        public string Description => "Play MP3 files with a simple playlist.";
        public Version Version => new(1, 3, 0); // Incremented for optimization and features
        public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget(this);

        // --- Observable Properties for UI Binding ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
        private string? _currentTrackName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStopped))]
        private bool _isPlaying;

        public bool IsStopped => !IsPlaying;

        [ObservableProperty]
        private TimeSpan _currentTime;

        [ObservableProperty]
        private TimeSpan _totalTime;

        public MP3PlayerPlugin()
        {
            // Set up a timer to update the current time property for UI progress bars.
            _progressTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Normal, OnTimerTick);
            _progressTimer.Stop();
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
        private async Task OpenFilesAsync()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;

            var openResult = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select MP3 Files",
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("MP3 Files") { Patterns = new[] { "*.mp3" } } }
            });

            if (openResult is null) return;
            
            // Use OfType<string>() for a cleaner way to filter and cast non-null paths.
            var validFiles = openResult.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();

            if (validFiles.Any())
            {
                LoadFiles(validFiles);
                Play();
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            if (_wavePlayer is null && _currentIndex != -1)
            {
                if (!InitializeReader(_playlist[_currentIndex]))
                {
                    Next(); // Failed to open, try the next file
                    return;
                }
            }
            
            _wavePlayer?.Play();
            IsPlaying = true;
            _progressTimer.Start();
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Pause()
        {
            _wavePlayer?.Pause();
            IsPlaying = false;
            _progressTimer.Stop();
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Stop() => CleanupPlayback();

        [RelayCommand(CanExecute = nameof(HasNext))]
        private void Next() => SkipToTrack(_currentIndex + 1);

        [RelayCommand(CanExecute = nameof(HasPrevious))]
        private void Previous() => SkipToTrack(_currentIndex - 1);

        [RelayCommand]
        private void Seek(TimeSpan position)
        {
            if (_audioReader is not null)
            {
                _audioReader.CurrentTime = position;
                CurrentTime = _audioReader.CurrentTime; // Update property immediately
            }
        }

        // --- Command CanExecute Conditions ---
        private bool CanPlay() => !IsPlaying && _playlist.Any();
        private bool HasNext() => _currentIndex < _playlist.Count - 1;
        private bool HasPrevious() => _currentIndex > 0;

        // --- Private Helper Methods ---

        private void LoadFiles(IEnumerable<string> files)
        {
            CleanupPlayback();
            _playlist.Clear();
            _playlist.AddRange(files.Where(File.Exists));
            _currentIndex = _playlist.Any() ? 0 : -1;
            UpdateCurrentTrackInfo();
        }

        private void SkipToTrack(int index)
        {
            if (index < 0 || index >= _playlist.Count) return;
            var wasPlaying = IsPlaying;
            CleanupPlayback();
            _currentIndex = index;
            UpdateCurrentTrackInfo();
            if (wasPlaying) Play();
        }

        private bool InitializeReader(string filePath)
        {
            try
            {
                _audioReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent();
                _wavePlayer.Init(_audioReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;
                
                TotalTime = _audioReader.TotalTime;
                CurrentTime = TimeSpan.Zero;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not open audio file '{filePath}': {ex.Message}");
                CleanupPlayback();
                return false;
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // The IsPlaying flag is now set by Play/Pause/Stop commands.
            // This handler is only for auto-advancing to the next track.
            bool finishedNaturally = _audioReader is not null && Math.Abs(_audioReader.Position - _audioReader.Length) < 1000;

            if (finishedNaturally)
            {
                if (HasNext())
                {
                    Next();
                    Play();
                }
                else
                {
                    CleanupPlayback(); // Last song finished
                }
            }
        }

        private void UpdateCurrentTrackInfo()
        {
            CurrentTrackName = _currentIndex != -1 ? Path.GetFileNameWithoutExtension(_playlist[_currentIndex]) : "No track loaded";
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_audioReader is not null && IsPlaying)
            {
                CurrentTime = _audioReader.CurrentTime;
            }
        }

        private void CleanupPlayback()
        {
            _progressTimer.Stop();
            if (_wavePlayer is not null)
            {
                _wavePlayer.PlaybackStopped -= OnPlaybackStopped;
                _wavePlayer.Stop();
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }
            if (_audioReader is not null)
            {
                _audioReader.Dispose();
                _audioReader = null;
            }

            IsPlaying = false;
            CurrentTime = TimeSpan.Zero;
            TotalTime = TimeSpan.Zero;
        }

        // --- Property Change Handlers ---
        partial void OnIsPlayingChanged(bool value)
        {
            // When IsPlaying changes, re-evaluate the CanExecute status of our commands.
            PlayCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
    }
}
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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
    /// <summary>
    /// A plugin that provides MP3 playback functionality.
    /// It acts as a ViewModel, exposing properties and commands for a UI to bind to.
    /// </summary>
    public partial class MP3PlayerPlugin : ObservableObject, IPlugin
    {
        // --- Fields ---
        private readonly List<string> _playlist = new();
        private int _currentIndex = -1;

        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioReader;

        // --- IPlugin Properties ---
        public string Name => "MP3 Player";
        public string Description => "Play MP3 files with a simple playlist.";
        public Version Version => new(1, 2, 0); // Incremented for major refactor
        public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget(this);
        public bool ForceDefaultTheme => false;

        // --- Observable Properties for UI Binding ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
        private string? _currentTrackName;

        [ObservableProperty]
        private bool _isPlaying;

        // --- IPlugin Lifecycle ---
        public void Start()
        {
            // No action needed on start, as this plugin is controlled by its widget.
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        public void Stop()
        {
            // This is the definitive cleanup method called by the host.
            CleanupPlayback();
        }

        // --- Commands for UI Binding ---

        [RelayCommand]
        private async Task OpenFilesAsync()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            {
                return;
            }

            var result = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select MP3 Files",
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("MP3 Files") { Patterns = new[] { "*.mp3" } } }
            });

            var validFiles = result.Select(f => f.TryGetLocalPath())
                                   .Where(p => !string.IsNullOrEmpty(p))
                                   .Cast<string>()
                                   .ToList();

            if (validFiles.Any())
            {
                LoadFiles(validFiles);
                Play(); // Automatically play the first loaded file
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            // If we have a valid file path but no player, create one.
            if (_wavePlayer is null && _currentIndex != -1)
            {
                var filePath = _playlist[_currentIndex];
                if (!InitializeReader(filePath))
                {
                    // Failed to open file, try the next one
                    Next();
                    return;
                }
            }

            // If the player exists, play.
            _wavePlayer?.Play();
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Pause() => _wavePlayer?.Pause();

        [RelayCommand(CanExecute = nameof(HasNext))]
        private void Next() => SkipToTrack(_currentIndex + 1);

        [RelayCommand(CanExecute = nameof(HasPrevious))]
        private void Previous() => SkipToTrack(_currentIndex - 1);
        
        // --- Command CanExecute Conditions ---
        private bool CanPlay() => !IsPlaying && _playlist.Any();
        private bool CanStop() => IsPlaying;
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
            if (index < 0 || index >= _playlist.Count)
            {
                return;
            }

            var wasPlaying = IsPlaying;
            CleanupPlayback();
            _currentIndex = index;
            UpdateCurrentTrackInfo();

            if (wasPlaying)
            {
                Play();
            }
        }

        private bool InitializeReader(string filePath)
        {
            try
            {
                _audioReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent();
                _wavePlayer.Init(_audioReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;
                return true;
            }
            catch (Exception ex)
            {
                // TODO: Log this error to a proper logging service
                Console.WriteLine($"[ERROR] Could not open audio file '{filePath}': {ex.Message}");
                CleanupPlayback();
                return false;
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            IsPlaying = false;
            
            // This event fires on pause/stop and at the end of a track.
            // We only want to auto-advance if the track finished naturally.
            bool finishedNaturally = _audioReader is not null && _audioReader.Position >= _audioReader.Length;

            if (finishedNaturally && HasNext())
            {
                Next();
                Play();
            }
            else if(finishedNaturally)
            {
                // Last song finished, clean up the player
                 CleanupPlayback();
                 UpdateCurrentTrackInfo();
            }
        }
        
        private void UpdateCurrentTrackInfo()
        {
            CurrentTrackName = _currentIndex != -1 ? Path.GetFileNameWithoutExtension(_playlist[_currentIndex]) : "No track loaded";
        }

        private void CleanupPlayback()
        {
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
        }

        // --- Property Change Handlers ---
        partial void OnIsPlayingChanged(bool value)
        {
            // When IsPlaying changes, we need to re-evaluate the CanExecute status of our commands.
            PlayCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
    }
}
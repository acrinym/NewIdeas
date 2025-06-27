using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cycloside.Services;
using NAudio.Wave;

namespace Cycloside.Plugins.BuiltIn
{
    // A simple data structure for our audio data message
    public record AudioData(byte[] Spectrum, byte[] Waveform);

    public partial class MP3PlayerPlugin : ObservableObject, IPlugin, IDisposable
    {
        private const string AudioDataTopic = "audio:data"; // Define a topic for our bus

        // --- Fields ---
        private readonly DispatcherTimer _progressTimer;
        private int _currentIndex = -1;
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioReader;
        private float _volumeBeforeMute;
        private readonly Random _random = new();

        // --- IPlugin Properties ---
        public string Name => "MP3 Player";
        public string Description => "Play MP3 files with a simple playlist.";
        public Version Version => new(1, 5, 0); // Incremented for new features
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
        private float _volume = 1.0f;

        [ObservableProperty]
        private bool _isMuted;

        public MP3PlayerPlugin()
        {
            _progressTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Background, OnTimerTick) { IsEnabled = false };
        }

        // --- IPlugin Lifecycle & Disposal ---
        void IPlugin.Start() { }
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

        // --- Private Logic ---
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
            ErrorMessage = null;
            try
            {
                _audioReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent { Volume = Volume };
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
                ErrorMessage = friendlyError;
                CleanupPlayback();
                return false;
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            IsPlaying = false;
            _progressTimer.Stop();
            if (e.Exception is null && _audioReader is not null && _audioReader.Position >= _audioReader.Length)
            {
                if (HasNext()) Next();
                else CleanupPlayback();
            }
        }

        /// <summary>
        /// This is the main timer tick that drives the UI and now, the visualization data.
        /// </summary>
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_audioReader is null || !IsPlaying) return;
            
            CurrentTime = _audioReader.CurrentTime;

            // --- NEW: Generate and broadcast visualization data ---
            var (spectrum, waveform) = GenerateDummyAudioData();
            var payload = new AudioData(spectrum, waveform);
            PluginBus.Publish(AudioDataTopic, payload);
        }

        /// <summary>
        /// Generates fake audio data for visualization.
        /// TODO: Replace this with a real Fast Fourier Transform (FFT) implementation.
        /// </summary>
        private (byte[] spectrum, byte[] waveform) GenerateDummyAudioData()
        {
            var spectrum = new byte[576 * 2];
            var waveform = new byte[576 * 2];
            
            int peakPos = _random.Next(10, 200);
            byte peakHeight = (byte)_random.Next(150, 255);

            for (int i = 0; i < 288; i++)
            {
                // Create a random "bouncing bar" for the spectrum
                byte value = 0;
                if (i > peakPos - 5 && i < peakPos + 5) value = peakHeight;
                else if (i > peakPos - 15 && i < peakPos + 15) value = (byte)(peakHeight * 0.5);
                else if (i > peakPos - 30 && i < peakPos + 30) value = (byte)(peakHeight * 0.2);
                spectrum[i] = value;
                spectrum[i + 288] = value; // Right channel mirror

                // Create a simple sine wave for the waveform
                waveform[i] = (byte)(128 + Math.Sin(i * 0.1 + _currentTime.TotalSeconds * 10) * 50);
                waveform[i + 288] = waveform[i]; // Right channel mirror
            }
            return (spectrum, waveform);
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
        
        partial void OnVolumeChanged(float value)
        {
            if (_wavePlayer != null) _wavePlayer.Volume = value;
            if (value > 0) IsMuted = false;
        }
        
        partial void OnIsPlayingChanged(bool value)
        {
            if (value) _progressTimer.Start();
            else _progressTimer.Stop();
            
            PlayCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
    }
}

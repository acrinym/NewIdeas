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
using Cycloside.Plugins.BuiltIn.Views;
using NAudio.Wave;
using NAudio.Dsp; // Required for FastFourierTransform

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// A simple data structure to hold the visualization data.
    /// </summary>
    public record AudioData(byte[] Spectrum, byte[] Waveform);

    /// <summary>
    /// A helper class to perform a Fast Fourier Transform (FFT) on an audio stream
    /// to generate spectrum data for visualizations.
    /// </summary>
    public class SpectrumAnalyzer
    {
        private readonly ISampleProvider _source;
        private readonly Complex[] _fftBuffer;
        private readonly float[] _sampleBuffer;
        private readonly int _fftLength;

        public SpectrumAnalyzer(ISampleProvider source, int fftLength = 1024)
        {
            if (fftLength <= 0 || (fftLength & (fftLength - 1)) != 0)
                throw new ArgumentException("FFT length must be a power of 2.");

            _source = source;
            _fftLength = fftLength;
            _fftBuffer = new Complex[fftLength];
            _sampleBuffer = new float[fftLength];
        }

        /// <summary>
        /// Reads the latest audio samples and calculates FFT data.
        /// </summary>
        public void GetFftData(byte[] fftData)
        {
            // Read samples from the source audio into our buffer
            int read = _source is SampleAggregator agg
                ? agg.Read(_sampleBuffer)
                : _source.Read(_sampleBuffer, 0, _fftLength);
            if (read == 0) return;

            // Apply a window function to the samples and load them into the FFT buffer
            for (int i = 0; i < read; i++)
            {
                _fftBuffer[i].X = (float)(_sampleBuffer[i] * FftExtensions.BlackmanHarrisWindow(i, _fftLength));
                _fftBuffer[i].Y = 0;
            }
            // Zero out the rest of the buffer if we didn't read a full block
            Array.Clear(_fftBuffer, read, _fftLength - read);

            // Perform the FFT
            FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2.0), _fftBuffer);

            // Calculate magnitude and scale it for the visualizer
            for (int i = 0; i < fftData.Length / 2; i++)
            {
                // Calculate magnitude in decibels
                double magnitude = Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X + _fftBuffer[i].Y * _fftBuffer[i].Y);
                double decibels = 20 * Math.Log10(magnitude + 1e-9); // Add epsilon to avoid log(0)

                // Scale the dB value (e.g., -90dB to 0dB) to a byte (0-255)
                double scaledValue = (90 + decibels) * (255.0 / 90.0);
                byte finalValue = (byte)Math.Max(0, Math.Min(255, scaledValue));
                
                // For stereo visualizations, copy the value to both left and right channels
                fftData[i] = finalValue;
                if(i + fftData.Length / 2 < fftData.Length)
                    fftData[i + fftData.Length / 2] = finalValue;
            }
        }

        /// <summary>
        /// Provides the raw waveform data from the last buffer read.
        /// </summary>
        public void GetWaveformData(byte[] waveformData)
        {
            for (int i = 0; i < Math.Min(waveformData.Length, _sampleBuffer.Length); i++)
            {
                // Scale the float sample (-1.0 to 1.0) to a byte (0-255)
                waveformData[i] = (byte)((_sampleBuffer[i] + 1.0) * 127.5);
            }
        }
    }

    /// <summary>
    /// The final, optimized MP3 Player plugin acting as a ViewModel.
    /// </summary>
    public partial class MP3PlayerPlugin : ObservableObject, IPlugin, IDisposable
    {
        private const string AudioDataTopic = "audio:data";

        // --- Fields ---
        private readonly DispatcherTimer _progressTimer;
        private int _currentIndex = -1;
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioReader;
        private float _volumeBeforeMute;
        private SpectrumAnalyzer? _spectrumAnalyzer;
        // MERGED: Using the explicit `Views` namespace to avoid ambiguity.
        private Views.MP3PlayerWindow? _window;

        // --- IPlugin Properties ---
        public string Name => "MP3 Player";
        public string Description => "Play MP3 files with a simple playlist and visualization support.";
        public Version Version => new(1, 7, 0);
        public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget(this);
        public bool ForceDefaultTheme => false;

        // --- Observable Properties ---
        public ObservableCollection<string> Playlist { get; } = new();
        [ObservableProperty] private string? _currentTrackName;
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsStopped))] private bool _isPlaying;
        public bool IsStopped => !IsPlaying;
        [ObservableProperty] private TimeSpan _currentTime;
        [ObservableProperty] private TimeSpan _totalTime;
        [ObservableProperty] private string? _errorMessage;
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ToggleMuteCommand))] private float _volume = 1.0f;
        [ObservableProperty] private bool _isMuted;

        public MP3PlayerPlugin()
        {
            _progressTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Background, OnTimerTick) { IsEnabled = false };
        }

        // --- Plugin Lifecycle & Disposal ---
        public void Start()
        {
            if (_window != null)
            {
                _window.Activate();
                return;
            }
            // MERGED: Using the explicit `Views` namespace here as well.
            _window = new Views.MP3PlayerWindow { DataContext = this };
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, Name);
            _window.Closed += (_, _) => _window = null;
            _window.Show();
        }

        public void Stop() => Dispose();
        public void Dispose()
        {
            _progressTimer.Stop();
            CleanupPlayback();
            _window?.Close();
            GC.SuppressFinalize(this);
        }

        // --- Commands for UI Binding ---
        [RelayCommand]
        private async Task AddFiles()
        {
            var topLevel = _window ?? Application.Current?.GetMainTopLevel();
            if (topLevel is null) return;
            var openResult = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select MP3 Files", AllowMultiple = true,
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
                    Next(); // Try next song on failure
                    return;
                }
            }
            _wavePlayer?.Play();
            if (_wavePlayer != null)
            {
                IsPlaying = true;
            }
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Pause()
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.Pause();
                IsPlaying = false;
            }
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void StopPlayback() => CleanupPlayback();

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
            _volumeBeforeMute = Volume;
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
                
                // OPTIMIZED: Use a SampleAggregator to properly handle audio samples for FFT.
                var aggregator = new SampleAggregator(_audioReader);
                _spectrumAnalyzer = new SpectrumAnalyzer(aggregator, 1024);

                // MERGED: Kept the `DesiredLatency` setting for better performance.
                _wavePlayer = new WaveOutEvent { Volume = Volume, DesiredLatency = 200 };
                _wavePlayer.Init(aggregator); // Play through the aggregator
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;
                TotalTime = _audioReader.TotalTime;
                CurrentTime = TimeSpan.Zero;
                return true;
            }
            catch (Exception ex)
            {
                var friendlyError = $"Failed to load: {Path.GetFileName(filePath)}";
                Logger.Log($"[ERROR] {friendlyError} | Details: {ex.Message}");
                ErrorMessage = friendlyError;
                CleanupPlayback();
                return false;
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            IsPlaying = false;
            if (e.Exception is null && _audioReader is not null && _audioReader.Position >= _audioReader.Length)
            {
                Dispatcher.UIThread.InvokeAsync(() => { if (HasNext()) Next(); else CleanupPlayback(); });
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_audioReader is null || !IsPlaying || _spectrumAnalyzer is null) return;
            CurrentTime = _audioReader.CurrentTime;

            // MERGED: Kept the more descriptive comments from the `hkqxjj-codex` branch.
            // FIX: Ensure byte arrays are correctly sized for Winamp visualizers (576 samples)
            var spectrum = new byte[576];
            var waveform = new byte[576];

            _spectrumAnalyzer.GetFftData(spectrum);
            _spectrumAnalyzer.GetWaveformData(waveform);

            // NEW: Publish a stereo version of the data for plugins that expect it.
            var stereoSpectrum = new byte[1152];
            var stereoWaveform = new byte[1152];
            Array.Copy(spectrum, 0, stereoSpectrum, 0, 576);
            Array.Copy(spectrum, 0, stereoSpectrum, 576, 576); // Duplicate mono to right channel
            Array.Copy(waveform, 0, stereoWaveform, 0, 576);
            Array.Copy(waveform, 0, stereoWaveform, 576, 576);

            var payload = new AudioData(stereoSpectrum, stereoWaveform);
            PluginBus.Publish(AudioDataTopic, payload);
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
            _spectrumAnalyzer = null;
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
            if (value > 0 && IsMuted) IsMuted = false;
        }

        partial void OnIsPlayingChanged(bool value)
        {
            if (value) _progressTimer.Start();
            else _progressTimer.Stop();
            
            PlayCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StopPlaybackCommand.NotifyCanExecuteChanged();
        }
    }
}

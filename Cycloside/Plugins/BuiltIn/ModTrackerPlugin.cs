using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cycloside.Services;
using NAudio.Wave;
using OpenMpt.Sharp; // You will need to add the 'libopenmpt-sharp' NuGet package
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    #region Bridge Class
    /// <summary>
    /// A custom NAudio IWaveProvider that wraps a libopenmpt Module.
    /// This is the bridge that allows NAudio to play tracker music.
    /// </summary>
    public class OpenMptWaveProvider : IWaveProvider, IDisposable
    {
        private readonly Module _module;
        public WaveFormat WaveFormat { get; }

        public OpenMptWaveProvider(Module module)
        {
            _module = module;
            // Standard CD quality stereo audio.
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // libopenmpt-sharp provides helpers to read directly into a byte buffer.
            return _module.ReadInterleavedStereo(WaveFormat.SampleRate, count / (2 * 4), buffer, offset);
        }
        /// <summary>
        /// Provides unsafe access to the wrapped <see cref="Module"/> instance.
        /// </summary>
        /// <remarks>
        /// Modifying the returned module can interfere with playback; use only
        /// when necessary.
        /// </remarks>
        public Module UnsafeGetModule() => _module;


        public void Dispose()
        {
            _module.Dispose();
        }
    }
    #endregion

    public partial class ModTrackerPlugin : ObservableObject, IPlugin, IDisposable
    {
        // --- State and Services ---
        private Window? _window;
        private IWavePlayer? _wavePlayer;
        private OpenMptWaveProvider? _waveProvider;
        private readonly DispatcherTimer _uiTimer;

        // --- IPlugin Properties ---
        public string Name => "ModPlug Tracker";
        public string Description => "Plays and inspects various tracker module files (MOD, IT, S3M, XM, etc.).";
        public Version Version => new(1, 0, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.RetroComputing;

        // --- Observable Properties for UI Binding ---
        [ObservableProperty]
        private string _moduleName = "No file loaded.";
        [ObservableProperty]
        private string _moduleInfo = string.Empty;
        [ObservableProperty]
        private string _patternData = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
        private bool _isPlaying;

        public ModTrackerPlugin()
        {
            // This timer will periodically refresh the UI with the current tracker state.
            _uiTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, UpdateUI);
        }

        public void Start()
        {
            if (_window != null)
            {
                _window.Activate();
                return;
            }

            _window = BuildTrackerWindow();
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, Name);
            _window.Closed += (s, e) => Stop();
            _window.Show();
        }

        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            _uiTimer.Stop();
            CleanupPlayback();
            _window?.Close();
            _window = null;
            GC.SuppressFinalize(this);
        }

        private void CleanupPlayback()
        {
            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _wavePlayer = null;

            _waveProvider?.Dispose();
            _waveProvider = null;

            IsPlaying = false;
        }

        private void UpdateUI(object? sender, EventArgs e)
        {
            if (!IsPlaying || _waveProvider == null) return;

            // This is where we poll libopenmpt for the current state and format it for display.
            var pattern = _waveProvider.UnsafeGetModule().GetCurrentPattern();
            var row = _waveProvider.UnsafeGetModule().GetCurrentRow();
            var numRows = _waveProvider.UnsafeGetModule().GetPatternRowCount(pattern);

            var sb = new StringBuilder();
            sb.AppendLine($"Pattern: {pattern} / {_waveProvider.UnsafeGetModule().GetNumberOfPatterns() - 1} | Row: {row} / {numRows - 1}");
            sb.AppendLine("-------------------------------------------------");

            // Display a snippet of the current pattern
            int startRow = Math.Max(0, row - 8);
            int endRow = Math.Min(numRows, row + 8);

            for (int r = startRow; r < endRow; r++)
            {
                if (r == row) sb.Append("> "); else sb.Append("  ");
                sb.Append($"{r:D2}: ");
                for (int ch = 0; ch < _waveProvider.UnsafeGetModule().GetNumberOfChannels(); ch++)
                {
                    sb.Append(_waveProvider.UnsafeGetModule().FormatPatternRowChannel(pattern, r, ch, 15));
                    sb.Append(" | ");
                }
                sb.AppendLine();
            }
            PatternData = sb.ToString();
        }

        // --- Commands ---

        [RelayCommand]
        private async Task OpenFile()
        {
            if (_window is null) return;

            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Tracker Module",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Tracker Modules") { Patterns = new[] { "*.it", "*.xm", "*.s3m", "*.mod" } }, FilePickerFileTypes.All },
                SuggestedStartLocation = start
            });

            if (result?.FirstOrDefault()?.TryGetLocalPath() is { } path)
            {
                LoadModule(path);
            }
        }

        private void LoadModule(string path)
        {
            CleanupPlayback();
            try
            {
                var fileBytes = File.ReadAllBytes(path);
                var module = Module.Create(fileBytes, new Module.Settings());
                _waveProvider = new OpenMptWaveProvider(module);

                _wavePlayer = new WaveOutEvent() { DesiredLatency = 200 };
                _wavePlayer.Init(_waveProvider);

                ModuleName = module.GetMetadata("title").Trim();
                if (string.IsNullOrEmpty(ModuleName))
                {
                    ModuleName = Path.GetFileName(path);
                }

                ModuleInfo = $"Channels: {module.GetNumberOfChannels()} | " +
                             $"Patterns: {module.GetNumberOfPatterns()} | " +
                             $"Samples: {module.GetNumberOfSamples()}";

                PlayCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load module '{path}': {ex.Message}");
                ModuleName = "Error loading file.";
                ModuleInfo = ex.Message;
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            _wavePlayer?.Play();
            IsPlaying = true;
            _uiTimer.Start();
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void Pause()
        {
            _wavePlayer?.Pause();
            IsPlaying = false;
            _uiTimer.Stop();
        }

        [RelayCommand(CanExecute = nameof(IsPlaying))]
        private void StopPlayback()
        {
            _wavePlayer?.Stop();
            // Reset position to the beginning
            _waveProvider?.UnsafeGetModule().SetPosition(0);
            IsPlaying = false;
            _uiTimer.Stop();
            UpdateUI(null, EventArgs.Empty); // Update to show position 0
        }

        private bool CanPlay() => !IsPlaying && _waveProvider != null;

        // --- UI Construction ---
        private Window BuildTrackerWindow()
        {
            var openButton = new Button { Content = "Open..." };
            openButton.Bind(Button.CommandProperty, new Binding(nameof(OpenFileCommand)));

            var playButton = new Button { Content = "Play" };
            playButton.Bind(Button.CommandProperty, new Binding(nameof(PlayCommand)));

            var pauseButton = new Button { Content = "Pause" };
            pauseButton.Bind(Button.CommandProperty, new Binding(nameof(PauseCommand)));

            var stopButton = new Button { Content = "Stop" };
            stopButton.Bind(Button.CommandProperty, new Binding(nameof(StopPlaybackCommand)));

            var controls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(5),
                Children = { openButton, playButton, pauseButton, stopButton }
            };

            var nameBlock = new TextBlock { FontWeight = FontWeight.Bold, FontSize = 14, Margin = new Thickness(5, 0) };
            nameBlock.Bind(TextBlock.TextProperty, new Binding(nameof(ModuleName)));

            var infoBlock = new TextBlock { Opacity = 0.8, Margin = new Thickness(5, 0, 5, 5) };
            infoBlock.Bind(TextBlock.TextProperty, new Binding(nameof(ModuleInfo)));

            var patternBlock = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,monospace"),
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(5)
            };
            patternBlock.Bind(TextBox.TextProperty, new Binding(nameof(PatternData)));

            var mainLayout = new DockPanel();
            DockPanel.SetDock(controls, Dock.Top);
            DockPanel.SetDock(nameBlock, Dock.Top);
            DockPanel.SetDock(infoBlock, Dock.Top);
            mainLayout.Children.AddRange(new Control[] { controls, nameBlock, infoBlock, patternBlock });

            return new Window
            {
                Title = "ModPlug Tracker",
                Width = 800,
                Height = 600,
                Content = mainLayout,
                DataContext = this
            };
        }
    }
}

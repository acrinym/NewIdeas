// ============================================================================
// ENHANCED VISUALIZER SERVICE - Modern visualizer management
// ============================================================================
// Purpose: Manage visualizers with new EnhancedAudioService integration
// Features: Adapter pattern for legacy visualizers, modern rendering pipeline
// Dependencies: EnhancedAudioService, IVisualizerAdapter, IManagedVisualizer
// ============================================================================

using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Visuals.Managed;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;

namespace Cycloside.Services;

public class EnhancedVisualizerService
{
    private readonly EnhancedAudioService _audioService;
    private readonly IVisualizerAdapter _adapter;
    private readonly List<IManagedVisualizer> _activeVisualizers = new();
    private readonly Dictionary<IManagedVisualizer, DispatcherTimer> _renderTimers = new();
    private IManagedVisualizer? _currentVisualizer;
    private bool _isRunning;

    // Modern visualizer collection
    public ObservableCollection<VisualizerInfo> AvailableVisualizers { get; } = new();

    public event EventHandler<VisualizerChangedEventArgs>? VisualizerChanged;
    public event EventHandler? VisualizerStarted;
    public event EventHandler? VisualizerStopped;

    public IManagedVisualizer? CurrentVisualizer => _currentVisualizer;
    public bool IsRunning => _isRunning;

    public EnhancedVisualizerService(EnhancedAudioService audioService)
    {
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _adapter = new VisualizerAdapter();

        // Subscribe to audio service events
        _audioService.AudioDataAvailable += AudioService_AudioDataAvailable;

        LoadAvailableVisualizers();
    }

    private void LoadAvailableVisualizers()
    {
        // Load from Cycloside's existing visualizer collection
        AvailableVisualizers.Clear();

        // Add Phoenix-style visualizers
        AvailableVisualizers.Add(new VisualizerInfo("Spectrum Bars", "Classic spectrum analyzer bars", typeof(Cycloside.Visuals.Managed.PhoenixVisuals.SpectrumVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Oscilloscope", "Waveform oscilloscope display", typeof(Cycloside.Visuals.Managed.Visualizers.OscilloscopeVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Matrix Rain", "Matrix-style falling characters", typeof(Cycloside.Visuals.Managed.Visualizers.MatrixRainVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Lava Lamp", "Classic lava lamp simulation", typeof(Cycloside.Visuals.Managed.Visualizers.LavaLampVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Starfield", "3D starfield effect", typeof(Cycloside.Visuals.Managed.Visualizers.StarfieldVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Polar Wave", "Polar coordinate waveform", typeof(Cycloside.Visuals.Managed.Visualizers.PolarWaveVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Particle Pulse", "Particle system responding to audio", typeof(Cycloside.Visuals.Managed.Visualizers.ParticlePulseVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Circular Spectrum", "Circular spectrum visualization", typeof(Cycloside.Visuals.Managed.Visualizers.CircularSpectrumVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Spectrogram", "Time-frequency spectrogram", typeof(Cycloside.Visuals.Managed.Visualizers.SpectrogramVisualizer)));
        AvailableVisualizers.Add(new VisualizerInfo("Spectrum Mirror", "Mirrored spectrum effect", typeof(Cycloside.Visuals.Managed.Visualizers.WindowsMediaBarsVisualizer)));

        // Add Phoenix-specific visualizers
        AvailableVisualizers.Add(new VisualizerInfo("Fiber Lamp", "PhoenixVisualizer fiber lamp effect", typeof(Cycloside.Visuals.Managed.PhoenixVisuals.FiberLamp)));
        AvailableVisualizers.Add(new VisualizerInfo("Gears", "Mechanical gears visualization", typeof(Cycloside.Visuals.Managed.PhoenixVisuals.Gears)));
        AvailableVisualizers.Add(new VisualizerInfo("Time Tunnel", "Spiral time tunnel effect", typeof(Cycloside.Visuals.Managed.PhoenixVisuals.TimeTunnel)));
        AvailableVisualizers.Add(new VisualizerInfo("Rainbow Sphere", "3D rainbow sphere grid", typeof(Cycloside.Visuals.Managed.PhoenixVisuals.RainbowSphereGridSuperscope)));
        AvailableVisualizers.Add(new VisualizerInfo("Moebius Strip", "Mathematical moebius strip", typeof(Cycloside.Visuals.Managed.PhoenixVisuals.MoebiusStrip)));

        Logger.Log($"🎨 Loaded {AvailableVisualizers.Count} visualizers");
    }

    public void StartVisualizer(string visualizerName)
    {
        var info = AvailableVisualizers.FirstOrDefault(v => v.Name == visualizerName);
        if (info == null)
        {
            Logger.Log($"❌ Visualizer not found: {visualizerName}");
            return;
        }

        StartVisualizer(info);
    }

    public void StartVisualizer(VisualizerInfo info)
    {
        try
        {
            StopCurrentVisualizer();

            // Create visualizer instance
            var visualizer = (IManagedVisualizer)Activator.CreateInstance(info.Type)!;
            visualizer.Init();

            _currentVisualizer = visualizer;
            _activeVisualizers.Add(visualizer);

            // Set up render timer (60 FPS)
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000 / 60)
            };
            timer.Tick += (s, e) => RenderCurrentVisualizer();
            timer.Start();

            _renderTimers[visualizer] = timer;
            _isRunning = true;

            VisualizerChanged?.Invoke(this, new VisualizerChangedEventArgs(info.Name, true));
            VisualizerStarted?.Invoke(this, EventArgs.Empty);

            Logger.Log($"🎨 Started visualizer: {info.Name}");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to start visualizer {info.Name}: {ex.Message}");
        }
    }

    public void StopCurrentVisualizer()
    {
        if (_currentVisualizer == null) return;

        // Stop render timer
        if (_renderTimers.TryGetValue(_currentVisualizer, out var timer))
        {
            timer.Stop();
            _renderTimers.Remove(_currentVisualizer);
        }

        // Dispose visualizer
        _currentVisualizer.Dispose();
        _activeVisualizers.Remove(_currentVisualizer);
        _currentVisualizer = null;
        _isRunning = false;

        VisualizerStopped?.Invoke(this, EventArgs.Empty);
        Logger.Log("🛑 Stopped current visualizer");
    }

    public void StopAllVisualizers()
    {
        foreach (var visualizer in _activeVisualizers.ToList())
        {
            if (_renderTimers.TryGetValue(visualizer, out var timer))
            {
                timer.Stop();
                _renderTimers.Remove(visualizer);
            }
            visualizer.Dispose();
        }

        _activeVisualizers.Clear();
        _currentVisualizer = null;
        _isRunning = false;

        Logger.Log("🛑 Stopped all visualizers");
    }

    private void RenderCurrentVisualizer()
    {
        if (_currentVisualizer == null || !_isRunning) return;

        // This would typically render to a visualizer window/control
        // For now, we'll just call the render method for compatibility
        // In a real implementation, this would update a visualizer surface
    }

    private void AudioService_AudioDataAvailable(object? sender, AudioDataEventArgs e)
    {
        if (!_isRunning || _currentVisualizer == null) return;

        // Update all active visualizers with new audio data
        foreach (var visualizer in _activeVisualizers)
        {
            try
            {
                _adapter.UpdateVisualizer(visualizer, e.Analysis);
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Visualizer update error ({visualizer.Name}): {ex.Message}");
            }
        }
    }

    public void RenderVisualizerToContext(IManagedVisualizer visualizer, DrawingContext context, Size size, TimeSpan elapsed)
    {
        try
        {
            // Get latest audio analysis (this would be provided by the audio service)
            var analysis = _audioService.AnalyzeAudio();
            _adapter.RenderVisualizer(visualizer, context, size, elapsed, analysis);
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Visualizer render error ({visualizer.Name}): {ex.Message}");
        }
    }

    public List<string> GetVisualizerNames() => AvailableVisualizers.Select(v => v.Name).ToList();

    public VisualizerInfo? GetVisualizerInfo(string name) => AvailableVisualizers.FirstOrDefault(v => v.Name == name);
}

public class VisualizerInfo
{
    public string Name { get; }
    public string Description { get; }
    public Type Type { get; }

    public VisualizerInfo(string name, string description, Type type)
    {
        Name = name;
        Description = description;
        Type = type;
    }
}

public class VisualizerChangedEventArgs : EventArgs
{
    public string VisualizerName { get; }
    public bool Started { get; }

    public VisualizerChangedEventArgs(string visualizerName, bool started)
    {
        VisualizerName = visualizerName;
        Started = started;
    }
}

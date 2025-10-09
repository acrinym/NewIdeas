using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Services;
using Widgets = Cycloside.Widgets;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Color = Avalonia.Media.Color;

namespace Cycloside.Visuals.Managed;

public partial class VisualizerWindow : Window
{
    private readonly EnhancedVisualizerService _visualizerService;
    private readonly EnhancedAudioService _audioService;
    private DispatcherTimer? _renderTimer;
    private bool _showControls = false;

    public VisualizerWindow(EnhancedAudioService audioService)
    {
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _visualizerService = new EnhancedVisualizerService(audioService);

        InitializeComponent();
        DataContext = new VisualizerWindowViewModel(_visualizerService);

        SetupEventHandlers();
        SetupRendering();
    }

    public VisualizerWindow() : this(new EnhancedAudioService())
    {
        // Parameterless constructor for Avalonia XAML/resource usage
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupEventHandlers()
    {
        // Toggle controls visibility on button click
        var toggleButton = this.FindControl<Button>("ToggleControlsButton");
        if (toggleButton != null)
        {
            toggleButton.Click += ToggleControlsButton_Click;
        }

        // Show controls on mouse enter
        PointerEntered += (s, e) => ShowControls();
        PointerExited += (s, e) => HideControlsAfterDelay();

        // Visualizer service events
        _visualizerService.VisualizerChanged += VisualizerService_VisualizerChanged;
        _visualizerService.VisualizerStarted += VisualizerService_VisualizerStarted;
        _visualizerService.VisualizerStopped += VisualizerService_VisualizerStopped;

        // Audio service events for real-time info
        _audioService.AudioDataAvailable += AudioService_AudioDataAvailable;
    }

    private void SetupRendering()
    {
        // Set up 60 FPS rendering timer
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000 / 60)
        };
        _renderTimer.Tick += RenderTimer_Tick;
        _renderTimer.Start();
    }

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        if (_visualizerService.CurrentVisualizer != null)
        {
            var canvas = this.FindControl<Canvas>("VisualizerCanvas");
            if (canvas != null)
            {
                // For now, we'll use a simple approach since we don't have a direct canvas rendering context
                // In a real implementation, this would render to the canvas surface
                var size = new Size(canvas.Bounds.Width, canvas.Bounds.Height);
                var elapsed = TimeSpan.FromMilliseconds(Environment.TickCount);

                // Update the canvas background to show activity
                canvas.Background = new SolidColorBrush(Color.FromRgb(10, 10, 20));
            }
        }
    }

    private void ToggleControlsButton_Click(object? sender, RoutedEventArgs e)
    {
        _showControls = !_showControls;
        UpdateControlsVisibility();
    }

    private void ShowControls()
    {
        _showControls = true;
        UpdateControlsVisibility();
    }

    private void HideControlsAfterDelay()
    {
        // Hide controls after 3 seconds of no mouse movement
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            _showControls = false;
            UpdateControlsVisibility();
            timer.Stop();
        };
        timer.Start();
    }

    private void UpdateControlsVisibility()
    {
        var controlsOverlay = this.FindControl<Border>("ControlsOverlay");
        if (controlsOverlay != null)
        {
            controlsOverlay.IsVisible = _showControls;
        }
    }

    private void VisualizerService_VisualizerChanged(object? sender, VisualizerChangedEventArgs e)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
        {
            statusText.Text = e.Started
                ? $"🎨 Visualizer: {e.VisualizerName}"
                : "No visualizer active";
        }
    }

    private void VisualizerService_VisualizerStarted(object? sender, EventArgs e)
    {
        Title = $"Cycloside Visualizer - {_visualizerService.CurrentVisualizer?.Name}";
    }

    private void VisualizerService_VisualizerStopped(object? sender, EventArgs e)
    {
        Title = "Cycloside Visualizer";
    }

    private void AudioService_AudioDataAvailable(object? sender, AudioDataEventArgs e)
    {
        // Update audio info display
        Dispatcher.UIThread.Post(() =>
        {
            var audioInfo = this.FindControl<TextBlock>("AudioInfo");
            if (audioInfo != null)
            {
                audioInfo.Text = $"🎵 Bass: {e.Analysis.BassLevel:F2} | Mid: {e.Analysis.MidLevel:F2} | Treble: {e.Analysis.TrebleLevel:F2}";
            }
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _renderTimer?.Stop();
        _visualizerService.StopAllVisualizers();
        base.OnClosed(e);
    }
}

// ViewModel for the visualizer window
public class VisualizerWindowViewModel
{
    private readonly EnhancedVisualizerService _visualizerService;

    public VisualizerWindowViewModel(EnhancedVisualizerService visualizerService)
    {
        _visualizerService = visualizerService;

        StartVisualizerCommand = new Widgets.RelayCommand<string>(StartVisualizer);
        StopVisualizerCommand = new Widgets.RelayCommand(StopVisualizer);

        // Subscribe to service events for UI updates
        _visualizerService.VisualizerChanged += (s, e) => UpdateStatusMessage();
        _visualizerService.VisualizerStarted += (s, e) => UpdateStatusMessage();
        _visualizerService.VisualizerStopped += (s, e) => UpdateStatusMessage();

        UpdateStatusMessage();
    }

    public ObservableCollection<VisualizerInfo> AvailableVisualizers => _visualizerService.AvailableVisualizers;

    public VisualizerInfo? CurrentVisualizerInfo => _visualizerService.AvailableVisualizers
        .FirstOrDefault(v => v.Name == _visualizerService.CurrentVisualizer?.Name);

    public string StatusMessage { get; private set; } = "No visualizer active";

    public ICommand StartVisualizerCommand { get; }
    public ICommand StopVisualizerCommand { get; }

    private void StartVisualizer(string? visualizerName)
    {
        if (!string.IsNullOrEmpty(visualizerName))
        {
            _visualizerService.StartVisualizer(visualizerName);
        }
    }

    private void StopVisualizer(object? parameter)
    {
        _visualizerService.StopCurrentVisualizer();
    }

    private void UpdateStatusMessage()
    {
        StatusMessage = _visualizerService.IsRunning
            ? $"Active: {_visualizerService.CurrentVisualizer?.Name}"
            : "No visualizer active";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.Primitives;
using Cycloside.Plugins.BuiltIn; // AudioData

namespace Cycloside.Visuals.Managed;

public class ManagedVisHostWindow : Window
{
    private readonly ComboBox _selector;
    private readonly VisualizerCanvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly List<IManagedVisualizer> _visualizers;
    private DateTime _start;
    private readonly Border _optionsHost = new() { Margin = new Thickness(6, 0, 6, 6) };

    public ManagedVisHostWindow(IEnumerable<IManagedVisualizer> visualizers)
    {
        _visualizers = visualizers.ToList();

        Title = "Managed Visualizer";
        Width = 800;
        Height = 500;

        _selector = new ComboBox
        {
            ItemsSource = _visualizers.Select(v => v.Name).ToList(),
            SelectedIndex = _visualizers.Count > 0 ? 0 : -1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(6)
        };

        _canvas = new VisualizerCanvas(() => Current)
        {
            Focusable = true,
            Margin = new Thickness(6),
        };

        _selector.SelectionChanged += (_, __) => { _canvas.InvalidateVisual(); RefreshOptions(); };

        var topBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(6, 6, 6, 0) };
        topBar.Children.Add(new TextBlock { Text = "Visualizer:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        topBar.Children.Add(_selector);

        var nativeToggle = new CheckBox { Content = "Use native colors" };
        nativeToggle.IsChecked = (StateManager.Get("ManagedVis.NativeColors") ?? "no").Equals("yes", StringComparison.OrdinalIgnoreCase);
        nativeToggle.GetObservable(ToggleButton.IsCheckedProperty).Subscribe(v =>
        {
            StateManager.Set("ManagedVis.NativeColors", v == true ? "yes" : "no");
            _canvas.InvalidateVisual();
        });
        topBar.Children.Add(nativeToggle);

        var themeBox = new ComboBox { Width = 140 };
        themeBox.ItemsSource = new[] { "Neon", "Classic", "Fire", "Ocean", "Matrix", "Sunset" };
        var savedTheme = StateManager.Get("ManagedVis.Theme") ?? "Neon";
        themeBox.SelectedIndex = Array.IndexOf((string[])themeBox.ItemsSource!, savedTheme);
        themeBox.SelectionChanged += (_, __) => { if (themeBox.SelectedItem is string t) { StateManager.Set("ManagedVis.Theme", t); _canvas.InvalidateVisual(); } };
        topBar.Children.Add(new TextBlock { Text = "Theme:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        topBar.Children.Add(themeBox);

        var sens = new Slider { Minimum = 0.2, Maximum = 3.0, Width = 160 };
        if (double.TryParse(StateManager.Get("ManagedVis.Sensitivity"), out var sv)) sens.Value = sv; else sens.Value = 1.0;
        sens.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) { StateManager.Set("ManagedVis.Sensitivity", sens.Value.ToString("0.00")); } };
        topBar.Children.Add(new TextBlock { Text = "Sensitivity:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        topBar.Children.Add(sens);

        var compactToggle = new CheckBox { Content = "Compact" };
        compactToggle.IsChecked = (StateManager.Get("ManagedVis.Compact") ?? "no").Equals("yes", StringComparison.OrdinalIgnoreCase);
        compactToggle.GetObservable(ToggleButton.IsCheckedProperty).Subscribe(v =>
        {
            var on = v == true;
            Topmost = on;
            StateManager.Set("ManagedVis.Compact", on ? "yes" : "no");
        });
        topBar.Children.Add(compactToggle);

        var root = new DockPanel();
        DockPanel.SetDock(topBar, Dock.Top);
        root.Children.Add(topBar);
        DockPanel.SetDock(_optionsHost, Dock.Bottom);
        root.Children.Add(_optionsHost);
        root.Children.Add(_canvas);
        Content = root;

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Background, (_, __) => _canvas.InvalidateVisual());
        _timer.IsEnabled = true;
        _start = DateTime.UtcNow;

        this.Closed += (_, __) =>
        {
            _timer.Stop();
            foreach (var v in _visualizers) v.Dispose();
        };

        RefreshOptions();
    }

    public IManagedVisualizer? Current
        => _selector.SelectedIndex >= 0 && _selector.SelectedIndex < _visualizers.Count
            ? _visualizers[_selector.SelectedIndex]
            : null;

    public void UpdateAudio(AudioData data)
    {
        Current?.UpdateAudioData(data);
    }

    private void RefreshOptions()
    {
        _optionsHost.Child = null;
        if (Current is IManagedVisualizerConfigurable cfg)
        {
            cfg.LoadOptions();
            _optionsHost.Child = cfg.BuildOptionsView();
        }
    }

    private sealed class VisualizerCanvas : Control
    {
        private readonly Func<IManagedVisualizer?> _get;
        private DateTime _start = DateTime.UtcNow;
        public VisualizerCanvas(Func<IManagedVisualizer?> get) => _get = get;

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var viz = _get();
            var elapsed = DateTime.UtcNow - _start;
            viz?.Render(context, Bounds.Size, elapsed);
        }
    }
}

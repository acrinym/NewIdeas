using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cycloside.Plugins.BuiltIn; // AudioData
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.Visualizers;

/// <summary>
/// Wraps the Phoenix AVS engine inside Cycloside's managed visualizer host.
/// Renders Superscope-like presets using Avalonia's DrawingContext.
/// </summary>
public sealed class PhoenixAvsVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly AvsEngine _engine = new();
    private AudioData _latest = new(new byte[1152], new byte[1152]);
    private DateTime _start;
    private string _presetKey = "PhoenixAvs.Preset";

    public string Name => "Phoenix AVS";
    public string Description => "Phoenix engine (AVS Superscope)";

    public void Init()
    {
        _start = DateTime.UtcNow;
        var saved = StateManager.Get(_presetKey);
        _engine.Initialize(800, 500);
        _engine.LoadPreset(string.IsNullOrWhiteSpace(saved) ? DefaultPreset : saved);
    }

    public void UpdateAudioData(AudioData data) => _latest = data;

    public void Render(DrawingContext context, Size size, TimeSpan elapsed)
    {
        _engine.Resize((int)size.Width, (int)size.Height);
        var features = BuildFeatures(_latest, (DateTime.UtcNow - _start).TotalSeconds);
        var canvas = new AvaloniaCanvasAdapter(context, size);
        _engine.RenderFrame(features, canvas);
        // Overlay subtle grid
        var grid = ManagedVisStyle.Grid();
        var step = Math.Max(30, size.Width / 16);
        for (double x = 0; x < size.Width; x += step) context.DrawLine(grid, new Point(x, 0), new Point(x, size.Height));
        for (double y = 0; y < size.Height; y += step) context.DrawLine(grid, new Point(0, y), new Point(size.Width, y));
    }

    public void Dispose() { }

    public string ConfigKey => _presetKey;

    public Control BuildOptionsView()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        var tb = new TextBox { AcceptsReturn = true, MinHeight = 120, TextWrapping = TextWrapping.Wrap, Text = DefaultPreset };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var apply = new Button { Content = "Apply" };
        var open = new Button { Content = "Open .avs..." };

        apply.Click += (_, __) =>
        {
            var txt = tb.Text ?? string.Empty;
            _engine.LoadPreset(txt);
            StateManager.Set(_presetKey, txt);
        };

        open.Click += async (_, __) =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Open AVS Superscope Preset",
                FileTypeFilter = new[] { new FilePickerFileType("AVS/Superscope") { Patterns = new[] { "*.avs", "*.txt" } } }
            });
            var file = files.FirstOrDefault();
            if (file != null)
            {
                await using var s = await file.OpenReadAsync();
                using var sr = new StreamReader(s);
                var text = await sr.ReadToEndAsync();
                tb.Text = text;
                _engine.LoadPreset(text);
                StateManager.Set(_presetKey, text);
            }
        };

        row.Children.Add(apply);
        row.Children.Add(open);
        root.Children.Add(new TextBlock { Text = "Phoenix AVS preset:" });
        root.Children.Add(tb);
        root.Children.Add(row);
        return root;
    }

    public void LoadOptions() { }

    private static IAudioFeatures BuildFeatures(AudioData data, double time)
    {
        // Convert byte [0..255] to float [0..1]
        float[] ToFloats(byte[] src)
        {
            var n = src.Length;
            var f = new float[n];
            for (int i = 0; i < n; i++) f[i] = src[i] / 255f;
            return f;
        }
        var fft = ToFloats(data.Spectrum);
        var wav = ToFloats(data.Waveform);
        double sum = 0; for (int i = 0; i < Math.Min(512, wav.Length); i++) { var v = (wav[i] * 2 - 1); sum += v * v; }
        var rms = (float)Math.Sqrt(sum / Math.Max(1, Math.Min(512, wav.Length)));
        var energy = rms;
        var peak = fft.Length > 0 ? fft.Max() : 0f;
        return new Features
        {
            Fft = fft,
            Waveform = wav,
            Rms = rms,
            Bpm = 0,
            Beat = energy > 0.65f,
            Bass = TakeBand(fft, 0.00, 0.15),
            Mid = TakeBand(fft, 0.15, 0.5),
            Treble = TakeBand(fft, 0.5, 1.0),
            Energy = energy,
            Volume = energy,
            Peak = peak,
            TimeSeconds = time,
            FrequencyBands = new[] { TakeBand(fft,0,0.1f), TakeBand(fft,0.1f,0.2f), TakeBand(fft,0.2f,0.4f), TakeBand(fft,0.4f,0.8f), TakeBand(fft,0.8f,1.0f) },
            SmoothedFft = fft.ToArray(),
        };
        static float TakeBand(float[] f, double s, double e)
        {
            int a = (int)(s * (f.Length - 1));
            int b = (int)(e * (f.Length - 1));
            if (b < a) (a, b) = (b, a);
            a = Math.Clamp(a, 0, f.Length - 1); b = Math.Clamp(b, 0, f.Length - 1);
            if (b == a) return f[a];
            float m = 0; for (int i = a; i <= b; i++) m = Math.Max(m, f[i]);
            return m;
        }
    }

    private sealed class Features : IAudioFeatures
    {
        public required float[] Fft { get; init; }
        public required float[] Waveform { get; init; }
        public required float Rms { get; init; }
        public required double Bpm { get; init; }
        public required bool Beat { get; init; }
        public required float Bass { get; init; }
        public required float Mid { get; init; }
        public required float Treble { get; init; }
        public required float Energy { get; init; }
        public required float Volume { get; init; }
        public required float Peak { get; init; }
        public required double TimeSeconds { get; init; }
        public required float[] FrequencyBands { get; init; }
        public required float[] SmoothedFft { get; init; }
    }

    private const string DefaultPreset = "Init: n=256\nFrame: \nBeat: \nPoint: x=i*2-1; y=sin(i*6.283*4+t)*0.5*v";
}

using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn; // AudioData record
using Cycloside.Visuals.Managed.PhoenixCompat;

namespace Cycloside.Visuals.Managed.Visualizers;

/// <summary>
/// Generic wrapper that hosts a Phoenix-compatible visualizer inside Cycloside's managed host.
/// It adapts AudioData to IAudioFeatures and DrawingContext to ISimpleCanvas.
/// </summary>
public class PhoenixManagedWrapper<T> : IManagedVisualizer where T : IVisualizerPlugin, new()
{
    private readonly T _plugin = new();
    private AudioData _latest = new(new byte[1152], new byte[1152]);
    private DateTime _start;

    public string Name => _plugin.DisplayName;
    public string Description => $"Phoenix: {_plugin.Id}";

    public void Init()
    {
        _start = DateTime.UtcNow;
        _plugin.Initialize(800, 500);
    }

    public void UpdateAudioData(AudioData data) => _latest = data;

    public void Render(DrawingContext context, Size size, TimeSpan elapsed)
    {
        _plugin.Resize((int)size.Width, (int)size.Height);
        var canvas = new PhoenixCompat.AvaloniaCanvasAdapter(context, size);
        var features = BuildFeatures(_latest, (DateTime.UtcNow - _start).TotalSeconds);
        _plugin.RenderFrame(features, canvas);
    }

    public void Dispose()
    {
        _plugin.Dispose();
    }

    private static IAudioFeatures BuildFeatures(AudioData data, double time)
    {
        float[] ToFloats(byte[] src)
        {
            var f = new float[src.Length];
            for (int i = 0; i < src.Length; i++) f[i] = src[i] / 255f;
            return f;
        }
        var fft = ToFloats(data.Spectrum);
        var wav = ToFloats(data.Waveform);
        double sum = 0; int cnt = Math.Min(512, wav.Length);
        for (int i = 0; i < cnt; i++) { var v = wav[i] * 2 - 1; sum += v * v; }
        var rms = (float)Math.Sqrt(sum / Math.Max(1, cnt));
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
            FrequencyBands = new[] { TakeBand(fft, 0, 0.1f), TakeBand(fft, 0.1f, 0.2f), TakeBand(fft, 0.2f, 0.4f), TakeBand(fft, 0.4f, 0.8f), TakeBand(fft, 0.8f, 1.0f) },
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
}


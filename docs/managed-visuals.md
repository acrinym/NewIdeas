# Managed Visualizers

Cycloside ships a fully managed visualization system implemented in C# with Avalonia. It does not depend on Winamp or native plug‑ins and is suitable for publishing.

## Using the Managed Visual Host

1. Enable the plugin “Managed Visual Host” from the Plugins menu.
2. Start the MP3 player; visuals respond to the published audio data.
3. Use the top bar in the visualizer window to:
   - Switch visualizers
   - Toggle “Use native colors” (adopt OS accent/background)
   - Pick a preset theme (Neon, Classic, Fire, Ocean, Matrix, Sunset)
   - Adjust global Sensitivity
4. Some visualizers expose an options strip below the canvas (e.g., bar count, particle density). Changes persist.

## Included Visualizers

- Spectrum Bars — classic FFT bars with peak hold
- WMP Bars — Windows Media Player‑style bars
- Circular Spectrum — radial bars around a ring
- Oscilloscope — stereo waveform lines
- Spectrogram — scrolling frequency heatmap
- Matrix Rain — falling code, bass‑driven
- Lava Lamp — gradient blobs modulated by amplitude
- Particle Pulse — beat‑driven bursts from the center
- Starfield — 3D starfield with speed on beats
- Polar Wave — radial waveform rings

## How It Works

- The MP3 player publishes `AudioData` (stereo spectrum + waveform) on the `PluginBus` topic `audio:data`.
- The Managed Visual Host subscribes to `audio:data` and forwards it to the active visualizer.
- Visualizers implement `IManagedVisualizer`:

```csharp
public interface IManagedVisualizer : IDisposable
{
    string Name { get; }
    string Description { get; }
    void Init();
    void UpdateAudioData(AudioData data);
    void Render(DrawingContext context, Size size, TimeSpan elapsed);
}
```

- Optional: `IManagedVisualizerConfigurable` provides a small options `Control` and loads values persisted in `StateManager`.
- Visualizers are discovered by reflection (public parameterless constructor required).

## Styling

Call into `ManagedVisStyle` to respect user selections:

- `Background()` – background brush (native or preset)
- `Accent()` / `Secondary()` / `Peak()` – accent brushes
- `Grid()` – a dashed grid pen
- `Sensitivity` – global gain applied by several visualizers

## Performance Tips

- Allocate once: reuse buffers and geometries where possible.
- Keep `Render(...)` light; rely on incoming `AudioData` for state.
- Prefer `StreamGeometry` for poly‑lines and `DrawGeometry` for fills.


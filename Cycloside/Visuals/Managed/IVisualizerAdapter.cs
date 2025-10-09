// ============================================================================
// VISUALIZER ADAPTER - Bridge between old and new audio systems
// ============================================================================
// Purpose: Allow existing visualizers to work with EnhancedAudioService
// Features: Convert AudioAnalysisResult to legacy AudioData format
// Dependencies: EnhancedAudioService (new), IManagedVisualizer (legacy)
// ============================================================================

using Avalonia;
using Avalonia.Media;
using Cycloside.Services;
using Cycloside.Plugins.BuiltIn; // For AudioData
using System;
using System.Linq;

namespace Cycloside.Visuals.Managed;

public interface IVisualizerAdapter
{
    /// <summary>
    /// Convert modern AudioAnalysisResult to legacy AudioData format
    /// </summary>
    AudioData ConvertAudioData(AudioAnalysisResult analysis);

    /// <summary>
    /// Update visualizer with new audio data
    /// </summary>
    void UpdateVisualizer(IManagedVisualizer visualizer, AudioAnalysisResult analysis);

    /// <summary>
    /// Render visualizer with modern rendering context
    /// </summary>
    void RenderVisualizer(IManagedVisualizer visualizer, DrawingContext context, Size size, TimeSpan elapsed, AudioAnalysisResult analysis);
}

public class VisualizerAdapter : IVisualizerAdapter
{
    public AudioData ConvertAudioData(AudioAnalysisResult analysis)
    {
        // Convert float[] spectrum to byte[] (0-255 range)
        var spectrumBytes = new byte[analysis.SpectrumData.Length];
        for (int i = 0; i < spectrumBytes.Length && i < analysis.SpectrumData.Length; i++)
        {
            spectrumBytes[i] = (byte)Math.Clamp(analysis.SpectrumData[i] * 255, 0, 255);
        }

        // Convert float[] waveform to byte[] (normalize to 0-255)
        var waveformBytes = new byte[analysis.WaveformData.Length];
        var maxAmplitude = analysis.WaveformData.Length > 0 ? analysis.WaveformData.Select(x => Math.Abs(x)).Max() : 1.0f;
        for (int i = 0; i < waveformBytes.Length && i < analysis.WaveformData.Length; i++)
        {
            waveformBytes[i] = (byte)((analysis.WaveformData[i] / maxAmplitude + 1.0f) * 127.5f);
        }

        return new AudioData(spectrumBytes, waveformBytes);
    }

    public void UpdateVisualizer(IManagedVisualizer visualizer, AudioAnalysisResult analysis)
    {
        var legacyData = ConvertAudioData(analysis);
        visualizer.UpdateAudioData(legacyData);
    }

    public void RenderVisualizer(IManagedVisualizer visualizer, DrawingContext context, Size size, TimeSpan elapsed, AudioAnalysisResult analysis)
    {
        UpdateVisualizer(visualizer, analysis);
        visualizer.Render(context, size, elapsed);
    }
}

using System;
using System.IO;
using NAudio.Wave;

namespace Cycloside.Services;

/// <summary>
/// Simple audio playback helper for short sound effects.
/// </summary>
public static class AudioService
{
    private const int MaxConcurrent = 4;
    private static int _active;
    /// <summary>
    /// Plays the specified audio file if it exists.
    /// </summary>
    public static void Play(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            // Limit concurrent short sound plays to avoid resource churn.
            if (System.Threading.Interlocked.Increment(ref _active) > MaxConcurrent)
            {
                System.Threading.Interlocked.Decrement(ref _active);
                return;
            }

            var reader = new AudioFileReader(path);
            var output = new WaveOutEvent();
            output.Init(reader);
            output.Play();
            output.PlaybackStopped += (_, _) =>
            {
                output.Dispose();
                reader.Dispose();
                System.Threading.Interlocked.Decrement(ref _active);
            };
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio playback error for {path}: {ex.Message}");
            System.Threading.Interlocked.Decrement(ref _active);
        }
    }
}

using System;
using System.IO;
using NAudio.Wave;

namespace Cycloside.Services;

/// <summary>
/// Simple audio playback helper for short sound effects.
/// </summary>
public static class AudioService
{
    /// <summary>
    /// Plays the specified audio file if it exists.
    /// </summary>
    public static void Play(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            var reader = new AudioFileReader(path);
            var output = new WaveOutEvent();
            output.Init(reader);
            output.Play();
            output.PlaybackStopped += (_, _) =>
            {
                output.Dispose();
                reader.Dispose();
            };
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio playback error for {path}: {ex.Message}");
        }
    }
}

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
        if (!ThemeSecurityValidator.CheckFileSize(path, ThemeSecurityValidator.MaxAudioFileSize))
            return;
        if (BinaryFormatValidator.IsDataUri(path))
            return;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".wav" && !BinaryFormatValidator.ValidateWavStructure(path))
            return;
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

            // Timeout safety: if PlaybackStopped never fires (corrupted audio,
            // NAudio bug), force cleanup after 30 seconds to prevent permanent
            // resource leak and _active counter exhaustion (CYC-2026-018).
            bool cleaned = false;
            var cleanupLock = new object();
            void Cleanup()
            {
                lock (cleanupLock)
                {
                    if (cleaned) return;
                    cleaned = true;
                }
                try { output.Stop(); } catch { }
                try { output.Dispose(); } catch { }
                try { reader.Dispose(); } catch { }
                System.Threading.Interlocked.Decrement(ref _active);
            }

            output.PlaybackStopped += (_, _) => Cleanup();

            System.Threading.Tasks.Task.Delay(30_000).ContinueWith(_ => Cleanup());
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio playback error for {path}: {ex.Message}");
            System.Threading.Interlocked.Decrement(ref _active);
        }
    }
}

using System;
using System.IO;
using NAudio.Wave;
using NAudio.Vorbis;

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

            using var readerFactory = CreateReader(path);
            var output = new WaveOutEvent();
            var reader = readerFactory.DetachReader();
            try
            {
                output.Init(reader);
                output.Play();
                output.PlaybackStopped += (_, _) =>
                {
                    output.Dispose();
                    reader.Dispose();
                    System.Threading.Interlocked.Decrement(ref _active);
                };
            }
            catch
            {
                output.Dispose();
                reader.Dispose();
                throw;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio playback error for {path}: {ex.Message}");
            System.Threading.Interlocked.Decrement(ref _active);
        }
    }

    private static ReaderLease CreateReader(string path)
    {
        if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            return new ReaderLease(new VorbisWaveReader(path));
        }

        return new ReaderLease(new AudioFileReader(path));
    }

    private sealed class ReaderLease : IDisposable
    {
        private WaveStream? _reader;

        public ReaderLease(WaveStream reader)
        {
            _reader = reader;
        }

        public WaveStream DetachReader()
        {
            if (_reader == null)
            {
                throw new InvalidOperationException("Audio reader was already detached.");
            }

            var reader = _reader;
            _reader = null;
            return reader;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

internal static class JezzballSound
{
    public static readonly Dictionary<JezzballSoundEvent, string> Paths = new();

    public static void Play(JezzballSoundEvent ev)
    {
        if (Paths.TryGetValue(ev, out var path) && !string.IsNullOrWhiteSpace(path))
        {
            AudioService.Play(path);
            return;
        }
        PlayFallback(ev);
    }

    private static void PlayFallback(JezzballSoundEvent ev)
    {
        var (freq, ms) = ev switch
        {
            JezzballSoundEvent.Click => (800, 30),
            JezzballSoundEvent.WallBuild => (400, 50),
            JezzballSoundEvent.WallHit => (200, 80),
            JezzballSoundEvent.WallBreak => (150, 100),
            JezzballSoundEvent.BallBounce => (600, 25),
            JezzballSoundEvent.LevelComplete => (523.25, 150),
            _ => (440, 40)
        };
        try
        {
            var sampleRate = 44100;
            var gen = new SignalGenerator(sampleRate, 1)
            {
                Gain = 0.15,
                Frequency = freq,
                Type = SignalGeneratorType.Sin
            };
            var samplesNeeded = (int)(sampleRate * ms / 1000.0);
            var buffer = new float[samplesNeeded];
            gen.Read(buffer, 0, samplesNeeded);
            var bytes = BufferFloatTo16(buffer);
            var provider = new RawSourceWaveStream(new MemoryStream(bytes), new WaveFormat(sampleRate, 16, 1));
            var output = new WaveOutEvent();
            output.Init(provider);
            output.PlaybackStopped += (_, _) =>
            {
                provider.Dispose();
                output.Dispose();
            };
            output.Play();
        }
        catch (Exception ex)
        {
            Logger.Log($"Jezzball sound fallback: {ex.Message}");
        }
    }

    private static byte[] BufferFloatTo16(float[] buffer)
    {
        var result = new byte[buffer.Length * 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            var sample = (short)Math.Max(-32768, Math.Min(32767, buffer[i] * 32767));
            result[i * 2] = (byte)(sample & 0xFF);
            result[i * 2 + 1] = (byte)(sample >> 8);
        }
        return result;
    }
}
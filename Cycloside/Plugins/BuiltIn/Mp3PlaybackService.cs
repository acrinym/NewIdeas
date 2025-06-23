using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

internal static class Mp3PlaybackService
{
    private static readonly List<string> _playlist = new();
    private static int _index = -1;
    private static IWavePlayer? _output;
    private static AudioFileReader? _reader;

    public static string? CurrentFile =>
        _index >= 0 && _index < _playlist.Count ? _playlist[_index] : null;

    public static void LoadFiles(IEnumerable<string> files)
    {
        Stop();
        _playlist.Clear();
        _playlist.AddRange(files.Where(f => File.Exists(f)));
        _index = _playlist.Count > 0 ? 0 : -1;
    }

    private static void OpenReader(string file)
    {
        _reader = new AudioFileReader(file);
        _output = new WaveOutEvent();
        _output.Init(_reader);
    }

    public static void Play()
    {
        if (_playlist.Count == 0)
            return;
        if (_output == null || _reader == null)
            OpenReader(CurrentFile!);
        _output.Play();
    }

    public static void Pause() => _output?.Pause();

    public static void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }

    public static void Next()
    {
        if (_playlist.Count == 0)
            return;
        _index = (_index + 1) % _playlist.Count;
        Restart();
    }

    public static void Previous()
    {
        if (_playlist.Count == 0)
            return;
        _index = (_index - 1 + _playlist.Count) % _playlist.Count;
        Restart();
    }

    private static void Restart()
    {
        Stop();
        OpenReader(CurrentFile!);
        _output?.Play();
    }
}


using System;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace Cycloside.Plugins.BuiltIn;

public class MP3PlayerPlugin : IPlugin
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;

    public string Name => "MP3 Player";
    public string Description => "Plays MP3 files located in the Music folder.";
    public Version Version => new(1,0,0);

    public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget();

    public void Start()
    {
        var musicDir = Path.Combine(AppContext.BaseDirectory, "Music");
        Directory.CreateDirectory(musicDir);
        var file = Directory.GetFiles(musicDir, "*.mp3").FirstOrDefault();
        if (file == null)
            return;

        _reader = new AudioFileReader(file);
        _output = new WaveOutEvent();
        _output.Init(_reader);
        _output.Play();
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }
}

using Avalonia.Controls;
using Avalonia.Layout;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Widgets.BuiltIn;

public class Mp3Widget : IWidget
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;

    public string Name => "MP3 Player";

    public Control BuildView()
    {
        var playButton = new Button { Content = "Play" };
        playButton.Click += (_, _) => Play();
        var stopButton = new Button { Content = "Stop" };
        stopButton.Click += (_, _) => Stop();

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(playButton);
        panel.Children.Add(stopButton);
        return new Border
        {
            Background = Brushes.Black,
            Opacity = 0.7,
            Padding = new Thickness(4),
            Child = panel
        };
    }

    private void Play()
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

    private void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }
}

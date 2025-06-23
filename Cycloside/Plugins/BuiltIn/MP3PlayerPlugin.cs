using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

public partial class MP3PlayerPlugin : ObservableObject, IPlugin
{
    private readonly List<string> _playlist = new();
    private int _index = -1;
    private IWavePlayer? _output;
    private AudioFileReader? _reader;

    [ObservableProperty]
    private string? currentFile;

    public string Name => "MP3 Player";
    public string Description => "Play MP3 files with a simple playlist.";
    public Version Version => new(1,1,0);

    public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget(this);
    public bool ForceDefaultTheme => false;

    public void Start()
    {
    }

    public void Stop()
    {
        StopPlayback();
        CurrentFile = null;
    }

    [RelayCommand]
    private async Task OpenFilesAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var result = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select MP3 Files",
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("MP3 Files") { Patterns = new[] { "*.mp3" } } }
            });

            var files = result.Select(f => f.TryGetLocalPath()).Where(p => p != null).Cast<string>();
            if (files.Any())
            {
                LoadFiles(files);
            }
        }
    }

    private void LoadFiles(IEnumerable<string> files)
    {
        StopPlayback();
        _playlist.Clear();
        _playlist.AddRange(files.Where(File.Exists));
        _index = _playlist.Count > 0 ? 0 : -1;
        CurrentFile = _index >= 0 ? _playlist[_index] : null;
    }

    [RelayCommand]
    private void Play()
    {
        if (_playlist.Count == 0)
            return;
        if (_output == null || _reader == null)
            OpenReader(CurrentFile!);
        _output.Play();
    }

    [RelayCommand]
    private void Pause() => _output?.Pause();

    [RelayCommand]
    private void StopPlayback()
    {
        _output?.Stop();
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }

    [RelayCommand]
    private void Next()
    {
        if (_playlist.Count == 0)
            return;
        _index = (_index + 1) % _playlist.Count;
        Restart();
    }

    [RelayCommand]
    private void Previous()
    {
        if (_playlist.Count == 0)
            return;
        _index = (_index - 1 + _playlist.Count) % _playlist.Count;
        Restart();
    }

    private void Restart()
    {
        StopPlayback();
        CurrentFile = _index >= 0 && _index < _playlist.Count ? _playlist[_index] : null;
        if (CurrentFile != null)
        {
            OpenReader(CurrentFile);
            _output?.Play();
        }
    }

    private void OpenReader(string file)
    {
        _reader = new AudioFileReader(file);
        _output = new WaveOutEvent();
        _output.Init(_reader);
    }
}


using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

public class MP3PlayerPlugin : IPlugin
{
    private Window? _window;
    private TextBlock? _trackLabel;

    public string Name => "MP3 Player";
    public string Description => "Play MP3 files with a simple playlist.";
    public Version Version => new(1,1,0);

    public Widgets.IWidget? Widget => new Widgets.BuiltIn.Mp3Widget();

    public void Start()
    {
        var openButton = new Button { Content = "Open..." };
        openButton.Click += async (_, _) => await OpenFilesAsync();

        var prevButton = new Button { Content = "Prev" };
        prevButton.Click += (_, _) => { Mp3PlaybackService.Previous(); UpdateLabel(); };

        var playButton = new Button { Content = "Play" };
        playButton.Click += (_, _) => Mp3PlaybackService.Play();

        var pauseButton = new Button { Content = "Pause" };
        pauseButton.Click += (_, _) => Mp3PlaybackService.Pause();

        var stopButton = new Button { Content = "Stop" };
        stopButton.Click += (_, _) => Mp3PlaybackService.Stop();

        var nextButton = new Button { Content = "Next" };
        nextButton.Click += (_, _) => { Mp3PlaybackService.Next(); UpdateLabel(); };

        _trackLabel = new TextBlock
        {
            Text = "No file selected",
            Margin = new Thickness(5),
            Foreground = Brushes.White
        };

        var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        buttonRow.Children.Add(openButton);
        buttonRow.Children.Add(prevButton);
        buttonRow.Children.Add(playButton);
        buttonRow.Children.Add(pauseButton);
        buttonRow.Children.Add(stopButton);
        buttonRow.Children.Add(nextButton);

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(buttonRow);
        panel.Children.Add(_trackLabel);

        _window = new Window
        {
            Title = "MP3 Player",
            Width = 360,
            Height = 120,
            Content = panel
        };

        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(MP3PlayerPlugin));
        _window.Show();
    }

    private async Task OpenFilesAsync()
    {
        if (_window == null) return;

        var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select MP3 Files",
            AllowMultiple = true,
            FileTypeFilter = new[] { new FilePickerFileType("MP3 Files") { Patterns = new[] { "*.mp3" } } }
        });

        var files = result.Select(f => f.TryGetLocalPath()).Where(p => p != null).Cast<string>().ToList();
        if (files.Any())
        {
            Mp3PlaybackService.LoadFiles(files);
            UpdateLabel();
        }
    }

    private void UpdateLabel()
    {
        if (_trackLabel == null) return;
        _trackLabel.Text = Mp3PlaybackService.CurrentFile != null
            ? Path.GetFileName(Mp3PlaybackService.CurrentFile)
            : "No file selected";
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        _trackLabel = null;
        Mp3PlaybackService.Stop();
    }
}


using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Widgets.BuiltIn;

public class Mp3Widget : IWidget
{

    public string Name => "MP3 Player";

    public Control BuildView()
    {
        var openButton = new Button { Content = "Open" };
        openButton.Click += async (_, _) => await PickFilesAsync();

        var prevButton = new Button { Content = "Prev" };
        prevButton.Click += (_, _) => Mp3PlaybackService.Previous();

        var playButton = new Button { Content = "Play" };
        playButton.Click += (_, _) => Mp3PlaybackService.Play();

        var pauseButton = new Button { Content = "Pause" };
        pauseButton.Click += (_, _) => Mp3PlaybackService.Pause();

        var stopButton = new Button { Content = "Stop" };
        stopButton.Click += (_, _) => Mp3PlaybackService.Stop();

        var nextButton = new Button { Content = "Next" };
        nextButton.Click += (_, _) => Mp3PlaybackService.Next();

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(openButton);
        panel.Children.Add(prevButton);
        panel.Children.Add(playButton);
        panel.Children.Add(pauseButton);
        panel.Children.Add(stopButton);
        panel.Children.Add(nextButton);

        return new Border
        {
            Background = Brushes.Black,
            Opacity = 0.7,
            Padding = new Thickness(4),
            Child = panel
        };
    }

    private async Task PickFilesAsync()
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
                Mp3PlaybackService.LoadFiles(files);
        }
    }
}

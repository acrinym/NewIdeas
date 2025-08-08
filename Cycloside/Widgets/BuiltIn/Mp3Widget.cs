using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn;
using System;

namespace Cycloside.Widgets.BuiltIn
{
    public class Mp3Widget : IWidget
    {
        private readonly MP3PlayerPlugin? _plugin;

        public Mp3Widget()
        {
            // Parameterless constructor for Widget Host
            _plugin = null;
        }

        public Mp3Widget(MP3PlayerPlugin plugin)
        {
            _plugin = plugin;
        }

        public string Name => "MP3 Player";

        public Control BuildView()
        {
            if (_plugin == null)
            {
                return new TextBlock { Text = "MP3 Player not available", Foreground = Brushes.Red };
            }
            // --- Create Controls ---

            var trackDisplay = new TextBlock
            {
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            trackDisplay.Bind(TextBlock.TextProperty, new Binding(nameof(MP3PlayerPlugin.CurrentTrackName)));

            // A slider for showing progress and seeking
            var progressSlider = new Slider
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0)
            };
            // We bind the slider's range and value to the TimeSpan properties in the plugin, converting to TotalSeconds.
            progressSlider.Bind(RangeBase.MaximumProperty, new Binding(nameof(MP3PlayerPlugin.TotalTime.TotalSeconds)));
            progressSlider.Bind(RangeBase.ValueProperty, new Binding(nameof(MP3PlayerPlugin.CurrentTime.TotalSeconds)));

            // To implement seeking, we handle when the user releases the slider thumb.
            // This is more performant than updating on every tiny movement.
            progressSlider.AddHandler(InputElement.PointerReleasedEvent, (s, e) =>
            {
                // We execute the SeekCommand with the slider's final value as a TimeSpan.
                if (_plugin.SeekCommand.CanExecute(null))
                {
                    _plugin.SeekCommand.Execute(TimeSpan.FromSeconds(progressSlider.Value));
                }
            }, RoutingStrategies.Tunnel);

            // TextBlocks to display "01:23 / 04:56" style time.
            // We use StringFormat in the binding to format the TimeSpan without needing a converter.
            var currentTimeText = new TextBlock { Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center };
            currentTimeText.Bind(TextBlock.TextProperty, new Binding(nameof(MP3PlayerPlugin.CurrentTime)) { StringFormat = "mm\\:ss" });

            var totalTimeText = new TextBlock { Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center };
            totalTimeText.Bind(TextBlock.TextProperty, new Binding(nameof(MP3PlayerPlugin.TotalTime)) { StringFormat = "mm\\:ss" });

            // Buttons are bound using the type-safe nameof() operator.
            var openButton = new Button { Content = "Open", Command = _plugin.AddFilesCommand };
            var prevButton = new Button { Content = "◀", Command = _plugin.PreviousCommand };
            var playButton = new Button { Content = "▶", Command = _plugin.PlayCommand };
            var pauseButton = new Button { Content = "❚❚", Command = _plugin.PauseCommand };
            var stopButton = new Button { Content = "■", Command = _plugin.StopPlaybackCommand };
            var nextButton = new Button { Content = "▶|", Command = _plugin.NextCommand };

            // --- Assemble Layout using a Grid for precise alignment ---

            var mainPanel = new Grid
            {
                DataContext = _plugin, // Set DataContext for all children
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), // 3 rows for track, progress, buttons
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), // 3 columns for time, slider, time
                Margin = new Thickness(4)
            };

            // Row 0: Track Name
            Grid.SetRow(trackDisplay, 0);
            Grid.SetColumn(trackDisplay, 0);
            Grid.SetColumnSpan(trackDisplay, 3);

            // Row 1: Progress Bar and Times
            Grid.SetRow(currentTimeText, 1);
            Grid.SetColumn(currentTimeText, 0);
            Grid.SetRow(progressSlider, 1);
            Grid.SetColumn(progressSlider, 1);
            Grid.SetRow(totalTimeText, 1);
            Grid.SetColumn(totalTimeText, 2);

            // Row 2: Button Panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            buttonPanel.Children.AddRange(new Control[] { openButton, prevButton, playButton, pauseButton, stopButton, nextButton });

            Grid.SetRow(buttonPanel, 2);
            Grid.SetColumn(buttonPanel, 0);
            Grid.SetColumnSpan(buttonPanel, 3);

            mainPanel.Children.AddRange(new Control[] { trackDisplay, currentTimeText, progressSlider, totalTimeText, buttonPanel });

            return new Border
            {
                Background = Brushes.Black,
                Opacity = 0.75,
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(5),
                Child = mainPanel
            };
        }
    }
}
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Widgets.BuiltIn
{
    /// <summary>
    /// The View for the MP3 Player plugin. This class is responsible for building
    /// the UI controls and binding them to the MP3PlayerPlugin instance (which acts as the ViewModel).
    /// </summary>
    public class Mp3Widget : IWidget
    {
        private readonly MP3PlayerPlugin _plugin;

        /// <summary>
        /// The widget receives an instance of its parent plugin via dependency injection.
        /// This decouples the View from the playback logic.
        /// </summary>
        public Mp3Widget(MP3PlayerPlugin plugin)
        {
            _plugin = plugin;
        }

        public string Name => "MP3 Player";

        /// <summary>
        /// Builds the UI for the widget.
        /// </summary>
        public Control BuildView()
        {
            // --- Create Controls ---

            // The TextBlock for displaying the current track name.
            // It binds its Text property to the CurrentTrackName property on the plugin.
            var trackDisplay = new TextBlock
            {
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            trackDisplay.Bind(TextBlock.TextProperty, new Binding(nameof(MP3PlayerPlugin.CurrentTrackName)));

            // Buttons are bound to IRelayCommands on the plugin instance.
            // This separates the UI action (a click) from the implementation logic.
            var openButton = new Button { Content = "Open" };
            openButton.Bind(Button.CommandProperty, new Binding(nameof(MP3PlayerPlugin.OpenFilesCommand)));

            var prevButton = new Button { Content = "◀" };
            prevButton.Bind(Button.CommandProperty, new Binding(nameof(MP3PlayerPlugin.PreviousCommand)));

            var playButton = new Button { Content = "▶" };
            playButton.Bind(Button.CommandProperty, new Binding(nameof(MP3PlayerPlugin.PlayCommand)));

            var pauseButton = new Button { Content = "❚❚" };
            pauseButton.Bind(Button.CommandProperty, new Binding(nameof(MP3PlayerPlugin.PauseCommand)));

            var stopButton = new Button { Content = "■" };
            stopButton.Bind(Button.CommandProperty, new Binding(nameof(MP3PlayerPlugin.StopCommand)));

            var nextButton = new Button { Content = "▶|" };
            nextButton.Bind(Button.CommandProperty, new Binding(nameof(MP3PlayerPlugin.NextCommand)));

            // --- Assemble Layout ---

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            buttonPanel.Children.Add(openButton);
            buttonPanel.Children.Add(prevButton);
            buttonPanel.Children.Add(playButton);
            buttonPanel.Children.Add(pauseButton);
            buttonPanel.Children.Add(stopButton);
            buttonPanel.Children.Add(nextButton);

            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                DataContext = _plugin // Set the DataContext for all children to the plugin instance.
            };
            mainPanel.Children.Add(trackDisplay);
            mainPanel.Children.Add(buttonPanel);

            // The final control is wrapped in a semi-transparent Border for styling.
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
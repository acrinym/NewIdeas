using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Widgets;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// HACKER'S PARADISE DEMO PLUGIN
    /// Showcase of the ultimate Cycloside features for hackers and makers
    /// </summary>
    public class HackersParadisePlugin : IPlugin
    {
        public string Name => "Hacker's Paradise";
        public Version Version => new Version(1, 0, 0);
        public string Description => "Ultimate Cycloside showcase: Live Code Canvas, Hardware Bridge, Network Tools";
        public bool ForceDefaultTheme => false;
        public IWidget? Widget => CreateHackerDashboardWidget();

        public void Start()
        {
            Logger.Log("🚀 Hacker's Paradise initialized - Let the hacking begin! 🔥");

            // Show the widget
            if (Widget != null)
            {
                var widgetWindow = new Window
                {
                    Title = Widget.Name,
                    Content = Widget.BuildView(),
                    Width = 500,
                    Height = 400,
                    Background = Brushes.Black
                };

                widgetWindow.Show();
            }
        }

        public void Stop()
        {
            Logger.Log("🛑 Hacker's Paradise shutting down");
        }

        private IWidget CreateHackerDashboardWidget()
        {
            return new HackerDashboardWidget();
        }
    }

    /// <summary>
    /// Hacker's Paradise dashboard widget implementation
    /// </summary>
    public class HackerDashboardWidget : IWidget
    {
        public string Name => "🔮 Hacker's Paradise";

        public Control BuildView()
        {
            var panel = new StackPanel
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Colors.Black, 0.0),
                        new GradientStop(Colors.DarkBlue, 0.5),
                        new GradientStop(Colors.DarkGreen, 1.0)
                    }
                },
                Margin = new Thickness(10),
                Spacing = 10
            };

            // Awesome hacker title
            var title = new TextBlock
            {
                Text = "🔮 CYCLOSIDE HACKER'S PARADISE 🔮",
                FontSize = 20,
                Foreground = Brushes.Lime,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold
            };

            // Subtitle
            var subtitle = new TextBlock
            {
                Text = "Rainmeter × VSCode × Wireshark × Arduino IDE",
                FontSize = 12,
                Foreground = Brushes.Cyan,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontStyle = FontStyle.Italic
            };

            // Features list
            var featuresList = new StackPanel { Spacing = 8 };

            var features = new[]
            {
                ("🚀", "Live Code Canvas", "Drop .axaml files for instant preview, run Roslyn scripts!"),
                ("🔌", "ESP32 Serial Bridge", "Connect sensors, Arduino, Raspberry Pi to Cycloside!"),
                ("📡", "Packet Sniffer", "Mini-Wireshark for network analysis and debugging!"),
                ("🔱", "Binary Editor", "View and patch binary files with advanced hex editing!"),
                ("⚡", "AI Co-Pilot", "Local LLM integration for debugging and automation!"),
                ("🎛️", "Visualizer Host", "Enhanced Winamp visual presets and shader playground!"),
                ("🕹️", "Retro Emulator", "Embedded GameBoy, NES, Sega cores for mini-games!"),
                ("🌐", "Remote Control API", "Control Cycloside widgets via WebSocket/IoT!")
            };

            foreach (var (icon, name, desc) in features)
            {
                var featurePanel = new StackPanel
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    Margin = new Thickness(5)
                };

                var featureName = new TextBlock
                {
                    Text = $"{icon} {name}",
                    FontSize = 14,
                    Foreground = Brushes.Yellow,
                    FontWeight = FontWeight.Bold
                };

                var featureDesc = new TextBlock
                {
                    Text = desc,
                    FontSize = 10,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                };

                featurePanel.Children.Add(featureName);
                featurePanel.Children.Add(featureDesc);
                featuresList.Children.Add(featurePanel);
            }

            // Status area
            var statusPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0)),
                BorderBrush = Brushes.Lime,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(10, 10, 10, 0)
            };

            var statusText = new StackPanel();
            var statusTitle = new TextBlock
            {
                Text = "⚡ SYSTEM STATUS ⚡",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Lime,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var statusMessage = new TextBlock
            {
                Text = "🚀 Ready for ultimate hacking adventures!",
                FontSize = 11,
                Foreground = Brushes.Cyan,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            statusText.Children.Add(statusTitle);
            statusText.Children.Add(statusMessage);
            statusPanel.Child = statusText;

            // Add everything to main panel
            panel.Children.Add(title);
            panel.Children.Add(subtitle);
            panel.Children.Add(featuresList);
            panel.Children.Add(statusPanel);

            return panel;
        }
    }
}
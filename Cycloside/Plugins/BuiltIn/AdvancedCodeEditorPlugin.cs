using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Views;
using Cycloside.Widgets;
using Cycloside.Plugins.BuiltIn.Views;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// ADVANCED CODE EDITOR - Professional IDE-like code editing experience
    /// Features: Syntax highlighting, IntelliSense, code folding, multi-language support
    /// </summary>
    public class AdvancedCodeEditorPlugin : IPlugin
    {
        public string Name => "Advanced Code Editor";
        public string Description => "Professional code editor with syntax highlighting and IntelliSense";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.Development;

        public class AdvancedCodeEditorWidget : IWidget
        {
            public string Name => "Advanced Code Editor";

            private AdvancedCodeEditorWindow? _editorWindow;

            public Control BuildView()
            {
                var mainPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10)
                };

                // Header
                var headerText = new TextBlock
                {
                    Text = "💻 Advanced Code Editor",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var descriptionText = new TextBlock
                {
                    Text = "Professional IDE-like code editing with syntax highlighting",
                    FontSize = 12,
                    Opacity = 0.8,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // Feature highlights
                var featuresPanel = new StackPanel
                {
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var features = new[]
                {
                    "🎨 Syntax Highlighting",
                    "📄 File Operations",
                    "🌍 Multi-Language",
                    "⚡ Fast & Lightweight"
                };

                foreach (var feature in features)
                {
                    var featureText = new TextBlock
                    {
                        Text = $"• {feature}",
                        FontSize = 12,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    featuresPanel.Children.Add(featureText);
                }

                // Action buttons
                var buttonsPanel = new StackPanel
                {
                    Spacing = 10
                };

                var openEditorButton = new Button
                {
                    Content = "🚀 Open Editor",
                    Background = Avalonia.Media.Brushes.DodgerBlue,
                    Foreground = Avalonia.Media.Brushes.White,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Padding = new Thickness(15, 8)
                };
                openEditorButton.Click += OnOpenEditor;

                var newFileButton = new Button
                {
                    Content = "📄 New File",
                    Background = Avalonia.Media.Brushes.Green,
                    Foreground = Avalonia.Media.Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                newFileButton.Click += OnNewFile;

                var openFileButton = new Button
                {
                    Content = "📂 Open File",
                    Background = Avalonia.Media.Brushes.Orange,
                    Foreground = Avalonia.Media.Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                openFileButton.Click += OnOpenFile;

                buttonsPanel.Children.Add(openEditorButton);
                buttonsPanel.Children.Add(newFileButton);
                buttonsPanel.Children.Add(openFileButton);

                // Status area
                var statusPanel = new Border
                {
                    Background = Avalonia.Media.Brushes.LightYellow,
                    BorderBrush = Avalonia.Media.Brushes.Goldenrod,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(5),
                    Margin = new Thickness(0, 15, 0, 0),
                    Padding = new Thickness(10)
                };

                var statusText = new TextBlock
                {
                    Text = "Ready to edit code with professional tools",
                    FontSize = 12
                };

                statusPanel.Child = statusText;

                mainPanel.Children.Add(headerText);
                mainPanel.Children.Add(descriptionText);
                mainPanel.Children.Add(featuresPanel);
                mainPanel.Children.Add(buttonsPanel);
                mainPanel.Children.Add(statusPanel);

                var border = new Border
                {
                    Child = mainPanel,
                    Background = Avalonia.Media.Brushes.White,
                    BorderBrush = Avalonia.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Margin = new Thickness(10)
                };

                return border;
            }

            private void OnOpenEditor(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                try
                {
                    if (_editorWindow == null)
                    {
                        _editorWindow = new AdvancedCodeEditorWindow();
                        _editorWindow.Closed += (_, _) => _editorWindow = null;
                    }

                    _editorWindow.Show();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error opening code editor: {ex.Message}");
                }
            }

            private void OnNewFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                try
                {
                    if (_editorWindow == null)
                    {
                        _editorWindow = new AdvancedCodeEditorWindow();
                        _editorWindow.Closed += (_, _) => _editorWindow = null;
                    }

                    // The editor window will handle creating a new tab
                    _editorWindow.Show();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error creating new file: {ex.Message}");
                }
            }

            private void OnOpenFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                try
                {
                    if (_editorWindow == null)
                    {
                        _editorWindow = new AdvancedCodeEditorWindow();
                        _editorWindow.Closed += (_, _) => _editorWindow = null;
                    }

                    // The editor window will handle opening a file
                    _editorWindow.Show();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error opening file: {ex.Message}");
                }
            }
        }

        public IWidget? Widget => new AdvancedCodeEditorWidget();

        public void Start()
        {
            Logger.Log("🚀 Advanced Code Editor Plugin started - Professional IDE-like code editing ready!");

            // Initialize any required services for the code editor
            // (Syntax highlighting, IntelliSense data, etc.)
        }

        public void Stop()
        {
            Logger.Log("🛑 Advanced Code Editor Plugin stopped");
        }
    }
}

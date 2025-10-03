using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn.Views
{
    /// <summary>
    /// Advanced Code Editor - Professional IDE-like code editing experience
    /// Simplified version with core features working
    /// </summary>
    public partial class AdvancedCodeEditorWindow : Window
    {
        public AdvancedCodeEditorWindow()
        {
            InitializeComponent();
            SetupEventHandlers();
            InitializeEditor();
        }

        private void InitializeEditor()
        {
            if (MainEditor != null)
            {
                MainEditor.Text = @"using System;

namespace Cycloside.Editor
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine(""Welcome to Cycloside Advanced Code Editor!"");
        }
    }
}";
            }
        }

        private void SetupEventHandlers()
        {
            // File operations
            NewFileButton.Click += OnNewFile;
            OpenFileButton.Click += OnOpenFile;
            SaveFileButton.Click += OnSaveFile;

            // Language and theme
            LanguageComboBox.SelectionChanged += OnLanguageChanged;

            // View options
            WordWrapToggle.Click += OnWordWrapToggle;
            LineNumbersToggle.Click += OnLineNumbersToggle;
        }

        private void OnNewFile(object? sender, RoutedEventArgs e)
        {
            // Reset editor content for new file
            if (MainEditor != null)
            {
                MainEditor.Text = "// New file - start coding!";
            }
        }

        private async void OnOpenFile(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open File",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "All Supported Files", Extensions = new List<string> { "cs", "py", "js", "html", "css", "json" } },
                    new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                }
            };

            var result = await dialog.ShowAsync(this);
            if (result != null && result.Any())
            {
                var filePath = result.First();
                var content = await File.ReadAllTextAsync(filePath);

                if (MainEditor != null)
                {
                    MainEditor.Text = content;
                }
            }
        }

        private async void OnSaveFile(object? sender, RoutedEventArgs e)
        {
            if (MainEditor?.Text == null) return;

            var dialog = new SaveFileDialog
            {
                Title = "Save File",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                }
            };

            var result = await dialog.ShowAsync(this);
            if (result != null)
            {
                await File.WriteAllTextAsync(result, MainEditor.Text);
            }
        }

        private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                var language = item.Content?.ToString() ?? "C#";
                // Update editor styling based on language
                ApplyLanguageStyling(language);
            }
        }

        private void ApplyLanguageStyling(string language)
        {
            if (MainEditor == null) return;

            switch (language.ToLower())
            {
                case "python":
                    MainEditor.Background = Brushes.LightGray;
                    break;
                case "javascript":
                    MainEditor.Background = Brushes.LightBlue;
                    break;
                case "html":
                    MainEditor.Background = Brushes.LightYellow;
                    break;
                case "css":
                    MainEditor.Background = Brushes.LightGreen;
                    break;
                default: // C#
                    MainEditor.Background = Brushes.White;
                    break;
            }
        }

        private void OnWordWrapToggle(object? sender, RoutedEventArgs e)
        {
            if (MainEditor != null)
            {
                MainEditor.WordWrap = WordWrapToggle.IsChecked ?? false;
            }
        }

        private void OnLineNumbersToggle(object? sender, RoutedEventArgs e)
        {
            if (MainEditor != null)
            {
                MainEditor.ShowLineNumbers = LineNumbersToggle.IsChecked ?? true;
            }
        }
    }
}
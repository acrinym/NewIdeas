using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Cycloside.Services;
using Cycloside.Plugins.BuiltIn.Controls;

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
            InitializeEditor();
            SetupEventHandlers();
        }

        private void InitializeEditor()
        {
            // MainEditor is automatically generated from XAML
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
            if (MainEditor != null)
            {
                MainEditor.Text = string.Empty;
                Title = "💻 Advanced Code Editor - Untitled";
            }
        }

        private async void OnOpenFile(object? sender, RoutedEventArgs e)
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open File",
                    AllowMultiple = false
                });

                if (files.Count > 0 && MainEditor != null)
                {
                    var file = files[0];
                    var content = await File.ReadAllTextAsync(file.Path.LocalPath);
                    
                    MainEditor.Text = content;
                    
                    // Detect language based on file extension
                    var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                    MainEditor.Language = extension switch
                    {
                        ".cs" => "CSharp",
                        ".js" => "JavaScript",
                        ".ts" => "TypeScript",
                        ".py" => "Python",
                        ".java" => "Java",
                        ".cpp" or ".cc" or ".cxx" => "Cpp",
                        ".c" => "C",
                        ".html" or ".htm" => "Html",
                        ".css" => "Css",
                        ".xml" => "Xml",
                        ".json" => "Json",
                        _ => "PlainText"
                    };
                    
                    Title = $"💻 Advanced Code Editor - {file.Name}";
                }
            }
            catch (Exception ex)
            {
                Title = "💻 Advanced Code Editor - Error opening file";
                NotificationCenter.Notify($"Error: Failed to open file: {ex.Message}");
            }
        }

        private async void OnSaveFile(object? sender, RoutedEventArgs e)
        {
            try
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save File",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("C# Files") { Patterns = new[] { "*.cs" } },
                        new FilePickerFileType("JavaScript Files") { Patterns = new[] { "*.js" } },
                        new FilePickerFileType("TypeScript Files") { Patterns = new[] { "*.ts" } },
                        new FilePickerFileType("Python Files") { Patterns = new[] { "*.py" } },
                        new FilePickerFileType("Java Files") { Patterns = new[] { "*.java" } },
                        new FilePickerFileType("C++ Files") { Patterns = new[] { "*.cpp" } },
                        new FilePickerFileType("C Files") { Patterns = new[] { "*.c" } },
                        new FilePickerFileType("HTML Files") { Patterns = new[] { "*.html" } },
                        new FilePickerFileType("CSS Files") { Patterns = new[] { "*.css" } },
                        new FilePickerFileType("XML Files") { Patterns = new[] { "*.xml" } },
                        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                        new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (file != null && MainEditor != null)
                {
                    await File.WriteAllTextAsync(file.Path.LocalPath, MainEditor.Text);
                    Title = $"💻 Advanced Code Editor - {file.Name}";
                    NotificationCenter.Notify("File saved successfully");
                }
            }
            catch (Exception ex)
            {
                Title = "💻 Advanced Code Editor - Error saving file";
                NotificationCenter.Notify($"Error: Failed to save file: {ex.Message}");
            }
        }

        private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && MainEditor != null)
            {
                var language = item.Content?.ToString();
                if (!string.IsNullOrEmpty(language))
                {
                    MainEditor.Language = language;
                }
            }
        }



        private void OnWordWrapToggle(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && MainEditor != null)
            {
                var isEnabled = toggleButton.IsChecked == true;
                // Note: Word wrap functionality would need to be implemented in CodeEditor
                // For now, this is a placeholder
            }
        }

        private void OnLineNumbersToggle(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && MainEditor != null)
            {
                var isEnabled = toggleButton.IsChecked == true;
                MainEditor.ShowLineNumbers = isEnabled;
            }
        }
    }
}
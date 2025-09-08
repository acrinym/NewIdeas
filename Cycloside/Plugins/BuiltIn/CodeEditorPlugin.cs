using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronPython.Hosting;
using Jint;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class CodeEditorPlugin : ObservableObject, IPlugin
    {
        private CodeEditorWindow? _window;
        private TextEditor? _editor;
        private ComboBox? _languageBox;
        private TextBox? _outputBox;
        private string? _currentFile;

        public string Name => "Code Editor";
        public string Description => "Edit and run C#, Python, or JS";
        public Version Version => new(0, 1, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new CodeEditorWindow { DataContext = this };
            _editor = _window.FindControl<TextEditor>("Editor");
            _languageBox = _window.FindControl<ComboBox>("LanguageBox");
            _outputBox = _window.FindControl<TextBox>("OutputBox");
            _languageBox!.SelectedIndex = 0; // default C#
            _languageBox.SelectionChanged += (_, _) => UpdateHighlighting();
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(CodeEditorPlugin));
            _window.Show();
            UpdateHighlighting();
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
            _editor = null;
            _languageBox = null;
            _outputBox = null;
        }

        [RelayCommand]
        private async Task OpenFile()
        {
            if (_window == null) return;
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Code File",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.All },
                SuggestedStartLocation = start
            });
            if (result.FirstOrDefault()?.TryGetLocalPath() is { } path && File.Exists(path))
            {
                _currentFile = path;
                try
                {
                    var content = await File.ReadAllTextAsync(path);
                    if (_editor != null)
                    {
                        _editor.Text = content;
                        // Force a refresh of the editor to ensure content is displayed
                        _editor.InvalidateVisual();
                    }
                    DetectLanguageFromExtension(path);
                    Logger.Log($"Code Editor: Successfully loaded file '{path}' ({content.Length} characters)");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Code Editor: Failed to load file '{path}': {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFile))
            {
                await SaveFileAs();
                return;
            }
            try
            {
                await File.WriteAllTextAsync(_currentFile, _editor?.Text ?? string.Empty);
                Logger.Log($"Code Editor: Successfully saved file '{_currentFile}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"Code Editor: Failed to save file '{_currentFile}': {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveFileAs()
        {
            if (_window == null) return;
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Code File As...",
                SuggestedFileName = Path.GetFileName(_currentFile) ?? "script",
                SuggestedStartLocation = start
            });
            if (result?.TryGetLocalPath() is { } path)
            {
                _currentFile = path;
                try
                {
                    await File.WriteAllTextAsync(path, _editor?.Text ?? string.Empty);
                    DetectLanguageFromExtension(path);
                    Logger.Log($"Code Editor: Successfully saved file as '{path}'");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Code Editor: Failed to save file as '{path}': {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RunCode()
        {
            if (_editor == null || _outputBox == null) return;
            var code = _editor.Text ?? string.Empty;
            var lang = GetSelectedLanguage();

            if (string.IsNullOrWhiteSpace(code))
            {
                _outputBox.Text = "No code to run. Please enter some code first.";
                return;
            }

            try
            {
                _outputBox.Text = "Running code...\r\n";
                switch (lang)
                {
                    case "C#":
                        var csWriter = new StringWriter();
                        var originalOut = Console.Out;
                        Console.SetOut(csWriter);
                        object? csResult = null;
                        try
                        {
                            // FIXED: Use proper script options and handle errors better
                            var options = ScriptOptions.Default
                                .WithImports("System", "System.Collections.Generic", "System.Linq")
                                .WithEmitDebugInformation(true);
                            csResult = await CSharpScript.EvaluateAsync<object?>(code, options);
                        }
                        catch (CompilationErrorException ex)
                        {
                            _outputBox.Text = $"Compilation Error:\r\n{string.Join("\r\n", ex.Diagnostics)}";
                            return;
                        }
                        finally
                        {
                            Console.SetOut(originalOut);
                        }
                        _outputBox.Text = csWriter.ToString() + (csResult != null ? csResult.ToString() : string.Empty);
                        break;
                    case "Python":
                        {
                            var engine = Python.CreateEngine();
                            using var stream = new MemoryStream();
                            engine.Runtime.IO.SetOutput(stream, Encoding.UTF8);
                            engine.Runtime.IO.SetErrorOutput(stream, Encoding.UTF8);
                            var scope = engine.CreateScope();
                            var source = engine.CreateScriptSourceFromString(code);
                            var pyResult = source.Execute(scope);
                            stream.Position = 0;
                            var output = new StreamReader(stream).ReadToEnd();
                            _outputBox.Text = output + (pyResult != null ? pyResult.ToString() : string.Empty);
                            break;
                        }
                    case "JavaScript":
                        var sb = new StringBuilder();
                        var jsEngine = new Engine();
                        jsEngine.SetValue("console", new
                        {
                            log = new Action<object?>(o => sb.AppendLine(o?.ToString()))
                        });
                        var jsValue = jsEngine.Execute(code).GetCompletionValue();
                        sb.Append(jsValue.ToObject()?.ToString());
                        _outputBox.Text = sb.ToString();
                        break;
                }
            }
            catch (Exception ex)
            {
                _outputBox.Text = $"Error: {ex.Message}\r\n\r\nStack Trace:\r\n{ex.StackTrace}";
            }
        }

        [RelayCommand]
        private void Close() => _window?.Close();

        [RelayCommand]
        private void Undo() => _editor?.Undo();

        [RelayCommand]
        private void Redo() => _editor?.Redo();

        [RelayCommand]
        private void Cut() => _editor?.Cut();

        [RelayCommand]
        private void Copy() => _editor?.Copy();

        [RelayCommand]
        private void Paste() => _editor?.Paste();

        private string GetSelectedLanguage() => _languageBox?.SelectedItem switch
        {
            ComboBoxItem item => item.Content?.ToString() ?? "C#",
            string s => s,
            _ => "C#"
        };

        private void DetectLanguageFromExtension(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".cs") _languageBox!.SelectedIndex = 0;
            else if (ext == ".py") _languageBox!.SelectedIndex = 1;
            else if (ext == ".js") _languageBox!.SelectedIndex = 2;
            UpdateHighlighting();
        }

        private void UpdateHighlighting()
        {
            if (_editor == null) return;
            var lang = GetSelectedLanguage();
            _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(lang switch
            {
                "C#" => "C#",
                "Python" => "Python",
                "JavaScript" => "JavaScript",
                _ => "C#"
            });
        }
    }
}
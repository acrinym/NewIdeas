using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using Cycloside.Services;
using Pty.Net;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class TerminalPlugin : IPlugin, IDisposable
    {
        private TerminalWindow? _window;
        private TextEditor? _textEditor;
        private IPtyConnection? _ptyConnection;
        private CancellationTokenSource? _ptyCts;
        private StreamWriter? _ptyWriter;

        public string Name => "Terminal";
        public string Description => "Run shell commands in a pseudo-terminal.";
        public Version Version => new(0, 2, 0); // Version bump for Pty.Net integration
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => true; // Use a fixed theme for the terminal

        public void Start()
        {
            _window = new TerminalWindow();
            _textEditor = _window.FindControl<TextEditor>("TextEditor");
            if (_textEditor != null)
            {
                _textEditor.TextArea.KeyDown += OnInputKeyDown;
            }

            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TerminalPlugin));
            _window.Show();

            StartPtyProcess();
        }

        public void Stop()
        {
            _ptyCts?.Cancel();
            _ptyWriter?.Dispose();
            _ptyConnection?.Dispose();
            _window?.Close();
        }

        public void Dispose() => Stop();

        private async void StartPtyProcess()
        {
            _ptyCts = new CancellationTokenSource();
            var options = new PtyOptions
            {
                Name = "Cycloside Terminal",
                Cols = 80,
                Rows = 24,
                Cwd = AppContext.BaseDirectory,
                App = GetShell(),
                CommandLine = GetShellArguments(),
            };

            try
            {
                _ptyConnection = await PtyProvider.SpawnAsync(options, _ptyCts.Token);
                _ptyWriter = new StreamWriter(_ptyConnection.WriterStream, Encoding.UTF8) { AutoFlush = true };

                _ = Task.Run(async () =>
                {
                    using var reader = new StreamReader(_ptyConnection.ReaderStream);
                    char[] buffer = new char[4096];
                    int count;
                    while (!_ptyCts.IsCancellationRequested && (count = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        var text = new string(buffer, 0, count);
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => AppendOutput(text));
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to start PTY process: {ex.Message}");
                AppendOutput($"\nError: Could not start terminal.\n{ex.Message}\n");
            }
        }

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            // This is a simplified input handling.
            if (_ptyWriter == null) return;

            if (e.Key == Key.Enter)
            {
                _ptyWriter.WriteLine();
            }
            else if (!string.IsNullOrEmpty(e.KeySymbol))
            {
                _ptyWriter.Write(e.KeySymbol);
            }
            // Add more advanced key handling here if needed
            e.Handled = true;
        }

        private void AppendOutput(string text)
        {
            if (_textEditor == null) return;
            _textEditor.AppendText(text);
            _textEditor.ScrollToEnd();
        }

        private static string GetShell()
        {
            var shell = SettingsManager.Settings.TerminalShell;
            if (!string.IsNullOrWhiteSpace(shell))
            {
                return shell;
            }

            if (OperatingSystem.IsWindows())
            {
                return "powershell.exe";
            }
            return "/bin/bash";
        }

        private static string[] GetShellArguments()
        {
            // No arguments needed when spawning a shell in a PTY
            return Array.Empty<string>();
        }
    }
}

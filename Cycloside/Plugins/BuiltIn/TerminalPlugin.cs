using Avalonia.Controls;
using Avalonia.Input;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Controls.Documents;

namespace Cycloside.Plugins.BuiltIn;

public class TerminalPlugin : IPlugin
{
    private TerminalWindow? _window;
    private ScrollViewer? _scrollViewer;
    private StackPanel? _outputPanel;
    private TextBox? _inputBox;
    private const int MaxLines = 200;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public string Name => "Terminal";
    public string Description => "Run shell commands";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new TerminalWindow();
        _scrollViewer = _window.FindControl<ScrollViewer>("OutputScroll");
        _outputPanel = _window.FindControl<StackPanel>("OutputPanel");
        _inputBox = _window.FindControl<TextBox>("InputBox");
        var runButton = _window.FindControl<Button>("RunButton");
        runButton?.AddHandler(Button.ClickEvent, (_, __) => ExecuteCommand());
        if (_inputBox != null)
        {
            _inputBox.KeyDown += OnInputKeyDown;
        }
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TerminalPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ExecuteCommand();
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            if (_history.Count == 0) return;
            _historyIndex = Math.Clamp(_historyIndex - 1, 0, _history.Count - 1);
            _inputBox!.Text = _history[_historyIndex];
            _inputBox.CaretIndex = _inputBox.Text.Length;
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (_history.Count == 0) return;
            _historyIndex = Math.Clamp(_historyIndex + 1, 0, _history.Count - 1);
            _inputBox!.Text = _history[_historyIndex];
            _inputBox.CaretIndex = _inputBox.Text.Length;
            e.Handled = true;
        }
    }

    private void ExecuteCommand()
    {
        if (_inputBox == null || _outputPanel == null) return;
        var cmd = _inputBox.Text;
        if (string.IsNullOrWhiteSpace(cmd)) return;

        _history.Add(cmd);
        _historyIndex = _history.Count;

        AppendOutput($"> {cmd}\n");
        _inputBox.Text = string.Empty;

        Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = GetShell(),
                    Arguments = GetShellArguments(cmd),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var stdout = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (!string.IsNullOrEmpty(stdout))
                        Dispatcher.UIThread.InvokeAsync(() => AppendOutput(stdout));
                    if (!string.IsNullOrEmpty(stderr))
                        Dispatcher.UIThread.InvokeAsync(() => AppendOutput(stderr));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(() => AppendOutput($"Error: {ex.Message}\n"));
                Logger.Log($"Terminal plugin error: {ex.Message}");
            }
        });
    }

    private void AppendOutput(string text)
    {
        if (_outputPanel == null || _scrollViewer == null) return;

        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            var block = new TextBlock { FontFamily = new FontFamily("monospace") };
            foreach (var (segment, color) in ParseAnsi(line))
            {
                var run = new Run(segment);
                if (color.HasValue)
                    run.Foreground = new SolidColorBrush(color.Value);
                // Safeguard in case Inlines collection is unexpectedly null
                block.Inlines?.Add(run);
            }
            _outputPanel.Children.Add(block);
        }

        while (_outputPanel.Children.Count > MaxLines)
            _outputPanel.Children.RemoveAt(0);

        _scrollViewer.ScrollToEnd();
    }

    private static readonly Regex AnsiRegex = new(@"\x1B\[(?<code>[0-9;]+)m");

    private static IEnumerable<(string Text, Color? Color)> ParseAnsi(string line)
    {
        var results = new List<(string Text, Color? Color)>();
        Color? current = null;
        int lastIndex = 0;

        foreach (Match m in AnsiRegex.Matches(line))
        {
            if (m.Index > lastIndex)
            {
                results.Add((line[lastIndex..m.Index], current));
            }

            foreach (var code in m.Groups["code"].Value.Split(';'))
            {
                if (code == "0")
                {
                    current = null;
                }
                else if (AnsiColorMap.TryGetValue(code, out var col))
                {
                    current = col;
                }
            }

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < line.Length)
        {
            results.Add((line[lastIndex..], current));
        }

        return results;
    }

    private static readonly Dictionary<string, Color> AnsiColorMap = new()
    {
        ["30"] = Colors.Black,
        ["31"] = Colors.Red,
        ["32"] = Colors.Green,
        ["33"] = Colors.Yellow,
        ["34"] = Colors.Blue,
        ["35"] = Colors.Magenta,
        ["36"] = Colors.Cyan,
        ["37"] = Colors.White,
        ["90"] = Colors.Gray,
        ["91"] = Colors.LightCoral,
        ["92"] = Colors.LightGreen,
        ["93"] = Colors.LightYellow,
        ["94"] = Colors.LightBlue,
        ["95"] = Colors.Plum,
        ["96"] = Colors.LightCyan,
        ["97"] = Colors.White
    };

    private static string GetShell()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
    }

    private static string GetShellArguments(string command)
    {
        return OperatingSystem.IsWindows() ? $"/C {command}" : $"-c \"{command}\"";
    }
}

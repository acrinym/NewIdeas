using Avalonia.Controls;
using Avalonia.Input;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cycloside.Plugins.BuiltIn;

public class TerminalPlugin : IPlugin
{
    private TerminalWindow? _window;
    private TextBox? _outputBox;
    private TextBox? _inputBox;
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
        _outputBox = _window.FindControl<TextBox>("OutputBox");
        _inputBox = _window.FindControl<TextBox>("InputBox");
        if (_outputBox != null)
        {
            ScrollViewer.SetVerticalScrollBarVisibility(_outputBox, ScrollBarVisibility.Auto);
        }
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
        if (_inputBox == null || _outputBox == null) return;
        var cmd = _inputBox.Text;
        if (string.IsNullOrWhiteSpace(cmd)) return;

        _history.Add(cmd);
        _historyIndex = _history.Count;

        AppendOutput($"> {cmd}\n");
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
                if (!string.IsNullOrEmpty(stdout)) AppendOutput(stdout);
                if (!string.IsNullOrEmpty(stderr)) AppendOutput(stderr);
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"Error: {ex.Message}\n");
        }
        finally
        {
            _inputBox.Text = string.Empty;
        }
    }

    private void AppendOutput(string text)
    {
        if (_outputBox == null) return;
        _outputBox.Text += text;
        _outputBox.CaretIndex = _outputBox.Text.Length;
    }

    private static string GetShell()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
    }

    private static string GetShellArguments(string command)
    {
        return OperatingSystem.IsWindows() ? $"/C {command}" : $"-c \"{command}\"";
    }
}

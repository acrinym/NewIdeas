using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

/// <summary>
/// Provides several small network utilities in one plugin. Results can
/// be saved to disk for later reference.
/// </summary>
public class NetworkToolsPlugin : IPlugin
{
    private NetworkToolsWindow? _window;
    private TextBox? _outputBox;
    private TextBox? _inputBox;
    private ComboBox? _ipconfigArgsBox;

    public string Name => "Network Tools";
    public string Description => "Ping, traceroute and more";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new NetworkToolsWindow();
        _outputBox = _window.FindControl<TextBox>("OutputBox");
        _inputBox = _window.FindControl<TextBox>("InputBox");
        _ipconfigArgsBox = _window.FindControl<ComboBox>("IpconfigArgsBox");

        _window.FindControl<Button>("PingButton")?.AddHandler(Button.ClickEvent, PingClicked);
        _window.FindControl<Button>("TraceButton")?.AddHandler(Button.ClickEvent, TraceClicked);
        _window.FindControl<Button>("IpconfigButton")?.AddHandler(Button.ClickEvent, IpconfigClicked);
        _window.FindControl<Button>("PortScanButton")?.AddHandler(Button.ClickEvent, PortScanClicked);
        _window.FindControl<Button>("WhoisButton")?.AddHandler(Button.ClickEvent, WhoisClicked);
        _window.FindControl<Button>("IpViewButton")?.AddHandler(Button.ClickEvent, IpViewClicked);
        _window.FindControl<Button>("MacButton")?.AddHandler(Button.ClickEvent, MacClicked);
        _window.FindControl<Button>("ExportButton")?.AddHandler(Button.ClickEvent, ExportClicked);

        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(NetworkToolsPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }

    private void AppendOutput(string text)
    {
        if (_outputBox == null) return;
        _outputBox.Text += text + "\n";
    }

    private async void PingClicked(object? sender, RoutedEventArgs e)
    {
        if (_inputBox == null) return;
        var host = _inputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        AppendOutput($"Pinging {host}...");
        try
        {
            var ping = new Ping();
            var reply = await ping.SendPingAsync(host);
            AppendOutput(reply.Status == IPStatus.Success
                ? $"Reply from {reply.Address}: time={reply.RoundtripTime}ms"
                : $"Ping failed: {reply.Status}");
        }
        catch (Exception ex)
        {
            AppendOutput($"Ping error: {ex.Message}");
        }
    }

    private async void TraceClicked(object? sender, RoutedEventArgs e)
    {
        if (_inputBox == null) return;
        var host = _inputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        AppendOutput($"Tracing route to {host}...");
        try
        {
            var sb = new StringBuilder();
            using var ping = new Ping();
            for (int ttl = 1; ttl <= 30; ttl++)
            {
                var options = new PingOptions(ttl, true);
                var reply = await ping.SendPingAsync(host, 4000, new byte[32], options);
                sb.AppendLine($"{ttl}\t{reply.Address} {reply.Status}");
                if (reply.Status == IPStatus.Success) break;
            }
            AppendOutput(sb.ToString());
        }
        catch (Exception ex)
        {
            AppendOutput($"Traceroute error: {ex.Message}");
        }
    }

    private async void IpconfigClicked(object? sender, RoutedEventArgs e)
    {
        var arg = _ipconfigArgsBox?.SelectedItem as string ?? "/all";
        AppendOutput($"Running ipconfig {arg}...");
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "ipconfig" : "ifconfig",
                Arguments = arg,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                AppendOutput(output);
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"ipconfig error: {ex.Message}");
        }
    }

    private async void PortScanClicked(object? sender, RoutedEventArgs e)
    {
        if (_inputBox == null) return;
        var host = _inputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        AppendOutput($"Scanning {host} ports 1-1024...");
        var openPorts = new List<int>();
        for (int port = 1; port <= 1024; port++)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(200)) == connectTask && client.Connected)
                {
                    openPorts.Add(port);
                }
            }
            catch { }
        }
        AppendOutput(openPorts.Count > 0
            ? $"Open ports: {string.Join(", ", openPorts)}"
            : "No open ports detected.");
    }

    private async void WhoisClicked(object? sender, RoutedEventArgs e)
    {
        if (_inputBox == null) return;
        var domain = _inputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(domain)) return;
        AppendOutput($"Looking up {domain}...");
        try
        {
            using var tcp = new TcpClient("whois.verisign-grs.com", 43);
            using var stream = tcp.GetStream();
            var query = Encoding.ASCII.GetBytes(domain + "\r\n");
            await stream.WriteAsync(query, 0, query.Length);
            using var reader = new StreamReader(stream, Encoding.ASCII);
            var result = await reader.ReadToEndAsync();
            AppendOutput(result);
        }
        catch (Exception ex)
        {
            AppendOutput($"Whois error: {ex.Message}");
        }
    }

    private async void IpViewClicked(object? sender, RoutedEventArgs e)
    {
        AppendOutput("Getting IP addresses...");
        try
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString());
            AppendOutput("Internal IPs: " + string.Join(", ", addresses));
            try
            {
                using var client = new HttpClient();
                var external = await client.GetStringAsync("https://api.ipify.org");
                AppendOutput("External IP: " + external);
            }
            catch (Exception ex)
            {
                AppendOutput($"External IP error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"IP view error: {ex.Message}");
        }
    }

    private void MacClicked(object? sender, RoutedEventArgs e)
    {
        AppendOutput("MAC Addresses:");
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            var mac = ni.GetPhysicalAddress().ToString();
            if (!string.IsNullOrWhiteSpace(mac))
                AppendOutput($"{ni.Name}: {mac}");
        }
    }

    private async void ExportClicked(object? sender, RoutedEventArgs e)
    {
        if (_window == null || _outputBox == null) return;
        var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Results",
            SuggestedFileName = $"network_results_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new[] { new FilePickerFileType("Text") { Patterns = new[] { "*.txt" } } },
            SuggestedStartLocation = start
        });
        if (file?.Path.LocalPath != null)
        {
            try
            {
                await File.WriteAllTextAsync(file.Path.LocalPath, _outputBox.Text);
            }
            catch (Exception ex)
            {
                AppendOutput($"Save error: {ex.Message}");
            }
        }
    }
}

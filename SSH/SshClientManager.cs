using Cycloside.Core;
using Renci.SshNet;

namespace Cycloside.SSH;

public sealed class SshProfile
{
    public string Name { get; set; } = "raspi";
    public string Host { get; set; } = "raspberrypi.local";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "pi";
    public string Password { get; set; } = "raspberry";
    public string? KeyFile { get; set; }
}

public sealed class SshClientManager : IDisposable
{
    private readonly EventBus _bus;
    private SshClient? _client;
    public SshProfile Profile { get; }

    public SshClientManager(EventBus bus, SshProfile profile)
    {
        _bus = bus;
        Profile = profile;
    }

    public void Connect()
    {
        if (_client?.IsConnected == true) return;
        _client = Profile.KeyFile is { Length: > 0 } && File.Exists(Profile.KeyFile)
            ? new SshClient(Profile.Host, Profile.Port, Profile.Username, new PrivateKeyFile(Profile.KeyFile))
            : new SshClient(Profile.Host, Profile.Port, Profile.Username, Profile.Password);
        _client.Connect();
        _bus.Publish("ssh/status", new { profile = Profile.Name, connected = true });
    }

    public string RunCommand(string command, int timeoutMs = 30000)
    {
        if (_client?.IsConnected != true) throw new InvalidOperationException("SSH not connected.");
        var cmd = _client.CreateCommand(command);
        cmd.CommandTimeout = TimeSpan.FromMilliseconds(timeoutMs);
        var result = cmd.Execute();
        _bus.Publish("ssh/command", new { profile = Profile.Name, command, exit = cmd.ExitStatus });
        return result;
    }

    public CancellationTokenSource TailFile(string path)
    {
        if (_client?.IsConnected != true) throw new InvalidOperationException("SSH not connected.");
        var cts = new CancellationTokenSource();
        _ = Task.Run(() =>
        {
            using var cmd = _client.CreateCommand($"tail -n 200 -f {path}");
            var result = cmd.Execute();
            var lines = result.Split('\n');
            foreach (var line in lines)
            {
                if (!cts.IsCancellationRequested && !string.IsNullOrWhiteSpace(line))
                {
                    _bus.Publish("ssh/tail", new { profile = Profile.Name, path, text = line });
                }
            }
        }, cts.Token);
        return cts;
    }

    public void Dispose()
    {
        if (_client is { IsConnected: true })
        {
            try { _client.Disconnect(); } catch { }
        }
        _client?.Dispose();
    }
}

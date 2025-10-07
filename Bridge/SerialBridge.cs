using System.IO.Ports;
using System.Text;
using System.Threading;
using Cycloside.Core;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Cycloside.Bridge;

public sealed class SerialBridge : IDisposable
{
    private readonly EventBus _bus;
    private SerialPort? _port;
    private readonly string _topicPrefix;
    private readonly Timer _readTimer;

    public SerialBridge(EventBus bus, string portName, int baud = 115200, string topicPrefix = "serial")
    {
        _bus = bus;
        _topicPrefix = topicPrefix;
        _port = new SerialPort(portName, baud) { NewLine = "\n", Encoding = Encoding.UTF8 };
        _port.Open();

        // Use polling instead of DataReceived event for .NET Core compatibility
        _readTimer = new Timer(_ => ReadSerialData(), null, 100, 100);
    }

    private void ReadSerialData()
    {
        if (_port?.IsOpen != true) return;

        try
        {
            if (_port.BytesToRead > 0)
            {
                var line = _port.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) return;
                _bus.Publish($"{_topicPrefix}/in", new { text = line.Trim() });
            }
        }
        catch { /* ignore */ }
    }

    public void Send(string text)
    {
        if (_port is { IsOpen: true })
            _port.WriteLine(text);
    }

    public void Dispose()
    {
        if (_port is { IsOpen: true }) _port.Close();
        _port?.Dispose();
        _port = null;
        _readTimer?.Dispose();
    }
}

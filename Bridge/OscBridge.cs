using System.Net;
using System.Net.Sockets;
using Cycloside.Core;
using Rug.Osc;

namespace Cycloside.Bridge;

public sealed class OscBridge : IDisposable
{
    private readonly EventBus _bus;
    private readonly OscReceiver _receiver;
    private readonly UdpClient _sender;
    private readonly string _topicPrefix;

    public OscBridge(EventBus bus, int listenPort = 9000, string destinationHost = "127.0.0.1", int destinationPort = 9001, string topicPrefix = "osc")
    {
        _bus = bus;
        _receiver = new OscReceiver(listenPort);
        _sender = new UdpClient();
        _sender.Connect(destinationHost, destinationPort);
        _topicPrefix = topicPrefix;

        var thread = new Thread(ListenLoop) { IsBackground = true };
        thread.Start();

        _bus.Subscribe($"{_topicPrefix}/out/*", msg =>
        {
            try
            {
                var path = msg.Payload.TryGetProperty("path", out var p) ? p.GetString() : "/cyclo";
                var args = new List<object>();
                foreach (var prop in msg.Payload.EnumerateObject())
                    if (prop.Name != "path") args.Add(prop.Value.ToString());
                var packet = new OscMessage(path!, args.ToArray());
                var data = packet.ToByteArray();
                _sender.Send(data, data.Length);
            } catch { }
        });
    }

    private void ListenLoop()
    {
        _receiver.Connect();
        while (_receiver.State != OscSocketState.Closed)
        {
            try
            {
                var packet = _receiver.Receive();
                if (packet is OscMessage msg)
                {
                    _bus.Publish($"{_topicPrefix}/in", new { path = msg.Address, args = msg });
                }
            }
            catch { }
        }
    }

    public void Dispose()
    {
        try { _receiver.Close(); } catch { }
        _sender?.Dispose();
    }
}

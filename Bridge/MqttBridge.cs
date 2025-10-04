using System.Text;
using System.Text.Json;
using Cycloside.Core;
using MQTTnet;
using MQTTnet.Client;

namespace Cycloside.Bridge;

public sealed class MqttBridge : IAsyncDisposable
{
    private readonly EventBus _bus;
    private readonly string _topicPrefix;
    private readonly IMqttClient _client;
    private readonly string _broker;
    private readonly int _port;
    private readonly string? _username, _password;

    public MqttBridge(EventBus bus, string broker = "localhost", int port = 1883, string? username = null, string? password = null, string topicPrefix = "mqtt")
    {
        _bus = bus;
        _broker = broker;
        _port = port;
        _username = username;
        _password = password;
        _topicPrefix = topicPrefix;
        _client = new MqttFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += e =>
        {
            var payload = e.ApplicationMessage.PayloadSegment.Array is { } arr
                ? Encoding.UTF8.GetString(arr, e.ApplicationMessage.PayloadSegment.Offset, e.ApplicationMessage.PayloadSegment.Count)
                : string.Empty;
            _bus.Publish($"{_topicPrefix}/in", new { topic = e.ApplicationMessage.Topic, payload });
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync(string[] subscribeTopics)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_broker, _port)
            .WithCredentials(_username, _password)
            .WithClientId("Cycloside-" + Environment.MachineName)
            .Build();

        await _client.ConnectAsync(options);
        foreach (var t in subscribeTopics)
            await _client.SubscribeAsync(t);

        // Outbound from bus -> MQTT
        _bus.Subscribe($"{_topicPrefix}/out/*", msg =>
        {
            try
            {
                var topic = msg.Payload.TryGetProperty("topic", out var t) ? t.GetString() : null;
                var text = msg.Payload.TryGetProperty("payload", out var p) ? p.GetString() : msg.Payload.GetRawText();
                if (!string.IsNullOrEmpty(topic))
                {
                    var appMsg = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(text ?? "").Build();
                    _client.PublishAsync(appMsg);
                }
            } catch { }
        });
    }

    public async ValueTask DisposeAsync()
    {
        try { await _client.DisconnectAsync(); } catch { }
        _client?.Dispose();
    }
}

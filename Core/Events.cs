using System.Text.Json;

namespace Cycloside.Core;

public sealed record BusMessage(string Topic, JsonElement Payload, DateTime Timestamp)
{
    public static BusMessage From<T>(string topic, T payload) =>
        new(topic, JsonSerializer.SerializeToElement(payload), DateTime.UtcNow);
}

public sealed class BusSubscription : IDisposable
{
    private readonly Action _unsubscribe;
    internal BusSubscription(Action unsubscribe) { _unsubscribe = unsubscribe; }
    public void Dispose() => _unsubscribe();
}

using System.Collections.Concurrent;
using System.Text.Json;

namespace Cycloside.Core;

/// <summary>
/// Simple in-proc pub/sub by topic string with wildcard support ("topic/*").
/// </summary>
public sealed class EventBus
{
    private readonly ConcurrentDictionary<string, List<Action<BusMessage>>> _handlers = new();

    public BusSubscription Subscribe(string topicPattern, Action<BusMessage> handler)
    {
        var list = _handlers.GetOrAdd(topicPattern, _ => new List<Action<BusMessage>>());
        lock (list) list.Add(handler);
        return new BusSubscription(() =>
        {
            lock (list) list.Remove(handler);
        });
    }

    public void Publish<T>(string topic, T payload)
    {
        var msg = BusMessage.From(topic, payload);
        foreach (var kv in _handlers)
        {
            if (TopicMatches(kv.Key, topic))
            {
                List<Action<BusMessage>> snapshot;
                lock (kv.Value) snapshot = kv.Value.ToList();
                foreach (var h in snapshot)
                {
                    try { h(msg); } catch { /* swallow to keep bus alive */ }
                }
            }
        }
    }

    private static bool TopicMatches(string pattern, string topic)
    {
        if (pattern == topic) return true;
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2];
            return topic.StartsWith(prefix);
        }
        return false;
    }
}

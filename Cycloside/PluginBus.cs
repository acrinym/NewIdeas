using System;
using System.Collections.Generic;

namespace Cycloside;

/// <summary>
/// Lightweight publish/subscribe message bus used by plugins to communicate
/// without taking hard dependencies on each other. Topics are arbitrary
/// strings and payloads are passed as <see cref="object"/>.
/// </summary>
public static class PluginBus
{
    private static readonly Dictionary<string,List<Action<object?>>> _subs = new();

    /// <summary>
    /// Subscribe to a specific topic.
    /// </summary>
    /// <param name="topic">Topic name.</param>
    /// <param name="handler">Callback invoked with the published payload.</param>
    public static void Subscribe(string topic, Action<object?> handler)
    {
        lock(_subs)
        {
            if(!_subs.TryGetValue(topic, out var list))
            {
                list = new();
                _subs[topic] = list;
            }
            list.Add(handler);
        }
    }

    /// <summary>
    /// Remove a previously registered handler.
    /// </summary>
    public static void Unsubscribe(string topic, Action<object?> handler)
    {
        lock(_subs)
        {
            if(_subs.TryGetValue(topic, out var list))
            {
                list.RemoveAll(h => h == handler);
                if(list.Count == 0)
                    _subs.Remove(topic);
            }
        }
    }

    /// <summary>
    /// Publish an event on a topic. Any subscriber receives the payload.
    /// </summary>
    public static void Publish(string topic, object? payload = null)
    {
        List<Action<object?>>? list = null;
        lock(_subs)
            _subs.TryGetValue(topic, out list);
        if(list == null) return;
        foreach(var h in list.ToArray())
        {
            try { h(payload); } catch(Exception ex) { Logger.Log($"Bus handler error: {ex.Message}"); }
        }
    }
}

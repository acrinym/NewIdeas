using System;
using System.Collections.Generic;

namespace Cycloside;

public static class PluginBus
{
    private static readonly Dictionary<string,List<Action<object?>>> _subs = new();

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

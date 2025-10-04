using System.Diagnostics;
using System.Text.Json;
using Cycloside.Core;

namespace Cycloside.Rules;

public sealed class RuleEngine : IDisposable
{
    private readonly EventBus _bus;
    private readonly List<Rule> _rules;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly List<Timer> _timers = new();

    public RuleEngine(EventBus bus, IEnumerable<Rule> rules)
    {
        _bus = bus;
        _rules = rules.Where(r => r.Enabled).ToList();
        Wire();
    }

    private void Wire()
    {
        foreach (var rule in _rules)
        {
            switch (rule.Trigger)
            {
                case TriggerType.BusTopic:
                    _subscriptions.Add(_bus.Subscribe(rule.TriggerExpr, _ => Execute(rule)));
                    break;
                case TriggerType.FileChanged:
                    var dir = Path.GetDirectoryName(rule.TriggerExpr)!;
                    var file = Path.GetFileName(rule.TriggerExpr);
                    var fsw = new FileSystemWatcher(dir, file) { EnableRaisingEvents = true, IncludeSubdirectories = false };
                    fsw.Changed += (_, __) => Execute(rule);
                    fsw.Created += (_, __) => Execute(rule);
                    fsw.Renamed += (_, __) => Execute(rule);
                    _watchers.Add(fsw);
                    break;
                case TriggerType.ProcessStarted:
                    var timer = new Timer(_ => {
                        foreach (var p in Process.GetProcessesByName(rule.TriggerExpr))
                            Execute(rule);
                    }, null, 1000, 1000);
                    _timers.Add(timer);
                    break;
                case TriggerType.Timer:
                    var ms = ParseTimer(rule.TriggerExpr);
                    var t = new Timer(_ => Execute(rule), null, ms, ms);
                    _timers.Add(t);
                    break;
            }
        }
    }

    private static int ParseTimer(string expr)
    {
        // Simple: "5s" "2m" "1h"
        if (expr.EndsWith("ms")) return int.Parse(expr[..^2]);
        if (expr.EndsWith("s")) return int.Parse(expr[..^1]) * 1000;
        if (expr.EndsWith("m")) return int.Parse(expr[..^1]) * 60_000;
        if (expr.EndsWith("h")) return int.Parse(expr[..^1]) * 3_600_000;
        return 1000;
    }

    private void Execute(Rule rule)
    {
        try
        {
            switch (rule.Action)
            {
                case ActionType.PublishBus:
                    // ActionExpr example: {"topic":"demo/out","payload":{"ok":true}}
                    var doc = JsonDocument.Parse(rule.ActionExpr);
                    var root = doc.RootElement;
                    var topic = root.GetProperty("topic").GetString()!;
                    var payload = root.TryGetProperty("payload", out var p) ? p : root;
                    _bus.Publish(topic, JsonSerializer.Deserialize<object>(payload.GetRawText())!);
                    break;
                case ActionType.RunProcess:
                    var parts = SplitExeArgs(rule.ActionExpr);
                    var psi = new ProcessStartInfo(parts.exe, parts.args) { UseShellExecute = true };
                    Process.Start(psi);
                    break;
                case ActionType.ShowToast:
                    // For simplicity, publish to a toast bus that your UI can observe.
                    _bus.Publish("toast/show", new { text = rule.ActionExpr });
                    break;
            }
        }
        catch { }
    }

    private static (string exe, string args) SplitExeArgs(string s)
    {
        if (s.StartsWith("\""))
        {
            var idx = s.IndexOf('"', 1);
            var exe = s.Substring(1, idx - 1);
            var args = s.Substring(idx + 1).Trim();
            return (exe, args);
        }
        var sp = s.IndexOf(' ');
        return sp > 0 ? (s[..sp], s[(sp+1)..]) : (s, "");
    }

    public void Dispose()
    {
        foreach (var s in _subscriptions) s.Dispose();
        foreach (var w in _watchers) w.Dispose();
        foreach (var t in _timers) t.Dispose();
    }
}

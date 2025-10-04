using System.Text.Json.Serialization;

namespace Cycloside.Rules;

public enum TriggerType
{
    BusTopic, Timer, FileChanged, ProcessStarted
}

public enum ActionType
{
    PublishBus, RunProcess, ShowToast
}

public sealed class Rule
{
    public string Name { get; set; } = "New Rule";
    public TriggerType Trigger { get; set; }
    public string TriggerExpr { get; set; } = ""; // e.g., topic pattern, cron-like "*/5s", file path, process name
    public ActionType Action { get; set; }
    public string ActionExpr { get; set; } = ""; // e.g., topic/payload JSON, exe + args, message text
    public bool Enabled { get; set; } = true;
}

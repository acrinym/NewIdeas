using System;

namespace Cycloside.Services
{
    public class ProcessInfo
    {
        public int Pid { get; set; }
        public string Name { get; set; } = "";
        public long MemoryUsageMb { get; set; }
        public DateTime? StartTime { get; set; }

        public override string ToString()
        {
            var startTimeStr = StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
            return $"PID: {Pid}, Name: {Name}, Started: {startTimeStr}";
        }
    }
}

using System;
using System.IO;

namespace Cycloside;

public static class Logger
{
    private static readonly object _lock = new();
    private static string LogDir => Path.Combine(AppContext.BaseDirectory, "logs");
    private static string LogFile => Path.Combine(LogDir, "app.log");

    public static void Log(string message)
    {
        Directory.CreateDirectory(LogDir);
        lock (_lock)
        {
            if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 1_000_000)
            {
                var backup = LogFile + ".1";
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(LogFile, backup);
            }
            File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n");
        }
    }

    /// <summary>
    /// Gracefully shuts down the logger. No-op for current implementation.
    /// </summary>
    public static void Shutdown()
    {
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Cycloside;

/// <summary>
/// Thread-safe asynchronous logger.
/// </summary>
public static class Logger
{
    private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string LogFile = Path.Combine(LogDir, "app.log");
    private static readonly BlockingCollection<string> _queue = new();
    private static readonly Task _logTask;

    static Logger()
    {
        Directory.CreateDirectory(LogDir);
        _logTask = Task.Run(ProcessQueue);
    }

    public static void Log(string message)
    {
        if (!_queue.IsAddingCompleted)
        {
            _queue.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
    }

    private static void ProcessQueue()
    {
        foreach (var msg in _queue.GetConsumingEnumerable())
        {
            try
            {
                File.AppendAllText(LogFile, msg + Environment.NewLine);

                var info = new FileInfo(LogFile);
                if (info.Exists && info.Length > 1_048_576)
                {
                    var backup = Path.Combine(LogDir, "app.log.1");
                    File.Move(LogFile, backup, overwrite: true);
                }
            }
            catch
            {
                // ignore logging errors
            }
        }
    }

    public static void Shutdown()
    {
        _queue.CompleteAdding();
        try
        {
            _logTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (TaskCanceledException) { }
    }
}

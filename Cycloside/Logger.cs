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
        // This loop will run on a background thread, consuming messages from the queue
        // and writing them to the log file.
        foreach (var msg in _queue.GetConsumingEnumerable())
        {
            try
            {
                File.AppendAllText(LogFile, msg + Environment.NewLine);

                // Check file size for rotation
                var info = new FileInfo(LogFile);
                if (info.Exists && info.Length > 1_048_576) // 1 MB
                {
                    var backup = Path.Combine(LogDir, "app.log.1");
                    File.Move(LogFile, backup, overwrite: true);
                }
            }
            catch
            {
                // ignore logging errors to prevent the logger itself from crashing the app
            }
        }
    }

    /// <summary>
    /// Signals the logger to process any remaining messages and shut down gracefully.
    /// This should be called when the application is closing.
    /// </summary>
    public static void Shutdown()
    {
        // This stops any new messages from being added to the queue.
        _queue.CompleteAdding();
        try
        {
            // Wait for the background task to finish writing any remaining log messages,
            // but with a timeout to prevent the app from hanging on exit.
            _logTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (TaskCanceledException) { }
        catch (AggregateException) { } // Can be thrown if the task faults, ignore on shutdown.
    }
}

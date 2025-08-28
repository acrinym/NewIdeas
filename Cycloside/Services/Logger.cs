using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace Cycloside.Services
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cycloside",
            "cycloside.log"
        );

        private static readonly ConcurrentQueue<string> _queue = new();
        private static readonly Timer _flushTimer;
        private static int _shutdown;

        static Logger()
        {
            var logDir = Path.GetDirectoryName(LogFile);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir!);
            }

            // Periodically flush the queue to avoid blocking the UI thread.
            _flushTimer = new Timer(_ => Flush(), null, dueTime: 500, period: 500);
        }

        public static void Log(string message)
        {
            LogMessage("INFO", message);
        }

        public static void Info(string message)
        {
            LogMessage("INFO", message);
        }

        public static void Warning(string message)
        {
            LogMessage("WARN", message);
        }

        public static void Error(string message)
        {
            LogMessage("ERROR", message);
        }

        public static void Debug(string message)
        {
            LogMessage("DEBUG", message);
        }

        private static void LogMessage(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level}] {message}";

            // Enqueue and let the background flusher write to disk.
            _queue.Enqueue(logMessage);

            // Also write to debug output
            System.Diagnostics.Debug.WriteLine(logMessage);
        }

        private static void Flush()
        {
            if (_queue.IsEmpty) return;
            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using var writer = new StreamWriter(LogFile, append: true);
                while (_queue.TryDequeue(out var line))
                {
                    writer.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            if (Interlocked.Exchange(ref _shutdown, 1) == 1) return;
            try
            {
                _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch { }
            Flush();
        }
    }
}

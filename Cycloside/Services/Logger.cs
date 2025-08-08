using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Cycloside.Services
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cycloside",
            "cycloside.log"
        );

        static Logger()
        {
            var logDir = Path.GetDirectoryName(LogFile);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir!);
            }
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

            try
            {
                // Ensure we write to the file from the UI thread to avoid cross-thread issues
                if (Dispatcher.UIThread.CheckAccess())
                {
                    File.AppendAllText(LogFile, logMessage + Environment.NewLine);
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        File.AppendAllText(LogFile, logMessage + Environment.NewLine);
                    });
                }

                // Also write to debug output
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                // If we can't write to the log file, at least try to write to debug output
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
        }
    }
}
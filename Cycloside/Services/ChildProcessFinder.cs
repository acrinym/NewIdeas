using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    public static class ChildProcessFinder
    {
        /// <summary>
        /// Gets all direct child processes for the specified parent process ID
        /// </summary>
        /// <param name="parentPid">The parent process ID</param>
        /// <returns>List of child process IDs</returns>
        public static List<int> GetChildProcesses(int parentPid)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetChildProcessesWindows(parentPid);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetChildProcessesLinux(parentPid);
            }
            else
            {
                throw new PlatformNotSupportedException("This platform is not supported");
            }
        }

        /// <summary>
        /// Windows implementation using Process.GetProcesses() and ParentProcessId
        /// </summary>
        private static List<int> GetChildProcessesWindows(int parentPid)
        {
            var childProcesses = new List<int>();

            try
            {
                // Get all processes on the system
                var allProcesses = Process.GetProcesses();

                foreach (var process in allProcesses)
                {
                    try
                    {
                        // Get the parent process ID using WMI query
                        int processParentPid = GetParentProcessId(process.Id);

                        if (processParentPid == parentPid)
                        {
                            childProcesses.Add(process.Id);
                        }
                    }
                    catch
                    {
                        // Skip processes we can't access
                        continue;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting child processes on Windows: {ex.Message}");
            }

            return childProcesses;
        }

        /// <summary>
        /// Gets parent process ID on Windows using WMI
        /// </summary>
        private static int GetParentProcessId(int processId)
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}"))
                {
                    using (var results = searcher.Get())
                    {
                        var enumerator = results.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            var managementObject = (System.Management.ManagementObject)enumerator.Current;
                            var parentId = managementObject["ParentProcessId"];
                            return Convert.ToInt32(parentId);
                        }
                    }
                }
            }
            catch
            {
                // If WMI fails, return -1 to indicate unknown parent
            }

            return -1;
        }

        /// <summary>
        /// Linux implementation using /proc filesystem
        /// </summary>
        private static List<int> GetChildProcessesLinux(int parentPid)
        {
            var childProcesses = new List<int>();

            try
            {
                // Method 1: Try reading /proc/{parentPid}/task/{tid}/children (newer kernels)
                var childrenFromTaskDir = GetChildrenFromTaskDirectory(parentPid);
                if (childrenFromTaskDir.Any())
                {
                    return childrenFromTaskDir;
                }

                // Method 2: Fallback - scan all processes in /proc and check their parent
                return GetChildrenByScanningProc(parentPid);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting child processes on Linux: {ex.Message}");
                return childProcesses;
            }
        }

        /// <summary>
        /// Try to read children from /proc/{pid}/task/{tid}/children files
        /// </summary>
        private static List<int> GetChildrenFromTaskDirectory(int parentPid)
        {
            var childProcesses = new List<int>();

            try
            {
                var taskDir = $"/proc/{parentPid}/task";
                if (!Directory.Exists(taskDir))
                    return childProcesses;

                // Check each thread's children file
                var threadDirs = Directory.GetDirectories(taskDir);
                foreach (var threadDir in threadDirs)
                {
                    var childrenFile = Path.Combine(threadDir, "children");
                    if (File.Exists(childrenFile))
                    {
                        var content = File.ReadAllText(childrenFile).Trim();
                        if (!string.IsNullOrEmpty(content))
                        {
                            var childPids = content.Split(new[] { ' ', '\t', '\n' },
                                StringSplitOptions.RemoveEmptyEntries);

                            foreach (var pidStr in childPids)
                            {
                                if (int.TryParse(pidStr, out int childPid))
                                {
                                    childProcesses.Add(childPid);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // If this method fails, we'll fall back to scanning /proc
            }

            return childProcesses.Distinct().ToList();
        }

        /// <summary>
        /// Fallback method: scan all processes and check their parent PID
        /// </summary>
        private static List<int> GetChildrenByScanningProc(int parentPid)
        {
            var childProcesses = new List<int>();

            try
            {
                var procDirs = Directory.GetDirectories("/proc")
                    .Where(d => int.TryParse(Path.GetFileName(d), out _))
                    .ToList();

                foreach (var procDir in procDirs)
                {
                    try
                    {
                        var statFile = Path.Combine(procDir, "stat");
                        if (!File.Exists(statFile))
                            continue;

                        var statContent = File.ReadAllText(statFile);
                        var parentProcessId = GetParentPidFromStat(statContent);

                        if (parentProcessId == parentPid)
                        {
                            var pidStr = Path.GetFileName(procDir);
                            if (int.TryParse(pidStr, out int childPid))
                            {
                                childProcesses.Add(childPid);
                            }
                        }
                    }
                    catch
                    {
                        // Skip processes we can't read
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning /proc: {ex.Message}");
            }

            return childProcesses;
        }

        /// <summary>
        /// Parse parent PID from /proc/{pid}/stat file
        /// Format: pid (comm) state ppid ...
        /// </summary>
        private static int GetParentPidFromStat(string statContent)
        {
            try
            {
                // Handle process names with spaces by finding the last closing parenthesis
                var lastParen = statContent.LastIndexOf(')');
                if (lastParen == -1)
                    return -1;

                // Split the remaining part after the process name
                var remaining = statContent.Substring(lastParen + 1).Trim();
                var parts = remaining.Split(' ');

                // Parent PID is the second field after the process name
                // Format: state ppid ...
                if (parts.Length >= 2 && int.TryParse(parts[1], out int ppid))
                {
                    return ppid;
                }
            }
            catch
            {
                // If parsing fails, return -1
            }

            return -1;
        }

        /// <summary>
        /// Get process information including name and command line
        /// </summary>
        public static ProcessInfo GetProcessInfo(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return new ProcessInfo
                {
                    Pid = pid,
                    Name = process.ProcessName,
                    StartTime = process.StartTime
                };
            }
            catch
            {
                return new ProcessInfo { Pid = pid, Name = "Unknown" };
            }
        }
    }
}

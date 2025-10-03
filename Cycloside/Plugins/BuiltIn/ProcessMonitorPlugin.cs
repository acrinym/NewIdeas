using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// A simple data record to hold information about a single process.
    /// Using a record is cleaner than passing around formatted strings.
    /// </summary>
    public record ProcessInfo(string Name, long MemoryUsageMb, int ProcessId, bool IsSystemProcess);

    /// <summary>
    /// Acts as the ViewModel for the Process Monitor window.
    /// </summary>
    public partial class ProcessMonitorPlugin : ObservableObject, IPlugin, IDisposable
    {
        private ProcessMonitorWindow? _window;
        private DispatcherTimer? _timer;
        private readonly int _currentProcessId = Process.GetCurrentProcess().Id;

        // --- IPlugin Properties ---
        public string Name => "Process Monitor";
        public string Description => "List running processes with memory usage";
        public Version Version => new(0, 3, 0); // Incremented for process management features
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Observable Properties for UI Binding ---
        public ObservableCollection<ProcessInfo> Processes { get; } = new();
        
        [ObservableProperty]
        private ProcessInfo? _selectedProcess;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        // --- Commands ---
        [RelayCommand]
        private async Task CloseProcessAsync()
        {
            if (SelectedProcess == null) return;
            
            // Don't allow closing the current process
            if (SelectedProcess.ProcessId == _currentProcessId)
            {
                StatusMessage = "Cannot close the current application";
                return;
            }
            
            try
            {
                // Try to get the process by ID
                var process = Process.GetProcessById(SelectedProcess.ProcessId);
                
                // Show confirmation dialog
                var result = await DialogHelper.ShowYesNoDialogAsync(
                    _window!, 
                    $"Close Process", 
                    $"Are you sure you want to close {SelectedProcess.Name}?");
                
                if (result)
                {
                    // Try to close gracefully first
                    if (!process.HasExited)
                    {
                        if (process.CloseMainWindow())
                        {
                            // Wait a bit for the process to close gracefully
                            if (!process.WaitForExit(3000))
                            {
                                // If it doesn't close in time, ask if we should force it
                                var forceResult = await DialogHelper.ShowYesNoDialogAsync(
                                    _window!,
                                    "Force Close",
                                    $"{SelectedProcess.Name} is not responding. Force close?");
                                
                                if (forceResult)
                                {
                                    process.Kill();
                                    StatusMessage = $"Process {SelectedProcess.Name} was forcefully terminated";
                                }
                                else
                                {
                                    StatusMessage = "Process close operation canceled";
                                }
                            }
                            else
                            {
                                StatusMessage = $"Process {SelectedProcess.Name} closed successfully";
                            }
                        }
                        else
                        {
                            // If CloseMainWindow fails, ask if we should force it
                            var forceResult = await DialogHelper.ShowYesNoDialogAsync(
                                _window!,
                                "Force Close",
                                $"Cannot close {SelectedProcess.Name} gracefully. Force close?");
                            
                            if (forceResult)
                            {
                                process.Kill();
                                StatusMessage = $"Process {SelectedProcess.Name} was forcefully terminated";
                            }
                            else
                            {
                                StatusMessage = "Process close operation canceled";
                            }
                        }
                    }
                    else
                    {
                        StatusMessage = "Process has already exited";
                    }
                    
                    // Refresh the process list
                    UpdateProcessList(null, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = "Process close operation canceled";
                }
            }
            catch (ArgumentException)
            {
                StatusMessage = "Process no longer exists";
                UpdateProcessList(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Logger.Log($"Process Monitor: Error closing process: {ex}");
            }
        }

        [RelayCommand]
        private void RefreshProcesses()
        {
            UpdateProcessList(null, EventArgs.Empty);
            StatusMessage = "Process list refreshed";
        }

        // --- Plugin Lifecycle & Disposal ---

        public void Start()
        {
            _window = new ProcessMonitorWindow 
            { 
                DataContext = this,
                Plugin = this // Set the plugin reference for automatic theme/skin management
            };
            
            // Use the improved base class method for theme and skin application
            _window.ApplyPluginThemeAndSkin(this);
            
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ProcessMonitorPlugin));
            _window.Show();

            // Add initial process list update
            UpdateProcessList(null, EventArgs.Empty);

            _timer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, UpdateProcessList);
            _timer.Start();
            
            Logger.Log("Process Monitor plugin started with improved theme/skin management");
        }

        public void Stop()
        {
            // Stop the timer first to prevent updates during shutdown
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
            
            // Close the window which will trigger the Closed event and cleanup
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            
            Logger.Log("Process Monitor plugin stopped and resources cleaned up");
        }

        public void Dispose()
        {
            // Ensure proper cleanup
            Stop();
            Logger.Log("Process Monitor plugin disposed");
        }

        // --- Private Logic ---

        /// <summary>
        /// Fetches the process list on a background thread and updates the collection on the UI thread.
        /// </summary>
        private void UpdateProcessList(object? sender, EventArgs e)
        {
            // Getting all system processes can be slow, so we do it on a background
            // thread to ensure the UI remains perfectly smooth and responsive.
            Task.Run(() =>
            {
                var currentProcesses = Process.GetProcesses()
                    .Select(p =>
                    {
                        try
                        {
                            // FIXED: Better error handling and more process information
                            var memoryMB = p.WorkingSet64 / 1024 / 1024;
                            var processName = !string.IsNullOrEmpty(p.ProcessName) ? p.ProcessName : "Unknown";
                            var processId = p.Id;
                            
                            // Determine if it's a system process (simplified check)
                            bool isSystemProcess = false;
                            try
                            {
                                if (OperatingSystem.IsWindows())
                                {
                                    // Common Windows system process names
                                    isSystemProcess = processName.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("svchost", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("lsass", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("csrss", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("smss", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("winlogon", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("wininit", StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    // Common Unix/Linux system process names
                                    isSystemProcess = processName.Equals("init", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("systemd", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.Equals("kthreadd", StringComparison.OrdinalIgnoreCase) ||
                                                     processName.StartsWith("kworker", StringComparison.OrdinalIgnoreCase);
                                }
                            }
                            catch
                            {
                                // Ignore errors in system process detection
                            }

                            // Try to get the main window title for better identification
                            string? windowTitle = null;
                            try
                            {
                                if (!p.HasExited && !string.IsNullOrEmpty(p.MainWindowTitle))
                                {
                                    windowTitle = p.MainWindowTitle;
                                }
                            }
                            catch
                            {
                                // Ignore errors getting window title
                            }

                            var displayName = !string.IsNullOrEmpty(windowTitle) ? $"{processName} - {windowTitle}" : processName;

                            return new ProcessInfo(displayName, memoryMB, processId, isSystemProcess);
                        }
                        catch (Exception ex)
                        {
                            // Log the specific error for debugging
                            Logger.Log($"Process Monitor: Error reading process {p.ProcessName}: {ex.Message}");
                            return new ProcessInfo(p.ProcessName ?? "Unknown", 0, -1, false);
                        }
                    })
                    .Where(p => p.MemoryUsageMb > 0 && p.ProcessId > 0) // Only show valid processes with memory usage
                    .OrderByDescending(p => p.MemoryUsageMb) // Sort by memory usage
                    .Take(100) // Limit to top 100 processes
                    .ToList();

                // All UI updates MUST happen on the UI thread.
                // We dispatch the collection update back to the correct thread.
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Save the currently selected process ID if any
                    int? selectedId = SelectedProcess?.ProcessId;
                    
                    // This is a simple but effective way to update the list. For very high-frequency
                    // updates, a more complex diffing algorithm could be used, but this is great for a 2-second interval.
                    Processes.Clear();
                    foreach (var process in currentProcesses)
                    {
                        Processes.Add(process);
                    }
                    
                    // Try to restore the selection if possible
                    if (selectedId.HasValue)
                    {
                        SelectedProcess = Processes.FirstOrDefault(p => p.ProcessId == selectedId);
                    }

                    // Log the update for debugging
                    Logger.Log($"Process Monitor: Updated with {currentProcesses.Count} processes");
                });
            });
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// A simple data record to hold information about a single process.
    /// Using a record is cleaner than passing around formatted strings.
    /// </summary>
    public record ProcessInfo(string Name, long MemoryUsageMb);

    /// <summary>
    /// Acts as the ViewModel for the Process Monitor window.
    /// </summary>
    public partial class ProcessMonitorPlugin : ObservableObject, IPlugin, IDisposable
    {
        private ProcessMonitorWindow? _window;
        private DispatcherTimer? _timer;

        // --- IPlugin Properties ---
        public string Name => "Process Monitor";
        public string Description => "List running processes with memory usage";
        public Version Version => new(0, 2, 0); // Incremented for MVVM refactor
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Observable Properties for UI Binding ---
        public ObservableCollection<ProcessInfo> Processes { get; } = new();

        // --- Plugin Lifecycle & Disposal ---

        public void Start()
        {
            _window = new ProcessMonitorWindow { DataContext = this };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ProcessMonitorPlugin));
            _window.Show();

            // Add initial process list update
            UpdateProcessList(null, EventArgs.Empty);

            _timer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, UpdateProcessList);
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _window?.Close();
        }

        public void Dispose() => Stop();

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
                            // p.WorkingSet64 can throw if the process exits while we're querying it.
                            return new ProcessInfo(p.ProcessName, p.WorkingSet64 / 1024 / 1024);
                        }
                        catch
                        {
                            return new ProcessInfo(p.ProcessName, 0); // Gracefully handle access errors
                        }
                    })
                    .OrderBy(p => p.Name)
                    .ToList();

                // All UI updates MUST happen on the UI thread.
                // We dispatch the collection update back to the correct thread.
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // This is a simple but effective way to update the list. For very high-frequency
                    // updates, a more complex diffing algorithm could be used, but this is great for a 2-second interval.
                    Processes.Clear();
                    foreach (var process in currentProcesses)
                    {
                        Processes.Add(process);
                    }
                });
            });
        }
    }
}

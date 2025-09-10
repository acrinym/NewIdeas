using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class TaskManagerPlugin : ObservableObject, IPlugin, IDisposable
    {
        private Views.TaskManagerWindow? _window;
        private DispatcherTimer? _timer;

        public ObservableCollection<Services.ProcessInfo> Processes { get; } = new();

        public string Name => "Task Manager";
        public string Description => "Monitors and manages child processes of Cycloside.";
        public Version Version => new(0, 1, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new Views.TaskManagerWindow { DataContext = this };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TaskManagerPlugin));
            _window.Show();

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

        [RelayCommand]
        private void KillProcess(Services.ProcessInfo? processInfo)
        {
            if (processInfo == null) return;

            try
            {
                var process = Process.GetProcessById(processInfo.Pid);
                process.Kill();
                Logger.Log($"Task Manager: Killed process {processInfo.Name} (PID: {processInfo.Pid})");
            }
            catch (Exception ex)
            {
                Logger.Log($"Task Manager: Failed to kill process {processInfo.Name} (PID: {processInfo.Pid}): {ex.Message}");
            }
        }

        private void UpdateProcessList(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var currentProcess = Process.GetCurrentProcess();
                var childPids = ChildProcessFinder.GetChildProcesses(currentProcess.Id);

                var childProcesses = childPids
                    .Select(pid => ChildProcessFinder.GetProcessInfo(pid))
                    .OrderBy(p => p.Name)
                    .ToList();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Processes.Clear();
                    foreach (var process in childProcesses)
                    {
                        Processes.Add(process);
                    }
                    Logger.Log($"Task Manager: Updated with {childProcesses.Count} child processes.");
                });
            });
        }
    }
}

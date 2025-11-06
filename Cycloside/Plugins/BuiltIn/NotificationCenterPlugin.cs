using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

/// <summary>
/// Displays recent notifications from <see cref="NotificationCenter"/>.
/// </summary>
public class NotificationCenterPlugin : IPlugin, IDisposable, IWorkspaceItem
{
    private Views.NotificationCenterWindow? _window;

    public ObservableCollection<string> Messages { get; } = new();
    public string Name => "Notification Center";
    public string Description => "View recent notifications";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.Utilities;
    public bool UseWorkspace { get; set; }

    public void Start()
    {
        NotificationCenter.NotificationReceived += OnNotification;
        if (UseWorkspace) return;
        ShowWindow();
    }

    private void ShowWindow()
    {
        if (_window != null)
        {
            _window.Activate();
            return;
        }
        _window = new Views.NotificationCenterWindow { DataContext = this };
        ThemeManager.ApplyForPlugin(_window, this);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, Name);
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }

    public Control BuildWorkspaceView() => new Views.NotificationCenterView { DataContext = this };

    private void OnNotification(string msg)
    {
        Dispatcher.UIThread.Post(() => Messages.Add(msg));
    }

    public void Stop() => Dispose();

    public void Dispose()
    {
        NotificationCenter.NotificationReceived -= OnNotification;
        _window?.Close();
        _window = null;
    }
}

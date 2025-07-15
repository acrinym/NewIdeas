using System;

namespace Cycloside.Services;

/// <summary>
/// Provides a simple pub/sub mechanism for user notifications.
/// Any component can call <see cref="Notify"/> to broadcast a message
/// which listeners like the Notification Center plugin can display.
/// </summary>
public static class NotificationCenter
{
    /// <summary>
    /// Raised whenever a new notification message arrives.
    /// </summary>
    public static event Action<string>? NotificationReceived;

    /// <summary>
    /// Broadcasts a message to all subscribers and logs it.
    /// </summary>
    public static void Notify(string message)
    {
        NotificationReceived?.Invoke(message);
        Logger.Log(message);
    }
}

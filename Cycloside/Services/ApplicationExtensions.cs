using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Cycloside.Services
{
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Safely retrieves the application's main window when running with a classic desktop lifetime.
        /// </summary>
        /// <param name="app">The current application instance.</param>
        /// <returns>The main <see cref="TopLevel"/> window if available; otherwise <c>null</c>.</returns>
        public static TopLevel? GetMainTopLevel(this Application? app)
        {
            if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }
    }
}

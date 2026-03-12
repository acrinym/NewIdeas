using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Controls;
using Cycloside.Services;
using System;

namespace Cycloside.Helpers
{
    /// <summary>
    /// Helper class for integrating window decorations into existing windows.
    /// Provides utilities to convert standard windows to themed DecoratedWindow instances.
    /// </summary>
    public static class WindowDecorationHelper
    {
        /// <summary>
        /// Creates a new DecoratedWindow with the specified content and settings.
        /// This is the easiest way to create a themed window.
        /// </summary>
        /// <param name="title">Window title</param>
        /// <param name="content">Content to display inside the window</param>
        /// <param name="width">Window width (default: 800)</param>
        /// <param name="height">Window height (default: 600)</param>
        /// <param name="canResize">Whether window can be resized (default: true)</param>
        /// <returns>A new DecoratedWindow with theme applied</returns>
        public static DecoratedWindow CreateThemedWindow(
            string title,
            Control content,
            double width = 800,
            double height = 600,
            bool canResize = true)
        {
            var window = new DecoratedWindow
            {
                Title = title,
                Width = width,
                Height = height,
                CanResize = canResize,
                Content = content
            };

            return window;
        }

        /// <summary>
        /// Creates a themed window with a DataContext already set.
        /// Useful for MVVM patterns where the ViewModel drives the window.
        /// </summary>
        /// <param name="title">Window title</param>
        /// <param name="content">Content control (View)</param>
        /// <param name="dataContext">ViewModel or data context</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        /// <returns>DecoratedWindow with theme and DataContext</returns>
        public static DecoratedWindow CreateThemedWindowWithViewModel(
            string title,
            Control content,
            object dataContext,
            double width = 800,
            double height = 600)
        {
            var window = CreateThemedWindow(title, content, width, height);
            window.DataContext = dataContext;
            content.DataContext = dataContext;
            return window;
        }

        /// <summary>
        /// Wraps existing window content in a DecoratedWindow.
        /// This transfers the content from an existing window to a new themed window.
        /// </summary>
        /// <param name="sourceWindow">The window whose content should be wrapped</param>
        /// <returns>New DecoratedWindow with the content from sourceWindow</returns>
        public static DecoratedWindow WrapExistingWindow(Window sourceWindow)
        {
            // Extract properties from source window
            var title = sourceWindow.Title ?? "Window";
            var content = sourceWindow.Content as Control;
            var dataContext = sourceWindow.DataContext;
            var width = sourceWindow.Width;
            var height = sourceWindow.Height;
            var canResize = sourceWindow.CanResize;

            // Create new decorated window
            var decoratedWindow = new DecoratedWindow
            {
                Title = title,
                Width = double.IsNaN(width) ? 800 : width,
                Height = double.IsNaN(height) ? 600 : height,
                CanResize = canResize,
                Content = content,
                DataContext = dataContext
            };

            // Transfer window position if set
            if (sourceWindow.Position != default)
            {
                decoratedWindow.Position = sourceWindow.Position;
            }

            // Transfer window state
            decoratedWindow.WindowState = sourceWindow.WindowState;

            return decoratedWindow;
        }

        /// <summary>
        /// Replaces a window with a decorated version, maintaining position and state.
        /// This closes the source window and opens the decorated version.
        /// </summary>
        /// <param name="sourceWindow">Window to replace</param>
        /// <param name="onClosed">Optional callback when the new window closes</param>
        /// <returns>The new DecoratedWindow (already shown)</returns>
        public static DecoratedWindow ReplaceWithDecoratedWindow(
            Window sourceWindow,
            Action? onClosed = null)
        {
            var decoratedWindow = WrapExistingWindow(sourceWindow);

            if (onClosed != null)
            {
                decoratedWindow.Closed += (s, e) => onClosed();
            }

            // Show new window before closing old one for smooth transition
            decoratedWindow.Show();
            sourceWindow.Close();

            return decoratedWindow;
        }

        /// <summary>
        /// Checks if a window should be themed based on current configuration.
        /// Uses WindowDecorationManager's inclusion/exclusion rules.
        /// </summary>
        /// <param name="windowTitle">The title of the window to check</param>
        /// <returns>True if the window should be themed</returns>
        public static bool ShouldThemeWindow(string windowTitle)
        {
            var manager = WindowDecorationManager.Instance;
            return manager.ShouldApplyToWindow(windowTitle);
        }

        /// <summary>
        /// Creates a simple content panel with padding for use in decorated windows.
        /// This provides consistent spacing from the window edges.
        /// </summary>
        /// <param name="innerContent">The actual content to display</param>
        /// <param name="padding">Padding around content (default: 16)</param>
        /// <returns>Border control with padding applied</returns>
        public static Border CreateContentPanel(Control innerContent, double padding = 16)
        {
            return new Border
            {
                Padding = new Thickness(padding),
                Background = Brushes.White,
                Child = innerContent
            };
        }

        /// <summary>
        /// Creates a content panel with a custom background color.
        /// </summary>
        /// <param name="innerContent">The content to display</param>
        /// <param name="backgroundColor">Background color</param>
        /// <param name="padding">Padding around content</param>
        /// <returns>Border control with background and padding</returns>
        public static Border CreateContentPanelWithBackground(
            Control innerContent,
            Color backgroundColor,
            double padding = 16)
        {
            return new Border
            {
                Padding = new Thickness(padding),
                Background = new SolidColorBrush(backgroundColor),
                Child = innerContent
            };
        }

        /// <summary>
        /// Applies standard window settings commonly used in Cycloside.
        /// Includes window positioning, effects, and theme integration.
        /// </summary>
        /// <param name="window">Window to configure</param>
        /// <param name="pluginName">Plugin name for position configuration</param>
        public static void ApplyStandardWindowSettings(Window window, string pluginName)
        {
            // Apply window positioning from startup configuration
            WindowPositioningService.Instance.ApplyPosition(window, pluginName);

            // If it's a DecoratedWindow, theme is already applied
            // If it's a regular window and should be themed, log a suggestion
            if (window is not DecoratedWindow && ShouldThemeWindow(window.Title ?? ""))
            {
                Logger.Log($"💡 Tip: '{pluginName}' could use DecoratedWindow for theming");
            }
        }

        /// <summary>
        /// Factory method for creating a themed window for a plugin.
        /// Combines theming, positioning, and standard settings.
        /// </summary>
        /// <param name="pluginName">Name of the plugin creating the window</param>
        /// <param name="title">Window title</param>
        /// <param name="content">Window content</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        /// <returns>Fully configured DecoratedWindow</returns>
        public static DecoratedWindow CreatePluginWindow(
            string pluginName,
            string title,
            Control content,
            double width = 800,
            double height = 600)
        {
            var window = CreateThemedWindow(title, content, width, height);

            // Apply positioning after window is created
            WindowPositioningService.Instance.ApplyPosition(window, pluginName);

            Logger.Log($"🎨 Created themed window for {pluginName}: {title}");

            return window;
        }

        /// <summary>
        /// Creates a centered window at a specific size.
        /// Useful for dialogs and popups.
        /// </summary>
        /// <param name="title">Window title</param>
        /// <param name="content">Window content</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        /// <returns>DecoratedWindow centered on screen</returns>
        public static DecoratedWindow CreateCenteredDialog(
            string title,
            Control content,
            double width = 400,
            double height = 300)
        {
            var window = CreateThemedWindow(title, content, width, height, canResize: false);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return window;
        }

        /// <summary>
        /// Creates a dialog with standard buttons (OK/Cancel).
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message to display</param>
        /// <param name="onOk">Action to perform when OK is clicked</param>
        /// <param name="onCancel">Action to perform when Cancel is clicked (optional)</param>
        /// <returns>DecoratedWindow dialog</returns>
        public static DecoratedWindow CreateDialog(
            string title,
            string message,
            Action<Window> onOk,
            Action<Window>? onCancel = null)
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                Margin = new Thickness(20)
            };

            // Message text
            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360
            };
            panel.Children.Add(textBlock);

            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 32
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 32
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            panel.Children.Add(buttonPanel);

            var window = CreateCenteredDialog(title, panel, 400, 200);

            okButton.Click += (s, e) =>
            {
                onOk(window);
                window.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                onCancel?.Invoke(window);
                window.Close();
            };

            return window;
        }

        /// <summary>
        /// Shows an information message dialog.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message to display</param>
        public static void ShowInfoDialog(string title, string message)
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                Margin = new Thickness(20)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360
            };
            panel.Children.Add(textBlock);

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            panel.Children.Add(okButton);

            var window = CreateCenteredDialog(title, panel, 400, 180);

            okButton.Click += (s, e) => window.Close();

            window.Show();
        }
    }
}

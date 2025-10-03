using System;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    public static class DialogHelper
    {
        public static async Task<IStorageFolder?> GetDefaultStartLocationAsync(IStorageProvider provider)
        {
            string path = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return await provider.TryGetFolderFromPathAsync(path);
        }
        
        /// <summary>
        /// Shows a Yes/No dialog and returns true if the user clicked Yes.
        /// </summary>
        /// <param name="parent">The parent window</param>
        /// <param name="title">The dialog title</param>
        /// <param name="message">The dialog message</param>
        /// <returns>True if the user clicked Yes, false otherwise</returns>
        public static async Task<bool> ShowYesNoDialogAsync(Window parent, string title, string message)
        {
            var confirmWindow = new ConfirmationDialog(title, message);
            return await confirmWindow.ShowDialog<bool>(parent);
        }
        
        /// <summary>
        /// Shows an information dialog with an OK button.
        /// </summary>
        /// <param name="parent">The parent window</param>
        /// <param name="title">The dialog title</param>
        /// <param name="message">The dialog message</param>
        public static async Task ShowInfoDialogAsync(Window parent, string title, string message)
        {
            var messageWindow = new MessageWindow(title, message);
            await messageWindow.ShowDialog(parent);
        }
    }
    
    /// <summary>
    /// A simple confirmation dialog with Yes/No buttons.
    /// </summary>
    internal class ConfirmationDialog : Window
    {
        public ConfirmationDialog(string title, string message)
        {
            Title = title;
            Width = 350;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var messageBlock = new TextBlock 
            { 
                Text = message, 
                Margin = new Thickness(15), 
                TextWrapping = Avalonia.Media.TextWrapping.Wrap 
            };

            var yesButton = new Button { Content = "Yes", IsDefault = true, Margin = new Thickness(5) };
            yesButton.Click += (_, _) => Close(true);

            var noButton = new Button { Content = "No", IsCancel = true, Margin = new Thickness(5) };
            noButton.Click += (_, _) => Close(false);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center 
            };
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            var mainPanel = new StackPanel { Spacing = 10 };
            mainPanel.Children.Add(messageBlock);
            mainPanel.Children.Add(buttonPanel);

            Content = mainPanel;
        }
    }
}

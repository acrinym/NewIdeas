using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;

namespace Cycloside;

/// <summary>
/// Simple window to display a message with an OK button.
/// </summary>
public class MessageWindow : Window
{
    public MessageWindow(string title, string message)
    {
        Title = title;
        Width = 350;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var text = new TextBlock
        {
            Text = message,
            Margin = new Thickness(15),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        var ok = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(5) };
        ok.Click += (_, _) => Close();

        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(text);
        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { ok }
        });

        Content = panel;
    }
}

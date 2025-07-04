using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using System;
using Cycloside;
using Cycloside.Services;

namespace Cycloside.Widgets.BuiltIn;

public class WeatherSettingsWindow : Window
{
    private readonly TextBox _latBox;
    private readonly TextBox _lonBox;
    private readonly TextBox _cityBox;
    private readonly Action _onSaved;

    public WeatherSettingsWindow(Action onSaved)
    {
        _onSaved = onSaved;
        Title = "Weather Location";
        Width = 250;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 5 };
        panel.Children.Add(new TextBlock { Text = "City (optional):" });
        _cityBox = new TextBox { Text = SettingsManager.Settings.WeatherCity };
        panel.Children.Add(_cityBox);
        panel.Children.Add(new TextBlock { Text = "Latitude:" });
        _latBox = new TextBox { Text = SettingsManager.Settings.WeatherLatitude.ToString() };
        panel.Children.Add(_latBox);
        panel.Children.Add(new TextBlock { Text = "Longitude:" });
        _lonBox = new TextBox { Text = SettingsManager.Settings.WeatherLongitude.ToString() };
        panel.Children.Add(_lonBox);

        var save = new Button { Content = "Save", HorizontalAlignment = HorizontalAlignment.Right };
        save.Click += Save_Click;
        panel.Children.Add(save);

        Content = panel;

        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(WeatherSettingsWindow));
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (double.TryParse(_latBox.Text, out var lat))
            SettingsManager.Settings.WeatherLatitude = lat;
        if (double.TryParse(_lonBox.Text, out var lon))
            SettingsManager.Settings.WeatherLongitude = lon;
        SettingsManager.Settings.WeatherCity = _cityBox.Text ?? string.Empty;
        SettingsManager.Save();
        _onSaved?.Invoke();
        Close();
    }
}

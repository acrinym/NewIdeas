using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cycloside.Widgets.BuiltIn;

public class WeatherWidget : IWidget
{
    public string Name => "Weather";

    public Control BuildView()
    {
        var text = new TextBlock { Foreground = Brushes.White };
        _ = UpdateAsync(text);
        var border = new Border
        {
            Background = Brushes.Black,
            Opacity = 0.7,
            Padding = new Thickness(4),
            Child = text
        };
        border.PointerPressed += (_, e) =>
        {
            if (e.ClickCount == 2)
                new WeatherSettingsWindow(async () => await UpdateAsync(text)).Show();
        };
        return border;
    }

    private async Task UpdateAsync(TextBlock block)
    {
        try
        {
            using var client = new HttpClient();
            var lat = SettingsManager.Settings.WeatherLatitude;
            var lon = SettingsManager.Settings.WeatherLongitude;
            var city = SettingsManager.Settings.WeatherCity;
            if (!string.IsNullOrWhiteSpace(city))
            {
                var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1";
                var geoJson = await client.GetStringAsync(geoUrl);
                using var geoDoc = JsonDocument.Parse(geoJson);
                if (geoDoc.RootElement.TryGetProperty("results", out var res) && res.GetArrayLength() > 0)
                {
                    var first = res[0];
                    lat = first.GetProperty("latitude").GetDouble();
                    lon = first.GetProperty("longitude").GetDouble();
                }
            }

            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
            var json = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var temp = doc.RootElement.GetProperty("current_weather").GetProperty("temperature").GetDouble();
            block.Text = $"Temp: {temp}°C";
        }
        catch
        {
            block.Text = "Weather unavailable";
        }
    }
}

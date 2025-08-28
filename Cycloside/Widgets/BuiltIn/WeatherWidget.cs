using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside.Widgets.BuiltIn;

public class WeatherWidget : IWidget
{
    private static readonly HttpClient s_client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient();
        c.Timeout = TimeSpan.FromSeconds(5);
        return c;
    }

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
        // Open settings on double-click
        border.PointerPressed += (_, e) =>
        {
            if (e.ClickCount == 2)
                new WeatherSettingsWindow(async () => await UpdateAsync(text)).Show();
        };

        // Subscribe to global refresh so multiple widgets update together
        Action<object?> handler = async _ => await UpdateAsync(text);
        PluginBus.Subscribe("weather:refresh", handler);
        border.DetachedFromVisualTree += (_, __) => PluginBus.Unsubscribe("weather:refresh", handler);
        return border;
    }

    private async Task UpdateAsync(TextBlock block)
    {
        try
        {
            var client = s_client;
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
        catch (Exception ex)
        {
            Services.Logger.Error($"Weather error: {ex.Message}");
            block.Text = "Weather unavailable";
        }
    }
}

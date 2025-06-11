using Avalonia.Controls;
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
        return new Border
        {
            Background = Brushes.Black,
            Opacity = 0.7,
            Padding = new Thickness(4),
            Child = text
        };
    }

    private async Task UpdateAsync(TextBlock block)
    {
        try
        {
            using var client = new HttpClient();
            var url = "https://api.open-meteo.com/v1/forecast?latitude=35&longitude=139&current_weather=true";
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

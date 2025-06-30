using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using System.IO;

namespace Cycloside.Plugins.BuiltIn.Views;

public partial class MP3PlayerWindow : Window
{
    public MP3PlayerWindow()
    {
        InitializeComponent();
    }

    private void SeekSlider_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is MP3PlayerPlugin vm && sender is Slider slider)
        {
            var seekTime = TimeSpan.FromSeconds(slider.Value);
            if (vm.SeekCommand.CanExecute(seekTime))
            {
                vm.SeekCommand.Execute(seekTime);
            }
        }
    }
}

/// <summary>
/// A simple value converter to display just the file name from a full path.
/// </summary>
public class FullPathToFileNameConverter : IValueConverter
{
    public static readonly FullPathToFileNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string fullPath && !string.IsNullOrEmpty(fullPath))
        {
            try
            {
                return Path.GetFileNameWithoutExtension(fullPath);
            }
            catch
            {
                return fullPath;
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

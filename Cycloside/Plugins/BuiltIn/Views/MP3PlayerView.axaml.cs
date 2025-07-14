using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using System.IO;

// Chose the more modern file-scoped namespace syntax.
namespace Cycloside.Plugins.BuiltIn.Views;

public partial class MP3PlayerView : UserControl
{
    public MP3PlayerView()
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
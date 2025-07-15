using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Cycloside;

public partial class HotkeySettingsWindow : Window
{
    public ObservableCollection<HotkeyPair> HotkeyPairs { get; } = new();

    public IRelayCommand SaveCommand { get; }

    public HotkeySettingsWindow()
    {
        InitializeComponent();
        DataContext = this;
        foreach (var kv in SettingsManager.Settings.Hotkeys)
        {
            HotkeyPairs.Add(new HotkeyPair { Name = kv.Key, Gesture = kv.Value });
        }
        SaveCommand = new RelayCommand(Save);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void Save()
    {
        SettingsManager.Settings.Hotkeys.Clear();
        foreach (var pair in HotkeyPairs)
        {
            SettingsManager.Settings.Hotkeys[pair.Name] = pair.Gesture ?? string.Empty;
        }
        SettingsManager.Save();
        Close();
    }

    public partial class HotkeyPair : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        [ObservableProperty]
        private string? gesture;
    }
}

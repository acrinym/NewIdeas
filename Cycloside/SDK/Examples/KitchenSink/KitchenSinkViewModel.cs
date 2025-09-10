using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cycloside.Services;

namespace Cycloside.SDK.Examples.KitchenSink
{
    public partial class KitchenSinkViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _messageFromBus;

        [ObservableProperty]
        private string? _persistedSetting;

        public KitchenSinkViewModel()
        {
            // Load the setting from the SettingsManager
            PersistedSetting = SettingsManager.GetPluginSetting("KitchenSink", "MySetting", "Default Value");

            // Subscribe to changes in the setting
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PersistedSetting))
                {
                    SettingsManager.SetPluginSetting("KitchenSink", "MySetting", PersistedSetting);
                }
            };
        }

        [RelayCommand]
        private void SendMessage()
        {
            PluginBus.Publish("kitchensink:message", "Hello from the Kitchen Sink!");
        }
    }
}

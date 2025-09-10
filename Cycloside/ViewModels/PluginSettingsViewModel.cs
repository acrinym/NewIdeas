using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Cycloside.Plugins;

namespace Cycloside.ViewModels
{
    public partial class PluginInfoViewModel : ObservableObject
    {
        private readonly IPlugin _plugin;
        private readonly PluginManager _pluginManager;

        public string Name => _plugin.Name;
        public string Description => _plugin.Description;
        public string Version => _plugin.Version.ToString();
        public PluginChangeStatus Status => _pluginManager.GetStatus(_plugin);

        public bool IsEnabled
        {
            get => _pluginManager.IsEnabled(_plugin);
            set
            {
                if (value)
                {
                    _pluginManager.EnablePlugin(_plugin);
                }
                else
                {
                    _pluginManager.DisablePlugin(_plugin);
                }
                OnPropertyChanged();
            }
        }

        public PluginInfoViewModel(IPlugin plugin, PluginManager pluginManager)
        {
            _plugin = plugin;
            _pluginManager = pluginManager;
        }
    }

    public partial class PluginSettingsViewModel : ObservableObject
    {
        public ObservableCollection<PluginInfoViewModel> Plugins { get; } = new();

        public PluginSettingsViewModel(PluginManager pluginManager)
        {
            foreach (var plugin in pluginManager.Plugins)
            {
                Plugins.Add(new PluginInfoViewModel(plugin, pluginManager));
            }
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Cycloside.Plugins;

namespace Cycloside.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<IPlugin> AvailablePlugins { get; }

        public ICommand? ExitCommand { get; set; }
        public ICommand? StartPluginCommand { get; set; }

        public MainWindowViewModel(IEnumerable<IPlugin> plugins)
        {
            AvailablePlugins = new ObservableCollection<IPlugin>(plugins);
        }
    }
}

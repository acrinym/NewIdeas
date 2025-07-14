using CommunityToolkit.Mvvm.ComponentModel;
using Cycloside.Plugins;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Cycloside.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<IPlugin> AvailablePlugins { get; }

        public ObservableCollection<WorkspaceItemViewModel> WorkspaceItems { get; } = new();
        [ObservableProperty]
        private WorkspaceItemViewModel? _selectedWorkspaceItem;

        public ICommand? ExitCommand { get; set; }
        public ICommand? StartPluginCommand { get; set; }

        public MainWindowViewModel(IEnumerable<IPlugin> plugins)
        {
            AvailablePlugins = new ObservableCollection<IPlugin>(plugins);
        }
    }
}
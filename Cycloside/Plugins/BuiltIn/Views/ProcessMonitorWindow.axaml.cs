using Avalonia.Markup.Xaml;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class ProcessMonitorWindow : PluginWindowBase
    {
        public ProcessMonitorWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Logger.Log("ProcessMonitorWindow initialized");
        }
    }
}

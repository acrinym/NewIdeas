using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cycloside.Models;
using Cycloside.ViewModels;

namespace Cycloside.Views;

public partial class StartupConfigurationWindow : Window
{
    public StartupConfiguration? Result { get; private set; }

    public StartupConfigurationWindow()
    {
        InitializeComponent();
    }

    public StartupConfigurationWindow(Plugins.PluginManager pluginManager, Action<StartupConfiguration> onComplete)
    {
        InitializeComponent();

        DataContext = new StartupConfigurationViewModel(pluginManager, config =>
        {
            Result = config;
            Close();
            onComplete(config);
        });
    }
}

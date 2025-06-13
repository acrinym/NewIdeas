using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cycloside.ViewModels;
using System.Linq;

namespace Cycloside.Views;

public partial class WizardWindow : Window
{
    public WizardWindow()
    {
        InitializeComponent();
        DataContext = new WizardViewModel();
        ThemeManager.ApplyFromSettings(this, "Plugins");
        CursorManager.ApplyFromSettings(this, "Plugins");
        SkinManager.LoadForWindow(this);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(WizardWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WizardViewModel vm && vm.CurrentStep > 0)
            vm.CurrentStep--;
    }

    private void Next_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WizardViewModel vm)
            return;

        if (vm.CurrentStep < 4)
        {
            vm.CurrentStep++;
            return;
        }

        SettingsManager.Settings.ActiveSkin = vm.SelectedTheme;
        foreach (var item in vm.Plugins)
            SettingsManager.Settings.PluginEnabled[item.Name] = item.IsEnabled;
        var profile = new WorkspaceProfile
        {
            Name = vm.ProfileName,
            Plugins = vm.Plugins.ToDictionary(p => p.Name, p => p.IsEnabled)
        };
        WorkspaceProfiles.AddOrUpdate(profile);
        SettingsManager.Settings.ActiveProfile = vm.ProfileName;
        SettingsManager.Settings.FirstRun = false;
        SettingsManager.Save();
        Close();
    }
}

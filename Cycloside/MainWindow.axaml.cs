using Avalonia.Controls;

namespace Cycloside;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(MainWindow));
    }
}

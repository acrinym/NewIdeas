// FIX: Added likely using statements for your project's custom manager classes.
// You may need to adjust these namespaces to match your project structure if they differ.
using Cycloside;          // core services and models
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.ViewModels;
using Cycloside;
using System; // Required for EventHandler

namespace Cycloside.Views
{
    public partial class WizardWindow : Window
    {
        public WizardWindow()
        {
            InitializeComponent();

            var viewModel = new WizardViewModel();
            DataContext = viewModel;

            // FIX: On first run, apply a known-good default theme instead of trying to load
            // a theme from settings that don't exist yet. This prevents the "invisible wizard".
            if (SettingsManager.Settings.FirstRun)
            {
                // Assuming "Mint" is a valid theme name your ThemeManager understands.
                // This provides a safe, visible default for the first-time user experience.
                ThemeManager.ApplyTheme(this, "Mint"); 
            }
            else
            {
                // On subsequent runs, load the user's chosen theme from settings.
                ThemeManager.ApplyFromSettings(this, "Plugins");
            }
            
            // Assuming these are your other custom manager classes
            CursorManager.ApplyFromSettings(this, "Plugins");
            SkinManager.LoadForWindow(this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(WizardWindow));

            // Attach the Close logic to the ViewModel's request
            viewModel.RequestClose += (sender, e) => Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // NOTE: The Back_Click and Next_Click methods have been removed.
        // Their logic is now handled by commands within the WizardViewModel.
    }
}

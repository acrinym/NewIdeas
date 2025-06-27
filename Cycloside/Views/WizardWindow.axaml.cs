using Avalonia.Controls;
using Avalonia.Input; // Required for PointerPressedEventArgs
using Avalonia.Markup.Xaml;
using Cycloside.Services;
using Cycloside.ViewModels;

namespace Cycloside.Views
{
    public partial class WizardWindow : Window
    {
        public WizardWindow()
        {
            InitializeComponent();

            var viewModel = new WizardViewModel();
            DataContext = viewModel;

            // Apply custom styling and effects
            CursorManager.ApplyFromSettings(this, "Plugins");
            WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(WizardWindow));

            // Subscribe to the ViewModel's request to close the window.
            viewModel.RequestClose += (sender, e) => Close();

            // *** ADDED: Attach the event handler for dragging the window ***
            this.PointerPressed += Window_PointerPressed;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// This event handler makes our borderless window draggable.
        /// </summary>
        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // We only want to drag when the left mouse button is pressed.
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // Ensure the DataContext is our ViewModel before dragging.
                if (DataContext is WizardViewModel)
                {
                    // This command tells the OS to start a drag operation.
                    BeginMoveDrag(e);
                }
            }
        }
    }
}

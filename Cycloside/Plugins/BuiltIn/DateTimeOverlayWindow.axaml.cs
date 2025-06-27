using Avalonia.Controls;
using Avalonia.Input;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class DateTimeOverlayWindow : Window
    {
        public DateTimeOverlayWindow()
        {
            InitializeComponent();
        }

        // This event handler makes our borderless window draggable.
        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // We only want to drag with the left mouse button.
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // Check if the DataContext is our ViewModel and if it's not locked.
                if (DataContext is DateTimeOverlayPlugin vm && !vm.IsLocked)
                {
                    BeginMoveDrag(e);
                }
            }
        }
    }
}

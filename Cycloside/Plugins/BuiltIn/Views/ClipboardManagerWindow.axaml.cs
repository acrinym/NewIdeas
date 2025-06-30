using Avalonia.Controls;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class ClipboardManagerWindow : Window
    {
        public ClipboardManagerWindow()
        {
            // This line is essential to load the UI defined in the .axaml file.
            InitializeComponent();
        }

        private void ListBox_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ClipboardManagerPlugin vm && sender is ListBox lb && lb.SelectedItem is string text)
            {
                if (vm.EntrySelectedCommand.CanExecute(text))
                {
                    vm.EntrySelectedCommand.Execute(text);
                }
            }
        }
    }
}

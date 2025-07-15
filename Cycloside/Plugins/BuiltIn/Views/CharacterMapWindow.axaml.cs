using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Cycloside.Plugins.BuiltIn
{
    public partial class CharacterMapWindow : Window
    {
        public CharacterMapWindow()
        {
            InitializeComponent();
            CharList.DoubleTapped += CharList_DoubleTapped;
        }

        private void CharList_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CharacterMapPlugin vm && sender is ListBox lb && lb.SelectedItem is string ch)
            {
                if (vm.CharacterSelectedCommand.CanExecute(ch))
                {
                    vm.CharacterSelectedCommand.Execute(ch);
                }
            }
        }
    }
}

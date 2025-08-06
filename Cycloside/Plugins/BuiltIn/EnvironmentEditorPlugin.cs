using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Cycloside.Services;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    public class EnvironmentEditorPlugin : IPlugin
    {
        private EnvironmentEditorWindow? _window;
        private DataGrid? _grid;
        private RadioButton? _userScope;
        private RadioButton? _machineScope;
        private RadioButton? _processScope;
        private EnvironmentVariableTarget _currentTarget = EnvironmentVariableTarget.User;
        private readonly ObservableCollection<EnvItem> _items = new();

        public string Name => "Environment Editor";
        public string Description => "View and edit environment variables for different scopes.";
        public Version Version => new(1, 1, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new EnvironmentEditorWindow();
            _grid = _window.FindControl<DataGrid>("Grid");
            _userScope = _window.FindControl<RadioButton>("UserScope");
            _machineScope = _window.FindControl<RadioButton>("MachineScope");
            _processScope = _window.FindControl<RadioButton>("ProcessScope");
            var addButton = _window.FindControl<Button>("AddButton");
            var editButton = _window.FindControl<Button>("EditButton");
            var deleteButton = _window.FindControl<Button>("DeleteButton");

            if (OperatingSystem.IsWindows())
            {
                _userScope!.IsChecked = true;
                // FIXED: Use the modern IsCheckedChanged event instead of the obsolete Checked event.
                _userScope.IsCheckedChanged += (_, _) => { if (_userScope.IsChecked == true) UpdateScope(EnvironmentVariableTarget.User); };
                _machineScope!.IsCheckedChanged += (_, _) => { if (_machineScope.IsChecked == true) UpdateScope(EnvironmentVariableTarget.Machine); };
                _processScope!.IsCheckedChanged += (_, _) => { if (_processScope.IsChecked == true) UpdateScope(EnvironmentVariableTarget.Process); };
            }
            else
            {
                _userScope!.IsEnabled = false;
                _machineScope!.IsEnabled = false;
                _processScope!.IsChecked = true;
                _processScope.IsCheckedChanged += (_, _) => { if (_processScope.IsChecked == true) UpdateScope(EnvironmentVariableTarget.Process); };
                _currentTarget = EnvironmentVariableTarget.Process;
            }

            addButton?.AddHandler(Button.ClickEvent, async (_, _) => await AddVariableAsync());
            editButton?.AddHandler(Button.ClickEvent, async (_, _) => await EditVariableAsync());
            deleteButton?.AddHandler(Button.ClickEvent, async (_, _) => await DeleteVariableAsync());

            if (_grid != null)
            {
                _grid.ItemsSource = _items;
            }

            LoadVariables();
            ThemeManager.ApplyForPlugin(_window, this);
            _window.Show();
        }

        private void LoadVariables()
        {
            var target = _currentTarget;
            
            _items.Clear();
            try
            {
                var variables = Environment.GetEnvironmentVariables(target);
                var variableList = new List<EnvItem>();
                
                foreach (DictionaryEntry de in variables)
                {
                    if (de.Key != null)
                    {
                        var key = de.Key.ToString()!;
                        var value = de.Value?.ToString() ?? string.Empty;
                        
                        // FIXED: Add some common environment variables that should always be visible
                        if (target == EnvironmentVariableTarget.Process)
                        {
                            // Add some system variables that might not be in the dictionary
                            if (!variableList.Any(v => v.Key == "PATH"))
                            {
                                variableList.Add(new EnvItem { Key = "PATH", Value = Environment.GetEnvironmentVariable("PATH") ?? "" });
                            }
                            if (!variableList.Any(v => v.Key == "TEMP"))
                            {
                                variableList.Add(new EnvItem { Key = "TEMP", Value = Environment.GetEnvironmentVariable("TEMP") ?? "" });
                            }
                            if (!variableList.Any(v => v.Key == "TMP"))
                            {
                                variableList.Add(new EnvItem { Key = "TMP", Value = Environment.GetEnvironmentVariable("TMP") ?? "" });
                            }
                            if (!variableList.Any(v => v.Key == "USERNAME"))
                            {
                                variableList.Add(new EnvItem { Key = "USERNAME", Value = Environment.GetEnvironmentVariable("USERNAME") ?? "" });
                            }
                            if (!variableList.Any(v => v.Key == "COMPUTERNAME"))
                            {
                                variableList.Add(new EnvItem { Key = "COMPUTERNAME", Value = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "" });
                            }
                        }
                        
                        variableList.Add(new EnvItem { Key = key, Value = value });
                    }
                }
                
                // Sort variables alphabetically for better organization
                variableList = variableList.OrderBy(v => v.Key).ToList();
                
                foreach (var item in variableList)
                {
                    _items.Add(item);
                }
                
                Logger.Log($"Environment Editor: Loaded {_items.Count} variables for {target} scope");
            }
            catch (Exception ex)
            {
                // Often a SecurityException if trying to read Machine scope without rights
                _items.Add(new EnvItem { Key = "ERROR", Value = $"Could not load variables for this scope: {ex.Message}" });
                Logger.Log($"Environment Editor: Failed to load variables for {target} scope: {ex.Message}");
            }
        }
        
        private void SaveVariables()
        {
            var target = _currentTarget;

            var originalKeys = new HashSet<string>();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables(target))
            {
                originalKeys.Add(de.Key.ToString()!);
            }

            var currentKeys = new HashSet<string>(_items.Select(i => i.Key));

            var removedKeys = originalKeys.Except(currentKeys);
            foreach (var key in removedKeys)
            {
                try
                {
                    Environment.SetEnvironmentVariable(key, null, target);
                }
                catch(Exception ex)
                {
                     _items.Add(new EnvItem { Key = "ERROR", Value = $"Could not remove '{key}': {ex.Message}" });
                }
            }

            foreach (var item in _items)
            {
                if(string.IsNullOrWhiteSpace(item.Key)) continue;

                try
                {
                    Environment.SetEnvironmentVariable(item.Key, item.Value, target);
                }
                catch (Exception ex)
                {
                    _items.Add(new EnvItem { Key = "ERROR", Value = $"Could not save '{item.Key}': {ex.Message}" });
                }
            }
            
            LoadVariables();
        }

        private void UpdateScope(EnvironmentVariableTarget target)
        {
            _currentTarget = target;
            LoadVariables();
        }

        private async Task AddVariableAsync()
        {
            if (_window is null) return;

            var vm = new AddEditVariableViewModel();
            var dlg = new AddEditVariableWindow { DataContext = vm };
            var result = await dlg.ShowDialog<EnvironmentEditorPlugin.EnvItem?>(_window);
            if (result != null)
            {
                try
                {
                    Environment.SetEnvironmentVariable(result.Key, result.Value, _currentTarget);
                    LoadVariables();
                }
                catch (Exception ex)
                {
                    ShowMessage("Error Adding Variable", ex.Message);
                }
            }
        }

        private async Task EditVariableAsync()
        {
            if (_window is null || _grid?.SelectedItem is not EnvItem selected) return;

            var vm = new AddEditVariableViewModel(selected);
            var dlg = new AddEditVariableWindow { DataContext = vm };
            var result = await dlg.ShowDialog<EnvironmentEditorPlugin.EnvItem?>(_window);
            if (result != null)
            {
                try
                {
                    Environment.SetEnvironmentVariable(result.Key, result.Value, _currentTarget);
                    LoadVariables();
                }
                catch (Exception ex)
                {
                    ShowMessage("Error Editing Variable", ex.Message);
                }
            }
        }

        private async Task DeleteVariableAsync()
        {
            if (_window is null || _grid?.SelectedItem is not EnvItem selected) return;

            var confirm = new ConfirmationWindow("Confirm Delete", $"Delete variable '{selected.Key}'?");
            if (await confirm.ShowDialog<bool>(_window))
            {
                try
                {
                    Environment.SetEnvironmentVariable(selected.Key, null, _currentTarget);
                    LoadVariables();
                }
                catch (Exception ex)
                {
                    ShowMessage("Error Deleting Variable", ex.Message);
                }
            }
        }

        private void ShowMessage(string title, string message)
        {
            if (_window is null) return;
            var msg = new MessageWindow(title, message);
            msg.ShowDialog(_window);
        }

        public void Stop()
        {
            _window?.Close();
        }

        public class EnvItem
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private class ConfirmationWindow : Window
        {
            public ConfirmationWindow(string title, string message)
            {
                Title = title;
                Width = 350;
                SizeToContent = SizeToContent.Height;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var msg = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(15),
                    TextWrapping = TextWrapping.Wrap
                };

                var yes = new Button { Content = "Yes", IsDefault = true, Margin = new Thickness(5) };
                yes.Click += (_, _) => Close(true);
                var no = new Button { Content = "No", IsCancel = true, Margin = new Thickness(5) };
                no.Click += (_, _) => Close(false);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                buttons.Children.Add(yes);
                buttons.Children.Add(no);

                var panel = new StackPanel { Spacing = 10 };
                panel.Children.Add(msg);
                panel.Children.Add(buttons);
                Content = panel;
            }
        }

        private class MessageWindow : Window
        {
            public MessageWindow(string title, string message)
            {
                Title = title;
                Width = 350;
                SizeToContent = SizeToContent.Height;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var msg = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(15),
                    TextWrapping = TextWrapping.Wrap
                };

                var ok = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(5) };
                ok.Click += (_, _) => Close();

                var panel = new StackPanel { Spacing = 10 };
                panel.Children.Add(msg);
                panel.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = { ok }
                });
                Content = panel;
            }
        }
    }
}

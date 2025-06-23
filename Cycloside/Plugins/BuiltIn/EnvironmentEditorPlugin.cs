using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn
{
    public class EnvironmentEditorPlugin : IPlugin
    {
        private Window? _window;
        private DataGrid? _grid;
        private ComboBox? _scopeSelector;
        private readonly ObservableCollection<EnvItem> _items = new();

        public string Name => "Environment Editor";
        public string Description => "View and edit environment variables for different scopes.";
        public Version Version => new(1, 0, 0);
        public Widgets.IWidget? Widget => null;

        public void Start()
        {
            // --- DataGrid Setup ---
            _grid = new DataGrid
            {
                ItemsSource = _items,
                AutoGenerateColumns = false, // We will define columns manually for better control
                Columns =
                {
                    new DataGridTextColumn { Header = "Key", Binding = new Avalonia.Data.Binding("Key"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) },
                    new DataGridTextColumn { Header = "Value", Binding = new Avalonia.Data.Binding("Value"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) }
                }
            };

            // --- UI Control Setup ---
            _scopeSelector = new ComboBox
            {
                ItemsSource = Enum.GetValues(typeof(EnvironmentVariableTarget)),
                SelectedIndex = (int)EnvironmentVariableTarget.User // Default to User scope, which is more useful
            };
            _scopeSelector.SelectionChanged += (s, e) => LoadVariables();

            var addButton = new Button { Content = "Add" };
            addButton.Click += (_, _) => _items.Add(new EnvItem { Key = "NEW_VARIABLE", Value = "new value" });

            var removeButton = new Button { Content = "Remove" };
            removeButton.Click += (_, _) =>
            {
                if (_grid?.SelectedItem is EnvItem selectedItem)
                {
                    _items.Remove(selectedItem);
                }
            };
            
            var saveButton = new Button { Content = "Save Changes" };
            saveButton.Click += (_, _) => SaveVariables();
            
            // --- Layout Panels ---
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 5, 
                Margin = new Thickness(5) 
            };
            buttonPanel.Children.Add(new Label { Content = "Scope:" });
            buttonPanel.Children.Add(_scopeSelector);
            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(removeButton);
            buttonPanel.Children.Add(saveButton);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Top);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(_grid); // The DataGrid will fill the remaining space

            // --- Window Setup ---
            _window = new Window
            {
                Title = "Environment Variables Editor",
                Width = 700,
                Height = 500,
                Content = mainPanel
            };
            // Assuming these are your custom manager classes
            // ThemeManager.ApplyFromSettings(_window, "Plugins");
            // WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(EnvironmentEditorPlugin));
            
            LoadVariables(); // Initial load
            _window.Show();
        }
        
        private void LoadVariables()
        {
            if (_scopeSelector?.SelectedItem is not EnvironmentVariableTarget target) return;
            
            _items.Clear();
            try
            {
                var variables = Environment.GetEnvironmentVariables(target);
                foreach (DictionaryEntry de in variables)
                {
                    if (de.Key != null)
                    {
                        _items.Add(new EnvItem { Key = de.Key.ToString()!, Value = de.Value?.ToString() ?? string.Empty });
                    }
                }
            }
            catch (Exception ex)
            {
                // Often a SecurityException if trying to read Machine scope without rights
                _items.Add(new EnvItem { Key = "ERROR", Value = $"Could not load variables for this scope: {ex.Message}" });
            }
        }
        
        private void SaveVariables()
        {
            if (_scopeSelector?.SelectedItem is not EnvironmentVariableTarget target) return;

            // Important: A straight save is destructive. We need to compare to original state.
            // First, get all original keys for the target scope.
            var originalKeys = new HashSet<string>();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables(target))
            {
                originalKeys.Add(de.Key.ToString()!);
            }

            // Get all keys currently in our editor.
            var currentKeys = new HashSet<string>(_items.Select(i => i.Key));

            // Find keys that were removed.
            var removedKeys = originalKeys.Except(currentKeys);
            foreach (var key in removedKeys)
            {
                try
                {
                    Environment.SetEnvironmentVariable(key, null, target); // Setting value to null removes it.
                }
                catch(Exception ex)
                {
                     _items.Add(new EnvItem { Key = "ERROR", Value = $"Could not remove '{key}': {ex.Message}" });
                }
            }

            // Set/update all current keys.
            foreach (var item in _items)
            {
                if(string.IsNullOrWhiteSpace(item.Key)) continue;

                try
                {
                    Environment.SetEnvironmentVariable(item.Key, item.Value, target);
                }
                catch (Exception ex)
                {
                    // This will likely fail for Machine scope without admin rights.
                    _items.Add(new EnvItem { Key = "ERROR", Value = $"Could not save '{item.Key}': {ex.Message}" });
                }
            }
            
            // Show a confirmation or status. For now, just reload the list.
            LoadVariables();
        }

        public void Stop()
        {
            _window?.Close();
        }

        private class EnvItem
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}

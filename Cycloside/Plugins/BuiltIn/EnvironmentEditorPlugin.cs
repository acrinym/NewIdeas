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
        private EnvironmentEditorWindow? _window;
        private DataGrid? _grid;
        private ComboBox? _scopeSelector;
        private readonly ObservableCollection<EnvItem> _items = new();

        public string Name => "Environment Editor";
        public string Description => "View and edit environment variables for different scopes.";
        public Version Version => new(1, 0, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _window = new EnvironmentEditorWindow();
            _grid = _window.FindControl<DataGrid>("Grid");
            _scopeSelector = _window.FindControl<ComboBox>("ScopeSelector");
            var addButton = _window.FindControl<Button>("AddButton");
            var removeButton = _window.FindControl<Button>("RemoveButton");
            var saveButton = _window.FindControl<Button>("SaveButton");

            if (_scopeSelector != null)
            {
                _scopeSelector.ItemsSource = Enum.GetValues(typeof(EnvironmentVariableTarget));
                _scopeSelector.SelectedIndex = (int)EnvironmentVariableTarget.User;
                _scopeSelector.SelectionChanged += (s, e) => LoadVariables();
            }

            addButton?.AddHandler(Button.ClickEvent, (_, _) => _items.Add(new EnvItem { Key = "NEW_VARIABLE", Value = "new value" }));
            removeButton?.AddHandler(Button.ClickEvent, (_, _) =>
            {
                if (_grid?.SelectedItem is EnvItem selectedItem)
                {
                    _items.Remove(selectedItem);
                }
            });
            saveButton?.AddHandler(Button.ClickEvent, (_, _) => SaveVariables());

            if (_grid != null)
            {
                _grid.ItemsSource = _items;
                _grid.AutoGenerateColumns = false;
                _grid.Columns.Add(new DataGridTextColumn { Header = "Key", Binding = new Avalonia.Data.Binding("Key"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                _grid.Columns.Add(new DataGridTextColumn { Header = "Value", Binding = new Avalonia.Data.Binding("Value"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            }

            LoadVariables();
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

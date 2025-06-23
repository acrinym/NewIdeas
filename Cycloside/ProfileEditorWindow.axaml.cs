using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cycloside.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside
{
    public partial class ProfileEditorWindow : Window
    {
        private readonly PluginManager _manager;
        private string _originalName = string.Empty;

        // UI Control references for convenience
        private ListBox? _profileList;
        private TextBox? _nameBox;
        private TextBox? _wallpaperBox;
        private StackPanel? _pluginPanel;
        private TextBlock? _statusBlock;

        // Default constructor is required for the XAML previewer, but should not be used in production.
        public ProfileEditorWindow()
        {
            InitializeComponent();
            // This will cause a crash if used at runtime, which is a good signal
            // that the parameterized constructor is required.
            _manager = null!;
        }

        public ProfileEditorWindow(PluginManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            InitializeComponent();

            // Apply effects
            CursorManager.ApplyFromSettings(this, "Plugins");
            WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(ProfileEditorWindow));

            // Build the dynamic UI parts
            BuildProfileList();
            BuildPluginList();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            // Find controls once after the UI is loaded
            _profileList = this.FindControl<ListBox>("ProfileList");
            _nameBox = this.FindControl<TextBox>("NameBox");
            _wallpaperBox = this.FindControl<TextBox>("WallpaperBox");
            _pluginPanel = this.FindControl<StackPanel>("PluginPanel");
            _statusBlock = this.FindControl<TextBlock>("StatusBlock");
        }

        private void BuildProfileList()
        {
            if (_profileList == null) return;

            // Store the currently selected item to restore it after rebuilding the list
            var previouslySelected = _profileList.SelectedItem as string;

            _profileList.ItemsSource = WorkspaceProfiles.Profiles.Keys.OrderBy(name => name).ToList();
            _profileList.SelectionChanged -= OnProfileSelectionChanged; // Prevent event firing during update
            _profileList.SelectionChanged += OnProfileSelectionChanged;

            if (previouslySelected != null && (_profileList.ItemsSource as List<string>)!.Contains(previouslySelected))
            {
                _profileList.SelectedItem = previouslySelected;
            }
            else if (_profileList.Items.Count > 0)
            {
                _profileList.SelectedIndex = 0;
            }
            else
            {
                // No profiles, clear the form
                ClearForm();
            }
        }

        private void BuildPluginList()
        {
            if (_pluginPanel == null) return;

            _pluginPanel.Children.Clear();
            foreach (var plugin in _manager.Plugins.OrderBy(p => p.Name))
            {
                _pluginPanel.Children.Add(new CheckBox
                {
                    Content = plugin.Name,
                    Tag = plugin
                });
            }
        }

        private void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadSelectedProfile();
        }

        private void LoadSelectedProfile()
        {
            if (_profileList?.SelectedItem is not string name || !WorkspaceProfiles.Profiles.TryGetValue(name, out var profile))
            {
                ClearForm();
                return;
            }

            _originalName = name;

            if (_nameBox != null) _nameBox.Text = profile.Name;
            if (_wallpaperBox != null) _wallpaperBox.Text = profile.Wallpaper;
            if (_pluginPanel != null)
            {
                foreach (var child in _pluginPanel.Children.OfType<CheckBox>())
                {
                    if (child.Tag is IPlugin p)
                    {
                        child.IsChecked = profile.Plugins.TryGetValue(p.Name, out var isEnabled) && isEnabled;
                    }
                }
            }
            SetStatus($"Loaded profile: {name}");
        }

        private void ClearForm()
        {
            _originalName = string.Empty;
            if (_nameBox != null) _nameBox.Text = string.Empty;
            if (_wallpaperBox != null) _wallpaperBox.Text = string.Empty;
            if (_pluginPanel != null)
            {
                foreach (var child in _pluginPanel.Children.OfType<CheckBox>())
                {
                    child.IsChecked = false;
                }
            }
        }

        private void AddProfile(object? sender, RoutedEventArgs e)
        {
            try
            {
                var baseName = "NewProfile";
                var newName = baseName;
                var index = 1;
                while (WorkspaceProfiles.Profiles.ContainsKey(newName))
                {
                    newName = $"{baseName}{index++}";
                }

                WorkspaceProfiles.AddOrUpdate(new WorkspaceProfile { Name = newName });
                BuildProfileList();
                if (_profileList != null) _profileList.SelectedItem = newName;
                SetStatus($"Added new profile: {newName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error adding profile: {ex.Message}");
            }
        }

        private async void RemoveProfile(object? sender, RoutedEventArgs e)
        {
            if (_profileList?.SelectedItem is not string name) return;

            // Add a confirmation dialog for safety
            var confirmWindow = new ConfirmationWindow("Delete Profile", $"Are you sure you want to delete the profile '{name}'?");
            var result = await confirmWindow.ShowDialog<bool>(this);

            if (result)
            {
                WorkspaceProfiles.Remove(name);
                BuildProfileList();
                SetStatus($"Removed profile: {name}");
            }
        }

        private async void BrowseWallpaper(object? sender, RoutedEventArgs e)
        {
            try
            {
                var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Wallpaper Image",
                    FileTypeFilter = new[] { new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } } }
                });

                if (result.FirstOrDefault()?.TryGetLocalPath() is { } path && _wallpaperBox != null)
                {
                    _wallpaperBox.Text = path;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error Browse for wallpaper: {ex.Message}");
            }
        }

        private void SaveProfile(object? sender, RoutedEventArgs e)
        {
            var name = _nameBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SetStatus("Error: Profile name cannot be empty.");
                return;
            }

            // If the name is being changed, check if the new name already exists
            if (_originalName != name && WorkspaceProfiles.Profiles.ContainsKey(name))
            {
                SetStatus($"Error: A profile named '{name}' already exists.");
                return;
            }

            var wallpaper = _wallpaperBox?.Text ?? string.Empty;
            var pluginMap = new Dictionary<string, bool>();
            if (_pluginPanel != null)
            {
                foreach (var child in _pluginPanel.Children.OfType<CheckBox>())
                {
                    if (child.Tag is IPlugin p && child.IsChecked == true)
                    {
                        pluginMap[p.Name] = true;
                    }
                }
            }

            var profile = new WorkspaceProfile
            {
                Name = name,
                Wallpaper = wallpaper,
                Plugins = pluginMap
            };

            // If renaming, remove the old profile first
            if (!string.IsNullOrEmpty(_originalName) && _originalName != name)
            {
                WorkspaceProfiles.Remove(_originalName);
            }

            WorkspaceProfiles.AddOrUpdate(profile);
            BuildProfileList(); // Rebuild list to reflect changes (e.g., sorting)

            // Ensure the newly saved profile is selected
            if (_profileList != null) _profileList.SelectedItem = name;

            SetStatus($"Profile '{name}' saved successfully.");
        }

        private void SetStatus(string message)
        {
            if (_statusBlock == null) return;
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => _statusBlock.Text = message);
        }
    }

    /// <summary>
    /// A simple reusable confirmation window.
    /// </summary>
    public class ConfirmationWindow : Window
    {
        public ConfirmationWindow(string title, string message)
        {
            Title = title;
            Width = 350;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var messageBlock = new TextBlock { Text = message, Margin = new Thickness(15), TextWrapping = Avalonia.Media.TextWrapping.Wrap };

            var yesButton = new Button { Content = "Yes", IsDefault = true, Margin = new Thickness(5) };
            yesButton.Click += (_, _) => Close(true);

            var noButton = new Button { Content = "No", IsCancel = true, Margin = new Thickness(5) };
            noButton.Click += (_, _) => Close(false);

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            buttonPanel.Children.Add(yesButton);
buttonPanel.Children.Add(noButton);

            var mainPanel = new StackPanel { Spacing = 10 };
            mainPanel.Children.Add(messageBlock);
            mainPanel.Children.Add(buttonPanel);

            Content = mainPanel;
        }
    }
}
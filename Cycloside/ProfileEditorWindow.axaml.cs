using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using System;
using System.Linq;

namespace Cycloside;

public partial class ProfileEditorWindow : Window
{
    private readonly PluginManager _manager;
    private string _originalName = string.Empty;

    public ProfileEditorWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, "Plugins");
        CursorManager.ApplyFromSettings(this, "Plugins");
        SkinManager.LoadForWindow(this);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(ProfileEditorWindow));
        BuildProfileList();
        BuildPluginList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildProfileList()
    {
        var list = this.FindControl<ListBox>("ProfileList");
        list.Items = WorkspaceProfiles.Profiles.Keys.ToList();
        list.SelectionChanged += (_, _) => LoadSelectedProfile();
        if (list.Items.Count > 0)
            list.SelectedIndex = 0;
    }

    private void BuildPluginList()
    {
        var panel = this.FindControl<StackPanel>("PluginPanel");
        panel.Children.Clear();
        foreach (var plugin in _manager.Plugins)
        {
            var cb = new CheckBox
            {
                Content = plugin.Name,
                Tag = plugin
            };
            panel.Children.Add(cb);
        }
    }

    private void LoadSelectedProfile()
    {
        var list = this.FindControl<ListBox>("ProfileList");
        if (list.SelectedItem is not string name ||
            !WorkspaceProfiles.Profiles.TryGetValue(name, out var profile))
            return;

        _originalName = name;
        this.FindControl<TextBox>("NameBox").Text = profile.Name;
        this.FindControl<TextBox>("WallpaperBox").Text = profile.Wallpaper;
        var panel = this.FindControl<StackPanel>("PluginPanel");
        foreach (var child in panel.Children.OfType<CheckBox>())
        {
            if (child.Tag is IPlugin p)
                child.IsChecked = profile.Plugins.TryGetValue(p.Name, out var en) && en;
        }
    }

    private void AddProfile(object? sender, RoutedEventArgs e)
    {
        var name = "NewProfile";
        var idx = 1;
        while (WorkspaceProfiles.Profiles.ContainsKey(name + idx))
            idx++;
        name += idx;
        WorkspaceProfiles.AddOrUpdate(new WorkspaceProfile { Name = name });
        BuildProfileList();
        var list = this.FindControl<ListBox>("ProfileList");
        list.SelectedItem = name;
    }

    private void RemoveProfile(object? sender, RoutedEventArgs e)
    {
        var list = this.FindControl<ListBox>("ProfileList");
        if (list.SelectedItem is string name)
        {
            WorkspaceProfiles.Remove(name);
            BuildProfileList();
        }
    }

    private async void BrowseWallpaper(object? sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog();
        dlg.Filters.Add(new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg", "bmp" } });
        var files = await dlg.ShowAsync(this);
        if (files is { Length: > 0 })
            this.FindControl<TextBox>("WallpaperBox").Text = files[0];
    }

    private void SaveProfile(object? sender, RoutedEventArgs e)
    {
        var name = this.FindControl<TextBox>("NameBox").Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return;
        var wallpaper = this.FindControl<TextBox>("WallpaperBox").Text ?? string.Empty;
        var map = new System.Collections.Generic.Dictionary<string, bool>();
        var panel = this.FindControl<StackPanel>("PluginPanel");
        foreach (var child in panel.Children.OfType<CheckBox>())
        {
            if (child.Tag is IPlugin p)
                map[p.Name] = child.IsChecked == true;
        }
        var profile = new WorkspaceProfile
        {
            Name = name,
            Wallpaper = wallpaper,
            Plugins = map
        };
        if(!string.IsNullOrEmpty(_originalName) && _originalName != name)
            WorkspaceProfiles.Remove(_originalName);
        WorkspaceProfiles.AddOrUpdate(profile);
        BuildProfileList();
        var list = this.FindControl<ListBox>("ProfileList");
        list.SelectedItem = name;
    }
}

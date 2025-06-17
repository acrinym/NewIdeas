using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class DiskUsagePlugin : IPlugin
{
    private Window? _window;
    private TreeView? _tree;

    public string Name => "Disk Usage";
    public string Description => "Visualize disk usage";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _tree = new TreeView();
        var button = new Button { Content = "Select Folder" };
        button.Click += async (_, __) =>
        {
            if (_window == null) return;
            var folders = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (!string.IsNullOrWhiteSpace(path))
                LoadTree(path);
        };

        var panel = new StackPanel();
        panel.Children.Add(button);
        panel.Children.Add(_tree);

        _window = new Window
        {
            Title = "Disk Usage",
            Width = 500,
            Height = 400,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(DiskUsagePlugin));
        _window.Show();
    }

    private void LoadTree(string root)
    {
        var rootNode = BuildNode(new DirectoryInfo(root));
        _tree!.ItemsSource = new[] { rootNode };
    }

    private static TreeViewItem BuildNode(DirectoryInfo dir)
    {
        long size = dir.GetFiles().Sum(f => f.Length);
        foreach (var sub in dir.GetDirectories())
            size += GetDirSize(sub);

        var node = new TreeViewItem { Header = $"{dir.Name} ({size / 1024 / 1024} MB)" };
        node.ItemsSource = dir.GetDirectories().Select(BuildNode).ToList();
        return node;
    }

    private static long GetDirSize(DirectoryInfo dir)
    {
        long size = dir.GetFiles().Sum(f => f.Length);
        foreach (var sub in dir.GetDirectories())
            size += GetDirSize(sub);
        return size;
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        _tree = null;
    }
}

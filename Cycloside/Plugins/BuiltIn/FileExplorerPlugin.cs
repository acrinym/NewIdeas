using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Layout;
using Avalonia;
using Cycloside.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;

namespace Cycloside.Plugins.BuiltIn;

public class FileExplorerPlugin : IPlugin, IDisposable
{
    private FileExplorerWindow? _window;
    private TreeView? _tree;
    private ListBox? _list;
    private ObservableCollection<string> _items = new();
    private string _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string Name => "File Explorer";
    public string Description => "Browse and manage files";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new FileExplorerWindow
        {
            DataContext = this
        };
        _tree = _window.FindControl<TreeView>("DirectoryTree");
        _list = _window.FindControl<ListBox>("FileList");
        if (_list != null)
        {
            _list.ItemsSource = _items;
            _list.DoubleTapped += (_, __) => OpenSelected();
            _list.ContextMenu = BuildContextMenu();
        }
        if (_tree != null)
        {
            _tree.DoubleTapped += (_, __) => UpdatePathFromTree();
            PopulateTree();
        }
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(FileExplorerPlugin));
        _window.Show();
        RefreshList();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        _tree = null;
        _list = null;
        _items.Clear();
    }

    private void PopulateTree()
    {
        if (_tree == null) return;
        var drives = DriveInfo.GetDrives().Select(d => d.Name).ToList();
        _tree.ItemsSource = drives;
    }

    private void UpdatePathFromTree()
    {
        if (_tree?.SelectedItem is string path)
        {
            _currentPath = path;
            RefreshList();
        }
    }

    private void RefreshList()
    {
        if (_list == null) return;
        _items.Clear();
        try
        {
            foreach (var dir in Directory.GetDirectories(_currentPath))
                _items.Add(Path.GetFileName(dir) + Path.DirectorySeparatorChar);
            foreach (var file in Directory.GetFiles(_currentPath))
                _items.Add(Path.GetFileName(file));
        }
        catch (Exception ex)
        {
            Logger.Log($"Explorer error: {ex.Message}");
        }
    }

    private void OpenSelected()
    {
        if (_list?.SelectedItem is not string item) return;
        var path = Path.Combine(_currentPath, item.TrimEnd(Path.DirectorySeparatorChar));
        if (Directory.Exists(path))
        {
            _currentPath = path;
            RefreshList();
        }
        else if (File.Exists(path))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to open {path}: {ex.Message}");
            }
        }
    }

    private async void RenameSelected()
    {
        if (_list?.SelectedItem is not string item) return;
        var path = Path.Combine(_currentPath, item.TrimEnd(Path.DirectorySeparatorChar));
        if (!File.Exists(path) && !Directory.Exists(path)) return;

        var newName = await PromptAsync("Rename", item);
        if (string.IsNullOrWhiteSpace(newName) || newName == item) return;

        var newPath = Path.Combine(_currentPath, newName);
        try
        {
            if (File.Exists(path)) File.Move(path, newPath);
            else if (Directory.Exists(path)) Directory.Move(path, newPath);
            RefreshList();
        }
        catch (Exception ex)
        {
            Logger.Log($"Rename failed: {ex.Message}");
        }
    }

    private void DeleteSelected()
    {
        if (_list?.SelectedItem is not string item) return;
        var path = Path.Combine(_currentPath, item.TrimEnd(Path.DirectorySeparatorChar));
        try
        {
            if (File.Exists(path)) File.Delete(path);
            else if (Directory.Exists(path)) Directory.Delete(path, true);
            RefreshList();
        }
        catch (Exception ex)
        {
            Logger.Log($"Delete failed: {ex.Message}");
        }
    }

    private void OpenInEditor()
    {
        if (_list?.SelectedItem is not string item) return;
        var path = Path.Combine(_currentPath, item);
        if (File.Exists(path))
        {
            PluginBus.Publish("texteditor:open", path);
        }
    }

    private void EncryptSelected()
    {
        if (_list?.SelectedItem is not string item) return;
        var path = Path.Combine(_currentPath, item);
        if (File.Exists(path))
        {
            PluginBus.Publish("encryption:encryptFile", path);
        }
    }

    private void DecryptSelected()
    {
        if (_list?.SelectedItem is not string item) return;
        var path = Path.Combine(_currentPath, item);
        if (File.Exists(path))
        {
            PluginBus.Publish("encryption:decryptFile", path);
        }
    }

    private async Task<string?> PromptAsync(string title, string initial)
    {
        if (_window == null) return null;
        var win = new Window
        {
            Title = title,
            Width = 300,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };
        var box = new TextBox { Text = initial, Margin = new Thickness(10) };
        var ok = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(5) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(5) };
        ok.Click += (_, _) => win.Close(box.Text);
        cancel.Click += (_, _) => win.Close(null);
        win.Content = new StackPanel
        {
            Children =
            {
                box,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = { ok, cancel }
                }
            }
        };
        return await win.ShowDialog<string?>(_window);
    }

    private ContextMenu BuildContextMenu()
    {
        return new ContextMenu
        {
            ItemsSource = new[]
            {
                new MenuItem { Header = "Open", Command = ReactiveCommand.Create(OpenSelected) },
                new MenuItem { Header = "Rename", Command = ReactiveCommand.Create(RenameSelected) },
                new MenuItem { Header = "Delete", Command = ReactiveCommand.Create(DeleteSelected) },
                new MenuItem { Header = "Open in Code Editor", Command = ReactiveCommand.Create(OpenInEditor) },
                new MenuItem { Header = "Encrypt File", Command = ReactiveCommand.Create(EncryptSelected) },
                new MenuItem { Header = "Decrypt File", Command = ReactiveCommand.Create(DecryptSelected) }
            }
        };
    }

    public void Dispose() => Stop();
}

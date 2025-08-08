using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn
{
    public class DiskUsagePlugin : IPlugin
    {
        private DiskUsageWindow? _window;
        private TreeView? _tree;
        private Button? _selectFolderButton;
        private TextBlock? _statusText;

        public string Name => "Disk Usage";
        public string Description => "Visualize disk usage";
        public Version Version => new Version(0, 2, 0); // Incremented version for improvements
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            // Load window from XAML and grab named controls
            _window = new DiskUsageWindow();
            _tree = _window.FindControl<TreeView>("Tree");
            _selectFolderButton = _window.FindControl<Button>("SelectFolderButton");
            _statusText = _window.FindControl<TextBlock>("StatusText");

            if (_selectFolderButton != null)
                _selectFolderButton.Click += async (_, _) => await SelectAndLoadDirectoryAsync();

            // Apply theming and effects (assuming these are valid managers in your project)

            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(DiskUsagePlugin));

            _window.Show();
        }

        /// <summary>
        /// Asynchronously handles the folder selection and initiates the background loading process.
        /// </summary>
        private async Task SelectAndLoadDirectoryAsync()
        {
            if (_window == null || _selectFolderButton == null || _tree == null || _statusText == null) return;

            // Use the modern, recommended StorageProvider API to open a folder picker.
            var start = await DialogHelper.GetDefaultStartLocationAsync(_window.StorageProvider);
            var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a folder to analyze",
                AllowMultiple = false,
                SuggestedStartLocation = start
            });

            var selectedFolder = result.FirstOrDefault();
            var path = selectedFolder?.TryGetLocalPath();

            if (!string.IsNullOrWhiteSpace(path))
            {
                // Disable button and show a loading message to provide user feedback.
                _selectFolderButton.IsEnabled = false;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _tree.ItemsSource = null; // Clear previous results
                    _statusText.Text = $"Analyzing '{path}'... (This may take a while)";
                });

                try
                {
                    // Build a simple model on a background thread to avoid creating
                    // UI elements off the UI thread.
                    // Build the plain directory model on a background thread.
                    var rootModel = await Task.Run(() => BuildDirectoryModel(new DirectoryInfo(path)));

                    // Convert the model to UI elements on the UI thread to avoid
                    // cross-thread exceptions when constructing TreeViewItem instances.
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var rootItem = ConvertToTreeViewItem(rootModel);
                        _tree.ItemsSource = new[] { rootItem };
                        _statusText.Text = $"Analysis complete for '{path}'.";
                    });
                }
                catch (Exception ex)
                {
                    // Handle any unexpected errors during the process.
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _statusText.Text = $"Error: {ex.Message}";
                    });
                }
                finally
                {
                    // Always re-enable the button, even if an error occurred.
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _selectFolderButton.IsEnabled = true;
                    });
                }
            }
        }

        /// <summary>
        /// Plain model representing a directory and its children. This can be
        /// safely created on a background thread.
        /// </summary>
        private class DirectoryNode
        {
            public string Name { get; set; } = string.Empty;
            public long Size { get; set; }
            public bool AccessDenied { get; set; }
            public List<DirectoryNode> Children { get; set; } = new();
        }

        /// <summary>
        /// Recursively builds a DirectoryNode for a directory, calculating its
        /// total size. Designed to run on a background thread.
        /// </summary>
        /// <param name="dir">The directory to process.</param>
        private DirectoryNode BuildDirectoryModel(DirectoryInfo dir)
        {
            var node = new DirectoryNode { Name = dir.Name };
            long totalSize = 0;

            try
            {
                totalSize += dir.GetFiles().Sum(f => f.Length);

                foreach (var subDir in dir.GetDirectories())
                {
                    var child = BuildDirectoryModel(subDir);
                    totalSize += child.Size;
                    node.Children.Add(child);
                }
            }
            catch (UnauthorizedAccessException)
            {
                node.AccessDenied = true;
            }

            node.Size = totalSize;
            return node;
        }

        /// <summary>
        /// Converts a DirectoryNode into a TreeViewItem. Should be called on the
        /// UI thread.
        /// </summary>
        private TreeViewItem ConvertToTreeViewItem(DirectoryNode node)
        {
            var item = new TreeViewItem
            {
                Header = node.AccessDenied
                    ? $"{node.Name} (Access Denied)"
                    : $"{node.Name} ({FormatSize(node.Size)})",
                Tag = node.Size
            };

            item.ItemsSource = node.Children
                .OrderByDescending(n => n.Size)
                .Select(ConvertToTreeViewItem)
                .ToList();

            return item;
        }

        /// <summary>
        /// Formats a byte value into a human-readable string (KB, MB, GB).
        /// </summary>
        private static string FormatSize(long bytes)
        {
            const int scale = 1024;
            string[] orders = { "GB", "MB", "KB", "B" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 B";
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
            _tree = null;
            _selectFolderButton = null;
            _statusText = null;
        }
    }
}
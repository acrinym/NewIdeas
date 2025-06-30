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
            
            ThemeManager.ApplyFromSettings(_window, "Plugins");
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
            var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a folder to analyze",
                AllowMultiple = false
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
                    // Run the heavy directory scanning on a background thread to keep the UI responsive.
                    var rootNode = await Task.Run(() => BuildNodeRecursive(new DirectoryInfo(path)));

                    // Once done, update the UI on the UI thread.
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _tree.ItemsSource = new[] { rootNode };
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
        /// Recursively builds a TreeViewItem for a directory, calculating its total size.
        /// This method is designed to be run on a background thread.
        /// </summary>
        /// <param name="dir">The directory to process.</param>
        /// <returns>A TreeViewItem representing the directory and its contents.</returns>
        private TreeViewItem BuildNodeRecursive(DirectoryInfo dir)
        {
            long totalSize = 0;
            var subNodes = new List<TreeViewItem>();

            try
            {
                // Sum the size of all files in the current directory.
                totalSize += dir.GetFiles().Sum(file => file.Length);

                // Recursively process all subdirectories.
                foreach (var subDir in dir.GetDirectories())
                {
                    var subNode = BuildNodeRecursive(subDir);
                    // Add the size of the subdirectory to the parent's total.
                    totalSize += (long)(subNode.Tag ?? 0L);
                    subNodes.Add(subNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Gracefully handle directories that we don't have permission to access.
                return new TreeViewItem
                {
                    Header = $"{dir.Name} (Access Denied)",
                    Tag = 0L // Size is 0 as we couldn't read it.
                };
            }

            // Create the UI node for the current directory.
            var node = new TreeViewItem
            {
                Header = $"{dir.Name} ({FormatSize(totalSize)})",
                ItemsSource = subNodes.OrderByDescending(n => (long)(n.Tag ?? 0L)).ToList(),
                Tag = totalSize // Store the raw size in the Tag for sorting.
            };

            return node;
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
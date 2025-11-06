using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Widgets;
using Microsoft.Win32;
using System.IO;
using RegistryHive = Microsoft.Win32.RegistryHive;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// DIGITAL FORENSICS - Comprehensive digital evidence analysis and investigation toolkit
    /// Provides file system forensics, registry analysis, memory forensics, and artifact examination
    /// </summary>
    public class DigitalForensicsPlugin : IPlugin
    {
        public string Name => "Digital Forensics";
        public string Description => "Comprehensive digital evidence analysis and investigation toolkit";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.Security;

        public class DigitalForensicsWidget : IWidget
        {
            public string Name => "Digital Forensics";

            private TabControl? _mainTabControl;
            private TextBlock? _statusText;
            private ListBox? _fileAnalysisResults;
            private ListBox? _registryResults;
            private ListBox? _processMemoryResults;
            private ListBox? _artifacts;
            private ListBox? _timeline;
            private TextBox? _filePathInput;
            private TextBox? _registryPathInput;
            private TextBox? _processIdInput;

        public Control BuildView()
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10)
            };

                // Header
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var headerText = new TextBlock
                {
                    Text = "🔍 Digital Forensics",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold
                };

                var statusPanel = new Border
                {
                    Background = Brushes.LightGray,
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 4),
                    Margin = new Thickness(15, 0, 0, 0)
                };

                _statusText = new TextBlock
                {
                    Text = "Ready for forensic analysis",
                    FontSize = 12
                };

                statusPanel.Child = _statusText;

                headerPanel.Children.Add(headerText);
                headerPanel.Children.Add(statusPanel);

                // Warning banner
                var warningPanel = new Border
                {
                    Background = Brushes.Orange,
                    BorderBrush = Brushes.DarkOrange,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 0, 15),
                    Padding = new Thickness(10)
                };

                var warningText = new TextBlock
                {
                    Text = "⚠️ FORENSIC TOOLS: Use only for authorized investigations. Improper use may violate laws and privacy rights.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.DarkRed,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12
                };

                warningPanel.Child = warningText;

                // Main tab control
                _mainTabControl = new TabControl();

                // File Analysis Tab
                var fileTab = CreateFileAnalysisTab();
                _mainTabControl.Items.Add(fileTab);

                // Registry Analysis Tab
                var registryTab = CreateRegistryAnalysisTab();
                _mainTabControl.Items.Add(registryTab);

                // Memory Analysis Tab
                var memoryTab = CreateMemoryAnalysisTab();
                _mainTabControl.Items.Add(memoryTab);

                // Artifact Analysis Tab
                var artifactTab = CreateArtifactAnalysisTab();
                _mainTabControl.Items.Add(artifactTab);

                // Timeline Tab
                var timelineTab = CreateTimelineTab();
                _mainTabControl.Items.Add(timelineTab);

                mainPanel.Children.Add(headerPanel);
                mainPanel.Children.Add(warningPanel);
                mainPanel.Children.Add(_mainTabControl);

                var border = new Border
                {
                    Child = mainPanel,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10)
                };

                return border;
            }

            private TabItem CreateFileAnalysisTab()
            {
                var tab = new TabItem { Header = "📁 File Analysis" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // File selection
                var filePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var fileLabel = new TextBlock
                {
                    Text = "📄 File to Analyze:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var fileInputPanel = new StackPanel { Orientation = Orientation.Horizontal };

                _filePathInput = new TextBox
                {
                    Text = "C:\\Windows\\System32\\notepad.exe",
                    Width = 400,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var browseButton = new Button
                {
                    Content = "📂 Browse",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                browseButton.Click += OnBrowseFile;

                var analyzeButton = new Button
                {
                    Content = "🔍 Analyze File",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8),
                    Margin = new Thickness(10, 0, 0, 0)
                };
                analyzeButton.Click += OnAnalyzeFile;

                fileInputPanel.Children.Add(_filePathInput);
                fileInputPanel.Children.Add(browseButton);
                fileInputPanel.Children.Add(analyzeButton);

                filePanel.Children.Add(fileLabel);
                filePanel.Children.Add(fileInputPanel);

                // Results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var resultsLabel = new TextBlock
                {
                    Text = "📊 Analysis Results:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _fileAnalysisResults = new ListBox { Height = 400 };

                resultsPanel.Children.Add(resultsLabel);
                resultsPanel.Children.Add(_fileAnalysisResults);

                panel.Children.Add(filePanel);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateRegistryAnalysisTab()
            {
                var tab = new TabItem { Header = "🗂️ Registry Analysis" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Registry path input
                var registryPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var registryLabel = new TextBlock
                {
                    Text = "🔑 Registry Key to Analyze:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var registryInputPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var hiveLabel = new TextBlock { Text = "Hive:", Margin = new Thickness(0, 0, 10, 0) };
                var hiveCombo = new ComboBox { Width = 120 };
                hiveCombo.Items.Add("HKEY_LOCAL_MACHINE");
                hiveCombo.Items.Add("HKEY_CURRENT_USER");
                hiveCombo.Items.Add("HKEY_CLASSES_ROOT");
                hiveCombo.Items.Add("HKEY_USERS");
                hiveCombo.SelectedIndex = 0;

                var pathLabel = new TextBlock { Text = "Path:", Margin = new Thickness(10, 0, 10, 0) };
                _registryPathInput = new TextBox
                {
                    Text = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion",
                    Width = 300
                };

                registryInputPanel.Children.Add(hiveLabel);
                registryInputPanel.Children.Add(hiveCombo);
                registryInputPanel.Children.Add(pathLabel);
                registryInputPanel.Children.Add(_registryPathInput);

                var analyzeRegistryButton = new Button
                {
                    Content = "🔍 Analyze Registry",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8),
                    Margin = new Thickness(0, 10, 0, 0)
                };
                analyzeRegistryButton.Click += OnAnalyzeRegistry;

                registryPanel.Children.Add(registryLabel);
                registryPanel.Children.Add(registryInputPanel);
                registryPanel.Children.Add(analyzeRegistryButton);

                // Results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var resultsLabel = new TextBlock
                {
                    Text = "📋 Registry Analysis Results:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _registryResults = new ListBox { Height = 400 };

                resultsPanel.Children.Add(resultsLabel);
                resultsPanel.Children.Add(_registryResults);

                panel.Children.Add(registryPanel);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateMemoryAnalysisTab()
            {
                var tab = new TabItem { Header = "🧠 Memory Analysis" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Process selection
                var processPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var processLabel = new TextBlock
                {
                    Text = "⚙️ Process to Analyze:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var processInputPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var pidLabel = new TextBlock { Text = "PID:", Margin = new Thickness(0, 0, 10, 0) };
                _processIdInput = new TextBox
                {
                    Text = "1",
                    Width = 100,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var analyzeMemoryButton = new Button
                {
                    Content = "🧠 Analyze Memory",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                analyzeMemoryButton.Click += OnAnalyzeProcessMemory;

                processInputPanel.Children.Add(pidLabel);
                processInputPanel.Children.Add(_processIdInput);
                processInputPanel.Children.Add(analyzeMemoryButton);

                processPanel.Children.Add(processLabel);
                processPanel.Children.Add(processInputPanel);

                // Results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var resultsLabel = new TextBlock
                {
                    Text = "📊 Memory Analysis Results:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _processMemoryResults = new ListBox { Height = 400 };

                resultsPanel.Children.Add(resultsLabel);
                resultsPanel.Children.Add(_processMemoryResults);

                panel.Children.Add(processPanel);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateArtifactAnalysisTab()
            {
                var tab = new TabItem { Header = "🔎 Artifact Analysis" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Artifact type selection
                var artifactPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var artifactLabel = new TextBlock
                {
                    Text = "🔍 Artifact Type to Analyze:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var artifactButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                var browserButton = new Button
                {
                    Content = "🌐 Browser History",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(12, 6)
                };
                browserButton.Click += OnAnalyzeBrowserHistory;

                var logsButton = new Button
                {
                    Content = "📋 Event Logs",
                    Background = Brushes.Purple,
                    Foreground = Brushes.White,
                    Padding = new Thickness(12, 6)
                };
                logsButton.Click += OnAnalyzeEventLogs;

                var recentButton = new Button
                {
                    Content = "📁 Recent Files",
                    Background = Brushes.Orange,
                    Foreground = Brushes.White,
                    Padding = new Thickness(12, 6)
                };
                recentButton.Click += OnAnalyzeRecentFiles;

                var networkButton = new Button
                {
                    Content = "🌐 Network Connections",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(12, 6)
                };
                networkButton.Click += OnAnalyzeNetworkConnections;

                artifactButtons.Children.Add(browserButton);
                artifactButtons.Children.Add(logsButton);
                artifactButtons.Children.Add(recentButton);
                artifactButtons.Children.Add(networkButton);

                artifactPanel.Children.Add(artifactLabel);
                artifactPanel.Children.Add(artifactButtons);

                // Results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var resultsLabel = new TextBlock
                {
                    Text = "📋 Digital Artifacts:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _artifacts = new ListBox { Height = 400 };

                resultsPanel.Children.Add(resultsLabel);
                resultsPanel.Children.Add(_artifacts);

                panel.Children.Add(artifactPanel);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateTimelineTab()
            {
                var tab = new TabItem { Header = "📅 Forensic Timeline" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Timeline generation
                var timelinePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var timelineLabel = new TextBlock
                {
                    Text = "📅 Generate Forensic Timeline:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var timelineInputPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var pathLabel = new TextBlock { Text = "Directory:", Margin = new Thickness(0, 0, 10, 0) };
                var timelinePathInput = new TextBox
                {
                    Text = "C:\\Users",
                    Width = 300,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var generateTimelineButton = new Button
                {
                    Content = "📅 Generate Timeline",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                generateTimelineButton.Click += OnGenerateTimeline;

                timelineInputPanel.Children.Add(pathLabel);
                timelineInputPanel.Children.Add(timelinePathInput);
                timelineInputPanel.Children.Add(generateTimelineButton);

                timelinePanel.Children.Add(timelineLabel);
                timelinePanel.Children.Add(timelineInputPanel);

                // Timeline results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var resultsLabel = new TextBlock
                {
                    Text = "📋 Timeline Entries:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _timeline = new ListBox { Height = 400 };

                resultsPanel.Children.Add(resultsLabel);
                resultsPanel.Children.Add(_timeline);

                panel.Children.Add(timelinePanel);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private async void OnBrowseFile(object? sender, RoutedEventArgs e)
            {
                // For embedded widgets, we'll use the main window's storage provider
                var mainWindow = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                if (mainWindow?.MainWindow == null) return;

                var result = await mainWindow.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select File for Analysis",
                    AllowMultiple = false,
                    FileTypeFilter = new[] {
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (result != null && result.Any())
                {
                    var file = result.First();
                    if (_filePathInput != null)
                        _filePathInput.Text = file.Path.LocalPath;
                }
            }

            private async void OnAnalyzeFile(object? sender, RoutedEventArgs e)
            {
                var filePath = _filePathInput?.Text?.Trim();
                if (string.IsNullOrEmpty(filePath))
                {
                    UpdateStatus("❌ Please enter a file path");
                    return;
                }

                UpdateStatus($"🔍 Analyzing file: {Path.GetFileName(filePath)}...");

                try
                {
                    var result = await DigitalForensics.AnalyzeFileAsync(filePath);

                    if (_fileAnalysisResults != null)
                    {
                        var resultText = $"📄 {result.FileName} | Size: {result.FileSize:N0} bytes | Type: {result.FileType} | Modified: {result.LastWriteTime}";
                        _fileAnalysisResults.Items.Add(resultText);

                        if (_fileAnalysisResults.Items.Count > 50)
                            _fileAnalysisResults.Items.RemoveAt(0);
                    }

                    UpdateStatus($"✅ File analysis completed: {result.FileName}");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ File analysis failed: {ex.Message}");
                }
            }

            private async void OnAnalyzeRegistry(object? sender, RoutedEventArgs e)
            {
                var registryPath = _registryPathInput?.Text?.Trim();
                if (string.IsNullOrEmpty(registryPath))
                {
                    UpdateStatus("❌ Please enter a registry path");
                    return;
                }

                UpdateStatus($"🔍 Analyzing registry: {registryPath}...");

                try
                {
                    var results = await DigitalForensics.AnalyzeRegistryAsync(RegistryHive.LocalMachine, registryPath);

                    if (_registryResults != null)
                    {
                        foreach (var result in results)
                        {
                            var resultText = $"🔑 {result.KeyPath} | Values: {result.ValueCount} | Subkeys: {result.SubKeyCount}";
                            _registryResults.Items.Add(resultText);

                            if (_registryResults.Items.Count > 50)
                                _registryResults.Items.RemoveAt(0);
                        }
                    }

                    UpdateStatus($"✅ Registry analysis completed: {results.Count} keys analyzed");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Registry analysis failed: {ex.Message}");
                }
            }

            private async void OnAnalyzeProcessMemory(object? sender, RoutedEventArgs e)
            {
                if (!int.TryParse(_processIdInput?.Text?.Trim(), out var processId))
                {
                    UpdateStatus("❌ Please enter a valid process ID");
                    return;
                }

                UpdateStatus($"🧠 Analyzing process memory: PID {processId}...");

                try
                {
                    var result = await DigitalForensics.AnalyzeProcessMemoryAsync(processId);

                    if (_processMemoryResults != null)
                    {
                        var resultText = $"⚙️ {result.ProcessName} (PID {result.ProcessId}) | Memory: {result.MemoryUsage / 1024 / 1024:F1} MB | Threads: {result.ThreadCount}";
                        _processMemoryResults.Items.Add(resultText);

                        if (_processMemoryResults.Items.Count > 50)
                            _processMemoryResults.Items.RemoveAt(0);
                    }

                    UpdateStatus($"✅ Process memory analysis completed: {result.ProcessName}");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Process memory analysis failed: {ex.Message}");
                }
            }

            private async void OnAnalyzeBrowserHistory(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🔍 Analyzing browser history...");

                try
                {
                    var artifacts = await DigitalForensics.AnalyzeDigitalArtifactsAsync(ArtifactType.BrowserHistory);

                    if (_artifacts != null)
                    {
                        foreach (var artifact in artifacts)
                        {
                            var artifactText = $"🌐 {artifact.Description} | Size: {artifact.Size:N0} bytes | Modified: {artifact.Timestamp}";
                            _artifacts.Items.Add(artifactText);

                            if (_artifacts.Items.Count > 100)
                                _artifacts.Items.RemoveAt(0);
                        }
                    }

                    UpdateStatus($"✅ Browser history analysis completed: {artifacts.Count} artifacts found");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Browser history analysis failed: {ex.Message}");
                }
            }

            private async void OnAnalyzeEventLogs(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🔍 Analyzing event logs...");

                try
                {
                    var artifacts = await DigitalForensics.AnalyzeDigitalArtifactsAsync(ArtifactType.EventLogs);

                    if (_artifacts != null)
                    {
                        foreach (var artifact in artifacts)
                        {
                            var artifactText = $"📋 {artifact.Description} | Size: {artifact.Size:N0} bytes | Modified: {artifact.Timestamp}";
                            _artifacts.Items.Add(artifactText);

                            if (_artifacts.Items.Count > 100)
                                _artifacts.Items.RemoveAt(0);
                        }
                    }

                    UpdateStatus($"✅ Event log analysis completed: {artifacts.Count} artifacts found");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Event log analysis failed: {ex.Message}");
                }
            }

            private async void OnAnalyzeRecentFiles(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🔍 Analyzing recent files...");

                try
                {
                    var artifacts = await DigitalForensics.AnalyzeDigitalArtifactsAsync(ArtifactType.RecentFiles);

                    if (_artifacts != null)
                    {
                        foreach (var artifact in artifacts)
                        {
                            var artifactText = $"📁 {artifact.Description} | Size: {artifact.Size:N0} bytes | Modified: {artifact.Timestamp}";
                            _artifacts.Items.Add(artifactText);

                            if (_artifacts.Items.Count > 100)
                                _artifacts.Items.RemoveAt(0);
                        }
                    }

                    UpdateStatus($"✅ Recent files analysis completed: {artifacts.Count} artifacts found");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Recent files analysis failed: {ex.Message}");
                }
            }

            private async void OnAnalyzeNetworkConnections(object? sender, RoutedEventArgs e)
            {
                UpdateStatus("🔍 Analyzing network connections...");

                try
                {
                    var artifacts = await DigitalForensics.AnalyzeDigitalArtifactsAsync(ArtifactType.NetworkConnections);

                    if (_artifacts != null)
                    {
                        foreach (var artifact in artifacts)
                        {
                            var artifactText = $"🌐 {artifact.Description} | Location: {artifact.Location}";
                            _artifacts.Items.Add(artifactText);

                            if (_artifacts.Items.Count > 100)
                                _artifacts.Items.RemoveAt(0);
                        }
                    }

                    UpdateStatus($"✅ Network connections analysis completed: {artifacts.Count} artifacts found");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Network connections analysis failed: {ex.Message}");
                }
            }

            private async void OnGenerateTimeline(object? sender, RoutedEventArgs e)
            {
                var targetPath = "C:\\Users"; // Default path

                UpdateStatus($"📅 Generating forensic timeline for {targetPath}...");

                try
                {
                    var timeline = await DigitalForensics.CreateForensicTimelineAsync(targetPath);

                    if (_timeline != null)
                    {
                        _timeline.Items.Clear();
                        foreach (var entry in timeline.Take(100)) // Show first 100 entries
                        {
                            var timelineText = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} | {entry.EventType} | {entry.Description}";
                            _timeline.Items.Add(timelineText);
                        }
                    }

                    UpdateStatus($"✅ Forensic timeline generated: {timeline.Count} entries");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Forensic timeline generation failed: {ex.Message}");
                }
            }

            private void UpdateStatus(string message)
            {
                if (_statusText != null)
                {
                    _statusText.Text = message;
                }

                Logger.Log($"Digital Forensics: {message}");
            }
        }

        public IWidget? Widget => new DigitalForensicsWidget();

        public void Start()
        {
            Logger.Log("🔍 Digital Forensics Plugin started - Ready for evidence analysis!");

            // Initialize Digital Forensics service
            _ = DigitalForensics.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🛑 Digital Forensics Plugin stopped");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Widgets;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// 💻 PRODUCTION POWEROSSHELL TERMINAL - Advanced PowerShell Integration
    /// Real PowerShell execution, elevation, auto-detection, installation management
    /// </summary>
    public class PowerShellTerminalPlugin : IPlugin
    {
        public string Name => "PowerShell Terminal";
        public string Description => "Advanced PowerShell terminal with elevation and auto-detection";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;

        public class PowerShellTerminalWidget : IWidget
        {
            public string Name => "PowerShell Terminal";
            public string Description => "Advanced PowerShell terminal with elevation and auto-detection";

            private TextBox? _outputEditor;
            private TextBox? _commandInput;
            private StackPanel? _toolbar;
            private StackPanel? _statusBar;
            private List<string> _commandHistory;
            private int _commandHistoryIndex;
            private bool _isElevated;

            public PowerShellTerminalWidget()
            {
                _commandHistory = new List<string>();
                _commandHistoryIndex = 0;
                _isElevated = PowerShellManager.IsElevated;

                // Subscribe to elevation changes
                PowerShellManager.ElevationStatusChanged += OnElevationStatusChanged;
                PowerShellManager.StatusChanged += OnPowerShellStatusChanged;
            }

            public Control BuildView()
            {
                var mainPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(5)
                };

                // Toolbar with PowerShell controls
                _toolbar = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Background = Brushes.Orange,
                    Margin = new Thickness(0, 0, 0, 5),
                    Spacing = 10
                };

                var statusButton = new Button
                {
                    Content = _isElevated ? "🔒 ADMIN" : "👤 User",
                    Background = _isElevated ? Brushes.Red : Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(8, 4),
                    FontWeight = FontWeight.Bold
                };
                statusButton.Click += ToggleElevation;

                var installButton = new Button
                {
                    Content = "📥 Install PS",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(8, 4)
                };
                installButton.Click += InstallPowerShell;

                var policyButton = new Button
                {
                    Content = "📋 Policy",
                    Background = Brushes.Purple,
                    Foreground = Brushes.White,
                    Padding = new Thickness(8, 4)
                };
                policyButton.Click += ShowExecutionPolicy;

                var clearButton = new Button
                {
                    Content = "🗑️ Clear",
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    Padding = new Thickness(8, 4)
                };
                clearButton.Click += ClearOutput;

                _toolbar.Children.Add(statusButton);
                _toolbar.Children.Add(installButton);
                _toolbar.Children.Add(policyButton);
                _toolbar.Children.Add(clearButton);

                // Output area
                _outputEditor = new TextBox
                {
                    FontFamily = "Consolas",
                    FontSize = 14,
                    IsReadOnly = true,
                    Background = Brushes.Black,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 5),
                    Height = 400,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping = TextWrapping.Wrap,
                    Text = GetWelcomeMessage()
                };

                // Command input area
                var inputPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 5),
                    Spacing = 5
                };

                var psPrompt = new TextBlock
                {
                    Text = GetPrompt(),
                    FontFamily = "Consolas",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.Orange,
                    VerticalAlignment = VerticalAlignment.Center
                };

                _commandInput = new TextBox
                {
                    FontFamily = "Consolas",
                    FontSize = 14,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                _commandInput.KeyDown += OnKeyDown;

                inputPanel.Children.Add(psPrompt);
                inputPanel.Children.Add(_commandInput);

                // Status bar
                _statusBar = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Background = Brushes.DarkGray,
                    Margin = new Thickness(0, 5, 0, 0),
                    Height = 25
                };

                var statusText = new TextBlock
                {
                    Text = "PowerShell Terminal Ready",
                    Foreground = Brushes.LightGray,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0)
                };

                _statusBar.Children.Add(statusText);

                // Assemble main view
                mainPanel.Children.Add(_toolbar);
                mainPanel.Children.Add(_outputEditor);
                mainPanel.Children.Add(inputPanel);
                mainPanel.Children.Add(_statusBar);

                var border = new Border
                {
                    Child = mainPanel,
                    Background = Brushes.DarkSlateGray,
                    BorderBrush = Brushes.Orange,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(15)
                };

                return border;
            }

            private string GetWelcomeMessage()
            {
                return string.Join("\n", new[]
                {
                    "╔══════════════════════════════════════════════════════════════╗",
                    "║                💻 CYCLOSIDE POWERSHELL TERMINAL               ║",
                    "║                                                              ║",
                    "║  🚀 PRODUCTION POWEROSSHELL INTEGRATION                     ║",
                    "║  📡 AUTO-DETECTION • 🔒 ELEVATION • 📥 INSTALLATION         ║",
                    "║                                                              ║",
                    "║  Status:",
                    $"║    PowerShell Installed: {(PowerShellManager.IsPowerShellAvailable ? "✅ YES" : "❌ NO")}",
                    $"║    Version: {PowerShellManager.Version?.ToString() ?? "Not Available"}",
                    $"║    Elevation: {(_isElevated ? "🔒 ADMIN" : "👤 User Mode")}",
                    "║                                                              ║",
                    "║  COMMANDS:",
                    "║    • Type any PowerShell command                           ║",
                    "║    • Use ↑/↓ arrows for command history                    ║",
                    "║    • Click 🔒 for elevation (admin mode)                   ║",
                    "║    • Click 📥 to install/update PowerShell               ║",
                    "║    • Click 📋 to manage execution policy                   ║",
                    "║                                                              ║",
                    "║  Ready for advanced PowerShell operations!                   ║",
                    "╚══════════════════════════════════════════════════════════════╝",
                    "",
                    "=" + new string('=', 67),
                    ""
                });
            }

            private string GetPrompt()
            {
                if (_isElevated)
                {
                    return $"💻 PS ADMIN [{Environment.MachineName}]> ";
                }
                else
                {
                    return $"💻 PS [{Environment.UserName}@{Environment.MachineName}]> ";
                }
            }

            private async void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
            {
                switch (e.Key)
                {
                    case Avalonia.Input.Key.Enter:
                        {
                            var command = _commandInput?.Text?.Trim();
                            if (!string.IsNullOrEmpty(command))
                            {
                                await ExecutePowerShellCommand(command);
                                if (_commandInput != null)
                                    _commandInput.Text = "";
                                _commandHistoryIndex = _commandHistory.Count;
                            }
                            break;
                        }
                    case Avalonia.Input.Key.Up:
                        {
                            NavigateHistory(-1);
                            break;
                        }
                    case Avalonia.Input.Key.Down:
                        {
                            NavigateHistory(1);
                            break;
                        }
                    case Avalonia.Input.Key.Tab:
                        {
                            // Future: PowerShell tab completion
                            e.Handled = true;
                            break;
                        }
                }
            }

            private void NavigateHistory(int direction)
            {
                if (_commandHistory.Count == 0) return;

                _commandHistoryIndex = Math.Max(0, Math.Min(_commandHistory.Count - 1, _commandHistoryIndex + direction));

                if (_commandHistoryIndex >= 0 && _commandHistoryIndex < _commandHistory.Count && _commandInput != null)
                {
                    _commandInput.Text = _commandHistory[_commandHistoryIndex];
                }
            }

            private async Task ExecutePowerShellCommand(string command)
            {
                if (string.IsNullOrWhiteSpace(command)) return;

                // Add to history
                _commandHistory.Add(command);
                _commandHistoryIndex = _commandHistory.Count;

                // Show command
                AppendOutput($"💻 {GetPrompt()}{command}");
                UpdateStatus("⚡ Executing PowerShell command...");

                try
                {
                    // Check for special commands
                    if (command.StartsWith("exit", StringComparison.OrdinalIgnoreCase) ||
                        command.StartsWith("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendOutput("👋 Closing PowerShell Terminal...");
                        return;
                    }

                    // Execute command
                    var result = await PowerShellManager.ExecutePowerShellCommandAsync(command, false);

                    if (result != null)
                    {
                        AppendOutput(result);
                        UpdateStatus("✅ Command completed successfully");
                    }
                    else
                    {
                        AppendOutput("❌ Command failed or returned no output");
                        UpdateStatus("❌ Command execution failed");
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput($"💥 Error: {ex.Message}");
                    UpdateStatus($"❌ Execution error: {ex.Message}");
                    Logger.Log($"PowerShell execution error: {ex}");
                }
            }

            private void AppendOutput(string text)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_outputEditor != null)
                    {
                        var currentText = _outputEditor.Text;
                        _outputEditor.Text = currentText + "\n" + text;
                    }
                });
            }

            private void UpdateStatus(string message)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_statusBar?.Children.Count > 0 && _statusBar.Children[0] is TextBlock statusBlock)
                    {
                        statusBlock.Text = message;
                    }
                });
            }

            private async void ToggleElevation(object? sender, RoutedEventArgs e)
            {
                try
                {
                    UpdateStatus("🔒 Checking elevation status...");

                    if (_isElevated)
                    {
                        AppendOutput("ℹ️ Already running with admin privileges");
                        UpdateStatus("✅ Running as administrator");
                        return;
                    }

                    AppendOutput("🔒 Requesting elevation to admin privileges...");

                    var success = await PowerShellManager.ElevateToAdminAsync();

                    if (success)
                    {
                        AppendOutput("✅ Successfully elevated to admin privileges!");
                        UpdateStatus("🔒 Running as administrator");

                        // Update toolbar button
                        if (sender is Button button)
                        {
                            button.Content = "🔒 ADMIN";
                            button.Background = Brushes.Red;
                        }

                        // Update prompt
                        AppendOutput(GetPrompt());
                    }
                    else
                    {
                        AppendOutput("❌ Failed to elevate - Administrator access denied");
                        UpdateStatus("❌ Elevation failed");
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput($"💥 Elevation error: {ex.Message}");
                    UpdateStatus($"❌ Elevation error: {ex.Message}");
                    Logger.Log($"Elevation error: {ex}");
                }
            }

            private async void InstallPowerShell(object? sender, RoutedEventArgs e)
            {
                try
                {
                    AppendOutput("📥 Initiating PowerShell installation...");
                    UpdateStatus("📥 Checking PowerShell installation...");

                    if (PowerShellManager.IsPowerShellAvailable)
                    {
                        AppendOutput($"✅ PowerShell already installed: v{PowerShellManager.Version}");
                        UpdateStatus("✅ PowerShell is available");
                        return;
                    }

                    AppendOutput("⚠️ PowerShell not detected - Installer starting...");

                    var success = await PowerShellManager.InstallPowerShellAsync();

                    if (success)
                    {
                        AppendOutput("🎉 PowerShell installation completed successfully!");
                        UpdateStatus("✅ PowerShell installed and ready");

                        // Refresh welcome message
                        if (_outputEditor != null)
                            _outputEditor.Text = GetWelcomeMessage();

                        // Update button state
                        if (sender is Button button)
                        {
                            button.Content = "✅ Installed";
                            button.Background = Brushes.Green;
                        }
                    }
                    else
                    {
                        AppendOutput("❌ PowerShell installation failed");
                        AppendOutput("💡 Try downloading manually from: https://docs.microsoft.com/en-us/powershell/");
                        UpdateStatus("❌ Installation failed - Manual install required");
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput($"💥 Installation error: {ex.Message}");
                    UpdateStatus($"❌ Installation error: {ex.Message}");
                    Logger.Log($"PowerShell installation error: {ex}");
                }
            }

            private async void ShowExecutionPolicy(object? sender, RoutedEventArgs e)
            {
                try
                {
                    AppendOutput("📋 Checking PowerShell execution policy...");
                    UpdateStatus("📋 Analyzing execution policy...");

                    var policy = await PowerShellManager.GetExecutionPolicyAsync();

                    if (!string.IsNullOrEmpty(policy))
                    {
                        AppendOutput($"📋 Current execution policy: {policy}");

                        if (policy.Contains("Restricted", StringComparison.OrdinalIgnoreCase))
                        {
                            AppendOutput("⚠️ Policy is Restricted - Scripts may not run");
                            AppendOutput("💡 Consider: Set-ExecutionPolicy RemoteSigned");
                            AppendOutput("   (Requires admin elevation)");
                        }
                        else
                        {
                            AppendOutput("✅ Execution policy allows script execution");
                        }

                        UpdateStatus($"📋 Policy: {policy}");
                    }
                    else
                    {
                        AppendOutput("❌ Could not determine execution policy");
                        UpdateStatus("❌ Policy check failed");
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput($"💥 Policy check error: {ex.Message}");
                    UpdateStatus($"❌ Policy error: {ex.Message}");
                    Logger.Log($"Execution policy error: {ex}");
                }
            }

            private void ClearOutput(object? sender, RoutedEventArgs e)
            {
                if (_outputEditor != null)
                    _outputEditor.Text = GetWelcomeMessage();
                UpdateStatus("🗑️ Output cleared");
            }

            private void OnElevationStatusChanged(object? sender, bool isAdmin)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _isElevated = isAdmin;

                    // Update toolbar button
                    if (_toolbar?.Children.Count > 0 && _toolbar.Children[0] is Button statusButton)
                    {
                        statusButton.Content = _isElevated ? "🔒 ADMIN" : "👤 User";
                        statusButton.Background = _isElevated ? Brushes.Red : Brushes.Green;
                    }

                    AppendOutput($"🔒 Elevation status: {(_isElevated ? "ADMIN" : "User Mode")}");
                });
            }

            private void OnPowerShellStatusChanged(object? sender, string status)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AppendOutput($"📡 PowerShell Manager: {status}");
                });
            }
        }

        public IWidget? Widget => new PowerShellTerminalWidget();

        public void Start()
        {
            Logger.Log("🚀 PowerShell Terminal Plugin started - Production PowerShell integration active");

            // Initialize PowerShell Manager
            _ = PowerShellManager.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🛑 PowerShell Terminal Plugin stopped");
        }
    }
}
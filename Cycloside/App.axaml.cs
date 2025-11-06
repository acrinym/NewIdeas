using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cycloside.Models;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Services;
using Cycloside.ViewModels;
using Cycloside.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ServicesRelayCommand = Cycloside.Services.RelayCommand;

namespace Cycloside;

public partial class App : Application
{
    private const string TrayIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAGElEQVR4nGNkaGAgCTCRpnxUw6iGoaQBALsfAKDg6Y6zAAAAAElFTkSuQmCC";
    private RemoteApiServer? _remoteServer;
    private PluginManager? _pluginManager;
    private TrayIcon? _trayIcon; // Keep a reference to the tray icon
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _mainViewModel;
    private bool _pluginsLoadedSelectively = false;

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (NullReferenceException nre)
        {
            // Guard against instacrash if XAML triggers a null unboxing during app-level resource load
            Services.Logger.Error($"💥 App.Initialize XAML load NullReference handled: {nre.Message}");
        }
        catch (Exception ex)
        {
            Services.Logger.Error($"💥 App.Initialize XAML load error: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize configuration system
    /// </summary>
    private async Task InitializeConfigurationAsync()
    {
        try
        {
            await ConfigurationManager.InitializeAsync();
            Logger.Log("✅ Configuration Manager initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Configuration Manager initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize theme system
    /// </summary>
    private async Task InitializeThemesAsync()
    {
        try
        {
            // Theme initialization is synchronous, but we can preload theme resources
            await Task.Run(() => ThemeManager.PreloadThemeResources());
            Logger.Log("✅ Theme system initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Theme system initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize services and plugins
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        try
        {
            // Load plugins selectively based on configuration
            await LoadConfiguredPlugins();
            Logger.Log("✅ Services and plugins initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Services initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Show startup progress window
    /// </summary>
    private Window? ShowStartupProgress()
    {
        try
        {
            var progressWindow = new Window
            {
                Title = "Starting Cycloside...",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                Background = Avalonia.Media.Brushes.Black
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

            var title = new TextBlock
            {
                Text = "🚀 Initializing Cycloside Cybersecurity Platform",
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 16,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var status = new TextBlock
            {
                Text = "Loading core systems...",
                Foreground = Avalonia.Media.Brushes.Cyan,
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };

            var progressBar = new ProgressBar
            {
                Width = 300,
                Height = 4,
                Margin = new Thickness(0, 20, 0, 0),
                IsIndeterminate = true
            };

            panel.Children.Add(title);
            panel.Children.Add(status);
            panel.Children.Add(progressBar);

            progressWindow.Content = panel;
            progressWindow.Show();

            return progressWindow;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to show startup progress: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clean up resources and memory
    /// </summary>
    private void CleanupResources()
    {
        try
        {
            Logger.Log("🧹 Performing resource cleanup...");

            // Clear theme caches to free memory
            ThemeManager.ClearThemeCache();

            // Clear any temporary files or caches
            ClearTempFiles();

            // Force garbage collection for better memory usage
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            Logger.Log("✅ Resource cleanup completed");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Resource cleanup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Start periodic cleanup for memory optimization
    /// </summary>
    private Task StartPeriodicCleanup()
    {
        try
        {
            // Run cleanup every 30 minutes
            var timer = new System.Threading.Timer(_ =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    CleanupResources();
                }
            }, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

            Logger.Log("⏰ Periodic cleanup scheduled every 30 minutes");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to start periodic cleanup: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Clear temporary files and caches
    /// </summary>
    private void ClearTempFiles()
    {
        try
        {
            var tempDir = Path.GetTempPath();
            var cyclosideTemp = Path.Combine(tempDir, "Cycloside");

            if (Directory.Exists(cyclosideTemp))
            {
                var tempFiles = Directory.GetFiles(cyclosideTemp, "*", SearchOption.AllDirectories);
                foreach (var file in tempFiles)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"⚠️ Failed to delete temp file {file}: {ex.Message}");
                    }
                }

                Logger.Log($"🗑️ Cleared {tempFiles.Length} temporary files");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to clear temp files: {ex.Message}");
        }
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        // Prevent instant exit when no window is open (e.g., tray-only start)
        try
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Logger.Log("🛡️ ShutdownMode set to OnExplicitShutdown to keep app alive without windows");
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to set ShutdownMode: {ex.Message}");
        }

        // Hook UI dispatcher exception events to capture and log crash details
        try
        {
            Avalonia.Threading.Dispatcher.UIThread.UnhandledExceptionFilter += OnDispatcherUnhandledExceptionFilter;
            Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to register UI exception handlers: {ex.Message}");
        }

        var settings = SettingsManager.Settings;
        ThemeManager.InitializeFromSettings();

        // Optimize startup with parallel initialization
        var initializationTasks = new List<Task>
        {
            InitializeConfigurationAsync(),
            InitializeThemesAsync(),
            InitializeServicesAsync()
        };

        // Show loading progress while initializing
        var progressWindow = ShowStartupProgress();

        try
        {
            // Wait for all core systems to initialize in parallel
            await Task.WhenAll(initializationTasks);
            Logger.Log("✅ All core systems initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Core system initialization failed: {ex.Message}");
        }
        finally
        {
            progressWindow?.Close();
        }

        // Check if first-launch setup is needed
        bool needsFirstLaunchSetup = settings.StartupConfig == null || !settings.StartupConfig.HasCompletedFirstLaunch;

        if (needsFirstLaunchSetup)
        {
            // First launch - show startup configuration wizard
            Logger.Log("🎯 First launch detected - showing startup configuration wizard...");

            try
            {
                // Create plugin manager early for the wizard
                _pluginManager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify);
                LoadAllPluginsForInspection(_pluginManager, settings);

                var startupWindow = new StartupConfigurationWindow(_pluginManager, config =>
                {
                    // Save the configuration
                    settings.StartupConfig = config;
                    settings.FirstRun = false;
                    SettingsManager.Save();

                    Logger.Log("✅ Startup configuration saved - creating main window...");

                    // Create and show main window
                    try
                    {
                        _mainWindow = CreateMainWindowWithConfig(settings);
                        desktop.MainWindow = _mainWindow;
                        _mainWindow.Show();
                        Logger.Log("🚀 Main window shown with configured plugins!");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"❌ Failed to create main window after configuration: {ex.Message}");
                        CreateEmergencyMainWindow(desktop);
                    }
                });

                desktop.MainWindow = startupWindow;
                startupWindow.Show();
                Logger.Log("✅ Startup configuration wizard displayed");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to show startup configuration wizard: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");

                // Fallback to creating main window with defaults
                try
                {
                    _mainWindow = CreateMainWindow(settings);
                    desktop.MainWindow = _mainWindow;
                    _mainWindow.Show();
                }
                catch (Exception fallbackEx)
                {
                    Logger.Log($"💥 Fallback also failed: {fallbackEx.Message}");
                    CreateEmergencyMainWindow(desktop);
                }
            }
        }
        else
        {
            // Normal launch with saved configuration
            Logger.Log("🚀 Loading saved startup configuration...");

            try
            {
                _mainWindow = CreateMainWindowWithConfig(settings);
                desktop.MainWindow = _mainWindow;
                _mainWindow.Show();
                Logger.Log("✅ Main window shown with saved configuration!");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Main window creation failed: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");
                try
                {
                    Logger.Log("🔄 Attempting emergency main window creation...");
                    _mainWindow = new MainWindow();
                    desktop.MainWindow = _mainWindow;
                    _mainWindow.Show();
                    Logger.Log("✅ Emergency main window created");
                }
                catch (Exception emergencyEx)
                {
                    Logger.Log($"💥 Emergency fallback also failed: {emergencyEx.Message}");
                    Logger.Log("🚨 Cannot create main window - exiting application");
                    Environment.Exit(1);
                }
            }
        }

        // Final safeguard: ensure a desktop window is present to keep lifetime active
        if (desktop.MainWindow == null)
        {
            try
            {
                Logger.Log("🛡️ No MainWindow detected after initialization, creating fallback window");
                _mainWindow = new MainWindow();
                desktop.MainWindow = _mainWindow;
                _mainWindow.Show();
                Logger.Log("✅ Fallback main window created and shown");
            }
            catch (Exception ex)
            {
                Logger.Log($"💥 Fallback creation failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        desktop.Exit += (_, _) =>
        {
            SaveSessionState();
            CleanupResources();
            // Ensure logs are flushed at shutdown
            Services.Logger.Shutdown();
        };

        // Setup periodic cleanup for memory optimization
        _ = StartPeriodicCleanup();

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDispatcherUnhandledExceptionFilter(object? sender, Avalonia.Threading.DispatcherUnhandledExceptionFilterEventArgs args)
    {
        try
        {
            var ex = args.Exception;
            Logger.Log($"🚨 UI first-chance exception: {ex.Message}");
            Logger.Log($"Stack trace: {ex.StackTrace}");
            // Request catching so UnhandledException event can log details without crashing immediately
            args.RequestCatch = true;
        }
        catch (Exception logEx)
        {
            Logger.Log($"⚠️ Failed logging UI first-chance exception: {logEx.Message}");
        }
    }

    private void OnDispatcherUnhandledException(object? sender, Avalonia.Threading.DispatcherUnhandledExceptionEventArgs args)
    {
        try
        {
            var ex = args.Exception;
            Logger.Log($"💥 UI unhandled exception: {ex}");

            // Prevent instacrash when Avalonia/internal binding unboxes null to value type.
            // Handle only the known NullReference at CastHelpers.Unbox to keep app stable.
            try
            {
                var target = ex.TargetSite;
                var declaring = target?.DeclaringType?.FullName ?? string.Empty;
                var name = target?.Name ?? string.Empty;
                var isKnownNullRef = ex is NullReferenceException &&
                                     (declaring.Contains("System.Runtime.CompilerServices.CastHelpers") || name.Contains("Unbox") ||
                                      (ex.StackTrace?.Contains("CastHelpers.Unbox") ?? false));

                if (isKnownNullRef)
                {
                    Logger.Log("🛡️ Handled known UI NullReference from CastHelpers.Unbox to prevent crash");
                    args.Handled = true;
                }
            }
            catch (Exception guardEx)
            {
                Logger.Log($"⚠️ Failed to evaluate UI exception for handling: {guardEx.Message}");
            }
        }
        catch (Exception logEx)
        {
            Logger.Log($"⚠️ Failed logging UI unhandled exception: {logEx.Message}");
        }
    }

    private void CreateEmergencyMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            Logger.Log("🚨 Creating emergency main window...");
            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;
            _mainWindow.Show();
            Logger.Log("✅ Emergency main window created");
        }
        catch (Exception emergencyEx)
        {
            Logger.Log($"💥 Emergency fallback also failed: {emergencyEx.Message}");
            Logger.Log("🚨 Cannot create main window - exiting application");
            Environment.Exit(1);
        }
    }

    private MainWindow CreateMainWindowWithConfig(AppSettings settings)
    {
        if (_pluginManager == null)
        {
            _pluginManager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify);
        }

        // Subscribe to plugin reloads to update the UI when plugins are refreshed.
        _pluginManager.PluginsReloaded += OnPluginsReloaded;

        var volatileManager = new VolatilePluginManager();

        // Load plugins based on saved startup configuration
        LoadPluginsFromConfiguration(_pluginManager, settings);
        _pluginManager.StartWatching();

        var viewModel = new MainWindowViewModel(_pluginManager.Plugins);
        _mainViewModel = viewModel;
        var mainWindow = new MainWindow(_pluginManager)
        {
            DataContext = viewModel
        };
        _mainWindow = mainWindow;

        viewModel.ExitCommand = new ServicesRelayCommand(() => Shutdown());

        // Setup plugin commands
        SetupPluginCommands(viewModel);

        return mainWindow;
    }

    private MainWindow CreateMainWindow(AppSettings settings)
    {
        _pluginManager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify);

        // Subscribe to plugin reloads to update the UI when plugins are refreshed.
        _pluginManager.PluginsReloaded += OnPluginsReloaded;

        var volatileManager = new VolatilePluginManager();

        LoadAllPlugins(_pluginManager, settings);
        _pluginManager.StartWatching();

        var viewModel = new MainWindowViewModel(_pluginManager.Plugins);
        _mainViewModel = viewModel;
        var mainWindow = new MainWindow(_pluginManager)
        {
            DataContext = viewModel
        };
        _mainWindow = mainWindow;


        viewModel.ExitCommand = new ServicesRelayCommand(() => Shutdown());

        // Toggle plugin enablement from the main window.
        viewModel.StartPluginCommand = new ServicesRelayCommand(pluginObj =>
        {
            if (pluginObj is not IPlugin plugin || _pluginManager is null) return;

            if (plugin is IWorkspaceItem workspace)
            {
                var existing = viewModel.WorkspaceItems.FirstOrDefault(w => w.Plugin == plugin);
                bool enable = existing is null;
                if (enable)
                {
                    workspace.UseWorkspace = true;
                    _pluginManager.EnablePlugin(plugin);
                    var view = workspace.BuildWorkspaceView();
                    var vm = new WorkspaceItemViewModel(plugin.Name, view, plugin, DetachWorkspaceItem);
                    viewModel.WorkspaceItems.Add(vm);
                    viewModel.SelectedWorkspaceItem = vm;
                }
                else
                {
                    workspace.UseWorkspace = false;
                    _pluginManager.DisablePlugin(plugin);
                    viewModel.WorkspaceItems.Remove(existing!);
                }
                SettingsManager.Settings.PluginEnabled[plugin.Name] = enable;
                SettingsManager.Save();
                WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, enable);
            }
            else
            {
                bool shouldBeEnabled = !_pluginManager.IsEnabled(plugin);
                if (shouldBeEnabled)
                {
                    _pluginManager.EnablePlugin(plugin);
                }
                else
                {
                    _pluginManager.DisablePlugin(plugin);
                }

                SettingsManager.Settings.PluginEnabled[plugin.Name] = shouldBeEnabled;
                SettingsManager.Save();
                WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, shouldBeEnabled);
            }
        });

        // Start or stop a plugin in its own window, even if it supports the workspace.
        viewModel.StartPluginWindowCommand = new ServicesRelayCommand(pluginObj =>
        {
            if (pluginObj is not IPlugin plugin || _pluginManager is null) return;

            var existing = viewModel.WorkspaceItems.FirstOrDefault(w => w.Plugin == plugin);
            bool shouldBeEnabled = !_pluginManager.IsEnabled(plugin);

            if (shouldBeEnabled)
            {
                if (plugin is IWorkspaceItem ws) ws.UseWorkspace = false;
                if (existing != null) viewModel.WorkspaceItems.Remove(existing);
                _pluginManager.EnablePlugin(plugin);
            }
            else
            {
                if (plugin is IWorkspaceItem ws) ws.UseWorkspace = false;
                _pluginManager.DisablePlugin(plugin);
                if (existing != null) viewModel.WorkspaceItems.Remove(existing);
            }

            SettingsManager.Settings.PluginEnabled[plugin.Name] = shouldBeEnabled;
            SettingsManager.Save();
            WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, shouldBeEnabled);
        });

        _remoteServer = new RemoteApiServer(_pluginManager, settings.RemoteApiToken);
        _remoteServer.Start();
        WorkspaceProfiles.Apply(settings.ActiveProfile, _pluginManager);
        RestoreSessionState(viewModel);
        RegisterHotkeys(_pluginManager);

        _trayIcon = new TrayIcon
        {
            Icon = CreateTrayIcon(),
            ToolTipText = "Cycloside",
            Menu = BuildTrayMenu(_pluginManager, volatileManager, settings)
        };
        var icons = TrayIcon.GetIcons(this) ?? new TrayIcons();
        TrayIcon.SetIcons(this, icons);
        if (!icons.Contains(_trayIcon))
        {
            icons.Add(_trayIcon);
        }
        _trayIcon.IsVisible = true;

        // Post-startup: try upgrading the tray icon from a fast placeholder to a system icon on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        var icon = ExtractIconFromDll(Path.Combine(systemDir, "imageres.dll"), 25) ??
                                   ExtractIconFromDll(Path.Combine(systemDir, "shell32.dll"), 20) ??
                                   ExtractIconFromDll(Path.Combine(systemDir, "shell32.dll"), 8);
                        if (icon != null)
                        {
                            using var stream = new MemoryStream();
#pragma warning disable CA1416
                            icon.Save(stream);
#pragma warning restore CA1416
                            stream.Position = 0;
                            var winIcon = new WindowIcon(stream);
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (_trayIcon != null)
                                    _trayIcon.Icon = winIcon;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Tray icon upgrade failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex) { Logger.Log($"Tray icon upgrade scheduling failed: {ex.Message}"); }
        }

        return mainWindow;
    }

    // Handle the PluginsReloaded event to refresh menus and view models.
    private void OnPluginsReloaded()
    {
        if (_trayIcon is null || _pluginManager is null) return;

        // Rebuild the tray menu with the new plugin instances.
        var volatileManager = new VolatilePluginManager();
        _trayIcon.Menu = BuildTrayMenu(_pluginManager, volatileManager, SettingsManager.Settings);

        // Also update the main window's view model.
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainWindowViewModel vm)
        {
            vm.AvailablePlugins.Clear();
            foreach (var plugin in _pluginManager.Plugins)
            {
                vm.AvailablePlugins.Add(plugin);
            }
        }
    }

    /// <summary>
    /// Load plugins selectively based on configuration
    /// </summary>
    private async Task LoadConfiguredPlugins()
    {
        if (_pluginsLoadedSelectively) return;

        try
        {
            // Wait for Configuration Manager to be ready
            await Task.Delay(200); // Brief delay to ensure ConfigurationManager is initialized

            Logger.Log("🔧 Loading plugins selectively based on configuration...");

            // Enable selective loading based on configuration
            _pluginsLoadedSelectively = true;

            Logger.Log($"✅ Selective plugin loading enabled - {ConfigurationManager.CurrentConfig.Plugins.Count} plugins configured");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Selective plugin loading setup failed: {ex.Message}");
            Logger.Log($"Stack trace: {ex.StackTrace}");
            _pluginsLoadedSelectively = true; // Continue anyway with default behavior
        }
    }

    /// <summary>
    /// Loads all plugins without starting them - for inspection by the startup wizard
    /// </summary>
    private void LoadAllPluginsForInspection(PluginManager manager, AppSettings settings)
    {
        void TryAdd(Func<IPlugin> factory)
        {
            try
            {
                manager.AddBuiltInPlugin(factory);
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Plugin factory threw during inspection: {ex.Message}");
            }
        }

        // Add ALL plugins for inspection (don't start them yet)
        TryAdd(() => new DateTimeOverlayPlugin());
        TryAdd(() => new MP3PlayerPlugin());
        TryAdd(() => new ManagedVisHostPlugin());
        TryAdd(() => new MacroPlugin());
        TryAdd(() => new TextEditorPlugin());
        TryAdd(() => new WallpaperPlugin());
        TryAdd(() => new ClipboardManagerPlugin());
        TryAdd(() => new HackersParadisePlugin());
        TryAdd(() => new HackerTerminalPlugin());
        TryAdd(() => new PowerShellTerminalPlugin());
        TryAdd(() => new PluginMarketplacePlugin());
        TryAdd(() => new AdvancedCodeEditorPlugin());
        TryAdd(() => new NetworkToolsPlugin());
        TryAdd(() => new HardwareMonitorPlugin());
        TryAdd(() => new VulnerabilityScannerPlugin());
        TryAdd(() => new ExploitDevToolsPlugin());
        TryAdd(() => new ExploitDatabasePlugin());
        TryAdd(() => new DigitalForensicsPlugin());
        TryAdd(() => new DatabaseManagerPlugin());
        TryAdd(() => new ApiTestingPlugin());
        TryAdd(() => new CharacterMapPlugin());
        TryAdd(() => new FileWatcherPlugin());
        TryAdd(() => new TaskSchedulerPlugin());
        TryAdd(() => new DiskUsagePlugin());
        TryAdd(() => new TerminalPlugin());
        TryAdd(() => new LogViewerPlugin());
        TryAdd(() => new NotificationCenterPlugin());
        TryAdd(() => new EnvironmentEditorPlugin());
        TryAdd(() => new FileExplorerPlugin());
        TryAdd(() => new QuickLauncherPlugin());
        TryAdd(() => new JezzballPlugin());
        TryAdd(() => new QBasicRetroIDEPlugin());
        TryAdd(() => new ScreenSaverPlugin());
        TryAdd(() => new WidgetHostPlugin(manager));
        TryAdd(() => new EncryptionPlugin());
        TryAdd(() => new ModTrackerPlugin());
        TryAdd(() => new AiAssistantPlugin());

        Logger.Log($"✅ Loaded {manager.Plugins.Count()} plugins for inspection");
    }

    /// <summary>
    /// Loads plugins based on saved startup configuration
    /// </summary>
    private void LoadPluginsFromConfiguration(PluginManager manager, AppSettings settings)
    {
        var config = settings.StartupConfig;
        if (config == null)
        {
            Logger.Log("⚠️ No startup configuration found - loading defaults");
            LoadAllPlugins(manager, settings);
            return;
        }

        void TryAdd(Func<IPlugin> factory)
        {
            IPlugin? plugin = null;
            try
            {
                plugin = factory();
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Plugin factory threw during inspection: {ex.Message}");
                return;
            }

            // Check if plugin is enabled in configuration
            bool isEnabled = config.IsPluginEnabled(plugin.Name);

            if (isEnabled)
            {
                try
                {
                    manager.AddBuiltInPlugin(factory);
                    Logger.Log($"✅ Loading enabled plugin: {plugin.Name}");

                    // TODO: Apply window position from config.GetPluginPosition(plugin.Name)
                }
                catch (Exception addEx)
                {
                    Logger.Log($"❌ Failed to add plugin {plugin.Name}: {addEx.Message}");
                }
            }
            else
            {
                Logger.Log($"⏭️ Skipping disabled plugin: {plugin.Name}");
            }
        }

        // Try to add all plugins (configuration determines which load)
        TryAdd(() => new DateTimeOverlayPlugin());
        TryAdd(() => new MP3PlayerPlugin());
        TryAdd(() => new ManagedVisHostPlugin());
        TryAdd(() => new MacroPlugin());
        TryAdd(() => new TextEditorPlugin());
        TryAdd(() => new WallpaperPlugin());
        TryAdd(() => new ClipboardManagerPlugin());
        TryAdd(() => new HackersParadisePlugin());
        TryAdd(() => new HackerTerminalPlugin());
        TryAdd(() => new PowerShellTerminalPlugin());
        TryAdd(() => new PluginMarketplacePlugin());
        TryAdd(() => new AdvancedCodeEditorPlugin());
        TryAdd(() => new NetworkToolsPlugin());
        TryAdd(() => new HardwareMonitorPlugin());
        TryAdd(() => new VulnerabilityScannerPlugin());
        TryAdd(() => new ExploitDevToolsPlugin());
        TryAdd(() => new ExploitDatabasePlugin());
        TryAdd(() => new DigitalForensicsPlugin());
        TryAdd(() => new DatabaseManagerPlugin());
        TryAdd(() => new ApiTestingPlugin());
        TryAdd(() => new CharacterMapPlugin());
        TryAdd(() => new FileWatcherPlugin());
        TryAdd(() => new TaskSchedulerPlugin());
        TryAdd(() => new DiskUsagePlugin());
        TryAdd(() => new TerminalPlugin());
        TryAdd(() => new LogViewerPlugin());
        TryAdd(() => new NotificationCenterPlugin());
        TryAdd(() => new EnvironmentEditorPlugin());
        TryAdd(() => new FileExplorerPlugin());
        TryAdd(() => new QuickLauncherPlugin());
        TryAdd(() => new JezzballPlugin());
        TryAdd(() => new QBasicRetroIDEPlugin());
        TryAdd(() => new ScreenSaverPlugin());
        TryAdd(() => new WidgetHostPlugin(manager));
        TryAdd(() => new EncryptionPlugin());
        TryAdd(() => new ModTrackerPlugin());
        TryAdd(() => new AiAssistantPlugin());

        Logger.Log($"✅ Loaded {manager.Plugins.Count()} plugins from configuration");
    }

    /// <summary>
    /// Extracts plugin command setup logic for reuse
    /// </summary>
    private void SetupPluginCommands(MainWindowViewModel viewModel)
    {
        viewModel.ExitCommand = new ServicesRelayCommand(() => Shutdown());

        // Toggle plugin enablement from the main window - handled in MainWindowViewModel already
        // Just kept as placeholder for future enhancements
    }

    private void LoadAllPlugins(PluginManager manager, AppSettings settings)
    {
        void TryAdd(Func<IPlugin> factory)
        {
            IPlugin? plugin = null;
            try
            {
                plugin = factory();
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Plugin factory threw during inspection: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");
                return; // Skip broken plugin to keep app running
            }

            // Check if plugin should be loaded based on configuration
            bool shouldLoad = false;

            // First check if selective loading is enabled and plugin is configured
            if (_pluginsLoadedSelectively)
            {
                shouldLoad = ConfigurationManager.ShouldLoadPluginOnStartup(plugin.Name);
                Logger.Log($"🔌 Plugin {plugin.Name}: {(shouldLoad ? "Enabled" : "Disabled")} by configuration");
            }
            else
            {
                // Fallback to legacy settings (for compatibility)
                shouldLoad = !settings.DisableBuiltInPlugins || settings.SafeBuiltInPlugins.GetValueOrDefault(plugin.Name, false);
            }

            if (shouldLoad)
            {
                try
                {
                    manager.AddBuiltInPlugin(factory);
                    Logger.Log($"✅ Loading plugin: {plugin.Name}");
                }
                catch (Exception addEx)
                {
                    Logger.Log($"❌ Failed to add plugin {plugin.Name}: {addEx.Message}");
                }
            }
            else
            {
                Logger.Log($"⏭️ Skipping plugin: {plugin.Name}");
            }
        }

        TryAdd(() => new DateTimeOverlayPlugin());
        TryAdd(() => new MP3PlayerPlugin());
        TryAdd(() => new ManagedVisHostPlugin());
        TryAdd(() => new MacroPlugin());
        TryAdd(() => new TextEditorPlugin());
        TryAdd(() => new WallpaperPlugin());
        TryAdd(() => new ClipboardManagerPlugin());
        TryAdd(() => new HackersParadisePlugin());
        TryAdd(() => new HackerTerminalPlugin());
        TryAdd(() => new PowerShellTerminalPlugin());
        TryAdd(() => new PluginMarketplacePlugin());
        TryAdd(() => new AdvancedCodeEditorPlugin());
        TryAdd(() => new NetworkToolsPlugin());
        TryAdd(() => new HardwareMonitorPlugin());
        TryAdd(() => new VulnerabilityScannerPlugin());
        TryAdd(() => new ExploitDevToolsPlugin());
        TryAdd(() => new ExploitDatabasePlugin());
        TryAdd(() => new DigitalForensicsPlugin());
        TryAdd(() => new DatabaseManagerPlugin());
        TryAdd(() => new ApiTestingPlugin());
        TryAdd(() => new CharacterMapPlugin());
        TryAdd(() => new FileWatcherPlugin());
        TryAdd(() => new TaskSchedulerPlugin());
        TryAdd(() => new DiskUsagePlugin());
        TryAdd(() => new TerminalPlugin());
        TryAdd(() => new LogViewerPlugin());
        TryAdd(() => new NotificationCenterPlugin());
        TryAdd(() => new EnvironmentEditorPlugin());
        TryAdd(() => new JezzballPlugin());
        TryAdd(() => new QuickLauncherPlugin(manager));
        TryAdd(() => new WidgetHostPlugin(manager));
        // Switched from legacy Winamp-based visual host to the fully managed visualizer host.
        // The managed host renders with Avalonia, avoids native DLLs, and integrates directly
        // with our AudioData bus. This removes the dependency on vis_avs.dll and related C++ shims.
        TryAdd(() => new ManagedVisHostPlugin());
        TryAdd(() => new QBasicRetroIDEPlugin());
        // TryAdd(() => new ScreenSaverPlugin()); // Disabled for stability
    }

    private void RegisterHotkeys(PluginManager manager)
    {
        foreach (var kv in SettingsManager.Settings.Hotkeys)
        {
            KeyGesture gesture;
            try { gesture = KeyGesture.Parse(kv.Value); }
            catch { continue; }

            // Look for a plugin whose name matches the hotkey key (ignoring spaces)
            var plugin = manager.Plugins.FirstOrDefault(p =>
                string.Equals(p.Name.Replace(" ", string.Empty), kv.Key,
                    StringComparison.OrdinalIgnoreCase));

            if (plugin != null)
            {
                HotkeyManager.Register(gesture, () =>
                {
                    if (manager.IsEnabled(plugin)) manager.DisablePlugin(plugin);
                    else manager.EnablePlugin(plugin);
                });
            }
            // Additional actions can be handled here in the future
        }
    }

    private void Shutdown()
    {
        SaveSessionState();
        _pluginManager?.StopAll();
        _remoteServer?.Stop();
        HotkeyManager.UnregisterAll();
        Logger.Shutdown();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime appLifetime)
        {
            appLifetime.Shutdown();
        }
    }

    private void DetachWorkspaceItem(WorkspaceItemViewModel item)
    {
        if (_mainViewModel == null) return;
        if (item.Plugin is IWorkspaceItem workspace)
        {
            workspace.UseWorkspace = false;
            item.Plugin.Start();
        }
        _mainViewModel.WorkspaceItems.Remove(item);
    }

    private void SaveSessionState()
    {
        if (_mainViewModel == null) return;
        try
        {
            var names = _mainViewModel.WorkspaceItems.Select(w => w.Plugin.Name).ToList();
            WorkspaceProfiles.UpdateWorkspaceTabs(SettingsManager.Settings.ActiveProfile, names);
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to save workspace layout: {ex.Message}");
        }
    }

    private void RestoreSessionState(MainWindowViewModel vm)
    {
        if (_pluginManager == null) return;
        try
        {
            var tabs = WorkspaceProfiles.GetWorkspaceTabs(SettingsManager.Settings.ActiveProfile);
            foreach (var name in tabs)
            {
                var plugin = _pluginManager.Plugins.FirstOrDefault(p => p.Name == name);
                if (plugin is IWorkspaceItem ws)
                {
                    ws.UseWorkspace = true;
                    _pluginManager.EnablePlugin(plugin);
                    var view = ws.BuildWorkspaceView();
                    var vmItem = new WorkspaceItemViewModel(plugin.Name, view, plugin, DetachWorkspaceItem);
                    vm.WorkspaceItems.Add(vmItem);
                }
                else if (plugin != null)
                {
                    _pluginManager.EnablePlugin(plugin);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to restore workspace layout: {ex.Message}");
        }
    }

    #region Tray Menu and Icon Logic

    private NativeMenu BuildTrayMenu(PluginManager manager, VolatilePluginManager volatileManager, AppSettings settings)
    {
        var pluginsMenu = new NativeMenuItem("Plugins") { Menu = new NativeMenu() };
        var newPlugins = manager.Plugins.Where(p => manager.GetStatus(p) != PluginChangeStatus.None).ToList();
        var otherPlugins = manager.Plugins.Except(newPlugins).OrderBy(p => p.Name).ToList();

        if (newPlugins.Any())
        {
            var newMenu = new NativeMenuItem("New/Updated") { Menu = new NativeMenu() };
            foreach (var p in newPlugins) newMenu.Menu!.Items.Add(BuildPluginMenuItem(p, manager, settings));
            pluginsMenu.Menu!.Items.Add(newMenu);
            pluginsMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
        }

        foreach (var p in otherPlugins) pluginsMenu.Menu!.Items.Add(BuildPluginMenuItem(p, manager, settings));

        var volatileMenu = new NativeMenuItem("Volatile") { Menu = new NativeMenu() };
        volatileMenu.Menu!.Items.Add(BuildVolatileScriptMenuItem("Run Lua Script...", new FilePickerFileType("Lua Script") { Patterns = new[] { "*.lua" } }, volatileManager.RunLua));
        volatileMenu.Menu!.Items.Add(BuildVolatileScriptMenuItem("Run C# Script...", new FilePickerFileType("C# Script") { Patterns = new[] { "*.csx" } }, volatileManager.RunCSharp));
        volatileMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
        var inlineItem = new NativeMenuItem("Run Inline...");
        inlineItem.Click += (_, _) => new VolatileRunnerWindow(volatileManager).Show();
        volatileMenu.Menu!.Items.Add(inlineItem);

        // **NEW: A dedicated menu for log actions**
        var logsMenu = new NativeMenuItem("Logs") { Menu = new NativeMenu() };
        var viewErrorsItem = new NativeMenuItem("View Errors");
        viewErrorsItem.Click += (_, _) =>
        {
            var logViewerPlugin = manager.Plugins.FirstOrDefault(p => p.Name == "Log Viewer");
            if (logViewerPlugin is LogViewerPlugin viewer) // Cast to our specific type
            {
                viewer.InitialFilter = "[ERROR]"; // Set the filter before starting
                manager.EnablePlugin(viewer);
            }
        };
        logsMenu.Menu.Add(viewErrorsItem);

        return new NativeMenu
        {
            Items =
            {
                new NativeMenuItem("Settings") { Menu = new NativeMenu { Items = {
                    new NativeMenuItem("Control Panel...") { Command = new ServicesRelayCommand(() => new ControlPanelWindow(manager).Show()) },
                    new NativeMenuItem("Plugin Manager...") { Command = new ServicesRelayCommand(() => new PluginSettingsWindow(manager).Show()) },
                    new NativeMenuItem("Theme Settings...") { Command = new ServicesRelayCommand(() => new ThemeSettingsWindow(manager).Show()) },
                }}},
                new NativeMenuItemSeparator(),
                logsMenu, // **Add the new Logs menu here**
                new NativeMenuItemSeparator(),
                BuildProfilesMenu(manager),
                new NativeMenuItemSeparator(),
                pluginsMenu,
                volatileMenu,
                new NativeMenuItem("Open Plugins Folder") { Command = new ServicesRelayCommand(() => {
                    try { Process.Start(new ProcessStartInfo { FileName = manager.PluginDirectory, UseShellExecute = true }); }
                    catch (Exception ex) { Logger.Log($"Failed to open plugin folder: {ex.Message}"); }
                })},
                new NativeMenuItemSeparator(),
                new NativeMenuItem("Exit") { Command = new ServicesRelayCommand(() => Shutdown()) }
            }
        };
    }

    private NativeMenuItem BuildPluginMenuItem(IPlugin plugin, PluginManager manager, AppSettings settings)
    {
        var status = manager.GetStatus(plugin);
        string label = plugin.Name + status switch
        {
            PluginChangeStatus.New => " (NEW)",
            PluginChangeStatus.Updated => " (UPDATED)",
            _ => ""
        };

        var menuItem = new NativeMenuItem(label)
        {
            ToggleType = NativeMenuItemToggleType.CheckBox,
            IsChecked = manager.IsEnabled(plugin)
        };

        menuItem.Command = new ServicesRelayCommand(o =>
        {
            if (o is not NativeMenuItem item) return;

            bool shouldBeEnabled = !manager.IsEnabled(plugin);
            if (shouldBeEnabled)
            {
                manager.EnablePlugin(plugin);
            }
            else
            {
                manager.DisablePlugin(plugin);
            }

            item.IsChecked = manager.IsEnabled(plugin);
            settings.PluginEnabled[plugin.Name] = item.IsChecked;
            SettingsManager.Save();
            WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, item.IsChecked);
        });
        menuItem.CommandParameter = menuItem;

        return menuItem;
    }

    private NativeMenuItem BuildProfilesMenu(PluginManager manager)
    {
        var menu = new NativeMenuItem("Profiles") { Menu = new NativeMenu() };
        foreach (var name in WorkspaceProfiles.ProfileNames)
        {
            var item = new NativeMenuItem(name);
            item.Click += (_, _) => WorkspaceProfiles.Apply(name, manager);
            menu.Menu!.Items.Add(item);
        }
        return menu;
    }

    private NativeMenuItem BuildVolatileScriptMenuItem(string title, FilePickerFileType filter, Action<string> scriptRunner)
    {
        var menuItem = new NativeMenuItem(title);
        menuItem.Click += async (_, _) =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;

            var start = await DialogHelper.GetDefaultStartLocationAsync(desktop.MainWindow.StorageProvider);
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { filter },
                SuggestedStartLocation = start
            });

            if (files.FirstOrDefault() is { } file)
            {
                try
                {
                    await using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var code = await reader.ReadToEndAsync();
                    scriptRunner(code);
                }
                catch (Exception ex) { Logger.Log($"Failed to run volatile script {file.Name}: {ex.Message}"); }
            }
        };
        return menuItem;
    }

    private static WindowIcon CreateTrayIcon()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var icon = ExtractIconFromDll(Path.Combine(systemDir, "imageres.dll"), 25) ??
                             ExtractIconFromDll(Path.Combine(systemDir, "shell32.dll"), 20) ??
                             ExtractIconFromDll(Path.Combine(systemDir, "shell32.dll"), 8);
                if (icon != null)
                {
                    using var stream = new MemoryStream();
#pragma warning disable CA1416
                    icon.Save(stream);
#pragma warning restore CA1416
                    stream.Position = 0;
                    return new WindowIcon(stream);
                }
            }
            catch (Exception ex) { Logger.Log($"Failed to extract system icon: {ex.Message}"); }
        }
        var bytes = Convert.FromBase64String(TrayIconBase64);
        return new WindowIcon(new MemoryStream(bytes));
    }

    [SupportedOSPlatform("windows")]
    private static Icon? ExtractIconFromDll(string path, int index)
    {
        IntPtr hIcon = ExtractIcon(IntPtr.Zero, path, index);
        if (hIcon == IntPtr.Zero) return null;
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    #endregion
}

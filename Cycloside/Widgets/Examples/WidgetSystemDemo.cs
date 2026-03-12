using Avalonia.Controls;
using Cycloside.Widgets.Themes;
using Cycloside.Widgets.Animations;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Widgets.Examples;

/// <summary>
/// Demonstration class showing how to use the enhanced widget system
/// </summary>
public class WidgetSystemDemo
{
    private readonly EnhancedWidgetHostViewModel _hostViewModel;
    private readonly WidgetThemeManager _themeManager;

    public WidgetSystemDemo()
    {
        // Create a dummy window for the demo
        var dummyWindow = new WidgetHostWindow();
        _hostViewModel = new EnhancedWidgetHostViewModel(dummyWindow);
        _themeManager = _hostViewModel.ThemeManager;
    }

    /// <summary>
    /// Demonstrates creating and configuring widgets
    /// </summary>
    public async Task DemonstrateWidgetCreation()
    {
        Console.WriteLine("=== Widget Creation Demo ===");

        // Create a comprehensive example widget
        var comprehensiveWidget = new ComprehensiveWidgetExample();
        var context = new WidgetContext
        {
            ThemeManager = _themeManager,
            InstanceId = Guid.NewGuid().ToString(),
            HostViewModel = _hostViewModel,
            Configuration = new Dictionary<string, object>()
        };

        // Configure the widget
        context.SetConfigValue("updateInterval", 2000);
        context.SetConfigValue("showProgress", true);
        context.SetConfigValue("enableAnimations", true);
        context.SetConfigValue("customTitle", "Demo Widget");

        // Initialize and activate
        await comprehensiveWidget.OnInitializeAsync(context);
        await comprehensiveWidget.OnActivateAsync();

        Console.WriteLine($"Created widget: {comprehensiveWidget.Name}");
        Console.WriteLine($"Description: {comprehensiveWidget.Description}");
        Console.WriteLine($"Category: {comprehensiveWidget.Category}");
        Console.WriteLine($"Supports Multiple Instances: {comprehensiveWidget.SupportsMultipleInstances}");
        Console.WriteLine($"Default Size: {comprehensiveWidget.DefaultSize}");
        Console.WriteLine($"Minimum Size: {comprehensiveWidget.MinimumSize}");
    }

    /// <summary>
    /// Demonstrates theme management
    /// </summary>
    public async Task DemonstrateThemeManagement()
    {
        Console.WriteLine("\n=== Theme Management Demo ===");

        // Create different themes
        var lightTheme = BuiltInThemes.CreateLightTheme();
        var darkTheme = BuiltInThemes.CreateDarkTheme();
        var customTheme = new WidgetTheme
        {
            Name = "Custom Blue",
            DisplayName = "Custom Blue",
            Description = "A custom blue theme",
            Author = "Demo",
            CustomProperties = new Dictionary<string, object>
            {
                ["WidgetBackground"] = "#E3F2FD",
                ["HeaderForeground"] = "#0D47A1",
                ["BodyForeground"] = "#1565C0",
                ["AccentColor"] = "#2196F3",
                ["AccentForeground"] = "#FFFFFF",
                ["ControlBackground"] = "#FFFFFF",
                ["BorderBrush"] = "#BBDEFB",
                ["HeaderFontSize"] = 16.0,
                ["BodyFontSize"] = 12.0,
                ["WidgetPadding"] = 12.0,
                ["ButtonPadding"] = 8.0,
                ["CornerRadius"] = 6.0
            }
        };

        // Register themes
        _themeManager.RegisterTheme(lightTheme);
        _themeManager.RegisterTheme(darkTheme);
        _themeManager.RegisterTheme(customTheme);

        Console.WriteLine($"Registered themes:");
        foreach (var theme in _themeManager.GetAvailableThemes())
        {
            Console.WriteLine($"  - {theme.Name}");
        }

        // Switch themes
        _themeManager.SetCurrentTheme("Light");
        Console.WriteLine($"Current theme: {_themeManager.CurrentTheme}");

        await Task.Delay(1000);

        _themeManager.SetCurrentTheme("Dark");
        Console.WriteLine($"Switched to theme: {_themeManager.CurrentTheme}");

        await Task.Delay(1000);

        _themeManager.SetCurrentTheme("Custom Blue");
        Console.WriteLine($"Switched to theme: {_themeManager.CurrentTheme}");
    }

    /// <summary>
    /// Demonstrates widget lifecycle management
    /// </summary>
    public async Task DemonstrateWidgetLifecycle()
    {
        Console.WriteLine("\n=== Widget Lifecycle Demo ===");

        var widgets = new List<IWidgetV2>
        {
            new SystemMonitorWidget(),
            new QuickNotesWidget(),
            new CalculatorWidget(),
            new NetworkMonitorWidget(),
            new ComprehensiveWidgetExample()
        };

        foreach (var widget in widgets)
        {
            Console.WriteLine($"\nTesting lifecycle for: {widget.Name}");

            var context = new WidgetContext
            {
                ThemeManager = _themeManager,
                InstanceId = Guid.NewGuid().ToString(),
                HostViewModel = _hostViewModel,
                Configuration = new Dictionary<string, object>()
            };

            try
            {
                // Initialize
                Console.WriteLine("  Initializing...");
                await widget.OnInitializeAsync(context);

                // Activate
                Console.WriteLine("  Activating...");
                await widget.OnActivateAsync();

                // Simulate configuration change
                Console.WriteLine("  Changing configuration...");
                context.SetConfigValue("testProperty", "testValue");
                await widget.OnConfigurationChangedAsync(context.Configuration);

                // Simulate theme change
                Console.WriteLine("  Changing theme...");
                var newTheme = _themeManager.GetAvailableThemes().FirstOrDefault(t => t.Name != _themeManager.CurrentTheme);
                if (newTheme != null)
                {
                    await widget.OnThemeChangedAsync(newTheme.Name);
                }

                // Export data
                Console.WriteLine("  Exporting data...");
                var exportedData = await widget.ExportDataAsync();
                Console.WriteLine($"    Exported {exportedData.Count} data items");

                // Import data
                Console.WriteLine("  Importing data...");
                await widget.ImportDataAsync(exportedData);

                // Deactivate
                Console.WriteLine("  Deactivating...");
                await widget.OnDeactivateAsync();

                // Destroy
                Console.WriteLine("  Destroying...");
                await widget.OnDestroyAsync();

                Console.WriteLine("  ✓ Lifecycle completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error during lifecycle: {ex.Message}");
            }
        }
    }    /// <summary>
    /// Demonstrates animation capabilities
    /// </summary>
    public async Task DemonstrateAnimations()
    {
        Console.WriteLine("\n=== Animation Demo ===");

        var widget = new ComprehensiveWidgetExample();
        var context = new WidgetContext
        {
            ThemeManager = _themeManager,
            InstanceId = Guid.NewGuid().ToString(),
            HostViewModel = _hostViewModel,
            Configuration = new Dictionary<string, object>
            {
                ["enableAnimations"] = true
            }
        };

        await widget.OnInitializeAsync(context);
        var view = widget.BuildView(context);

        Console.WriteLine("Testing animations:");
        Console.WriteLine("  - Fade In");
        Console.WriteLine("  - Scale In");
        Console.WriteLine("  - Pulse");
        Console.WriteLine("  - Slide In");
        Console.WriteLine("  - Bounce");
        Console.WriteLine("  - Shake");

        // Note: In a real application, these would be visual animations
        // Here we're just demonstrating the API calls
        try
        {
            await WidgetAnimations.FadeInAsync(view, TimeSpan.FromMilliseconds(500));
            await WidgetAnimations.ScaleInAsync(view, TimeSpan.FromMilliseconds(300));
            await WidgetAnimationsExtended.PulseAsync(view, 3, TimeSpan.FromMilliseconds(400));
            await WidgetAnimationsExtended.SlideInAsync(view, Cycloside.Widgets.Animations.SlideDirection.Left, TimeSpan.FromMilliseconds(350));
            await WidgetAnimationsExtended.BounceAsync(view, 1.2, TimeSpan.FromMilliseconds(600));
            await WidgetAnimationsExtended.ShakeAsync(view, 10, TimeSpan.FromMilliseconds(300));
            await WidgetAnimations.FadeOutAsync(view, TimeSpan.FromMilliseconds(300));

            Console.WriteLine("  ✓ All animations completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Animation error: {ex.Message}");
        }

        await widget.OnDestroyAsync();
    }

    /// <summary>
    /// Demonstrates configuration management
    /// </summary>
    public void DemonstrateConfiguration()
    {
        Console.WriteLine("\n=== Configuration Demo ===");

        var widget = new ComprehensiveWidgetExample();
        var schema = widget.GetConfigurationSchema();

        Console.WriteLine("Configuration schema:");
        foreach (var kvp in schema)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        var context = new WidgetContext
        {
            ThemeManager = _themeManager,
            InstanceId = Guid.NewGuid().ToString(),
            HostViewModel = _hostViewModel,
            Configuration = new Dictionary<string, object>()
        };

        // Test configuration operations
        Console.WriteLine("\nTesting configuration operations:");

        // Set values
        context.SetConfigValue("updateInterval", 5000);
        context.SetConfigValue("customTitle", "Configured Widget");
        context.SetConfigValue("enableAnimations", false);
        Console.WriteLine("  ✓ Set configuration values");

        // Get values
        var interval = context.GetConfigValue("updateInterval", 1000);
        var title = context.GetConfigValue("customTitle", "Default");
        var animations = context.GetConfigValue("enableAnimations", true);
        Console.WriteLine($"  ✓ Retrieved values: interval={interval}, title={title}, animations={animations}");

        // Check existence
        var hasInterval = context.HasConfigValue("updateInterval");
        var hasNonExistent = context.HasConfigValue("nonExistentProperty");
        Console.WriteLine($"  ✓ Checked existence: hasInterval={hasInterval}, hasNonExistent={hasNonExistent}");

        // Remove value
        context.RemoveConfigValue("enableAnimations");
        var animationsAfterRemoval = context.HasConfigValue("enableAnimations");
        Console.WriteLine($"  ✓ Removed value: animationsExists={animationsAfterRemoval}");

        // Clear all
        context.ClearConfiguration();
        var countAfterClear = context.Configuration.Count;
        Console.WriteLine($"  ✓ Cleared configuration: count={countAfterClear}");
    }

    /// <summary>
    /// Demonstrates widget management features
    /// </summary>
    public async Task DemonstrateWidgetManagement()
    {
        Console.WriteLine("\n=== Widget Management Demo ===");

        // Create multiple widget instances
        var widgets = new List<(IWidgetV2 Widget, WidgetContext Context)>();

        for (int i = 0; i < 3; i++)
        {
            var widget = new ComprehensiveWidgetExample();
            var context = new WidgetContext
            {
                ThemeManager = _themeManager,
                InstanceId = $"instance-{i + 1}",
                HostViewModel = _hostViewModel,
                Configuration = new Dictionary<string, object>
                {
                    ["customTitle"] = $"Widget Instance {i + 1}",
                    ["updateInterval"] = 1000 + (i * 500)
                }
            };

            await widget.OnInitializeAsync(context);
            await widget.OnActivateAsync();
            widgets.Add((widget, context));

            Console.WriteLine($"  Created widget instance: {context.InstanceId}");
        }

        Console.WriteLine($"\nManaging {widgets.Count} widget instances:");

        // Export data from all widgets
        var allData = new Dictionary<string, Dictionary<string, object>>();
        foreach (var (widget, context) in widgets)
        {
            var data = await widget.ExportDataAsync();
            allData[context.InstanceId] = data;
            Console.WriteLine($"  Exported data from {context.InstanceId}: {data.Count} items");
        }

        // Change theme for all widgets
        var darkTheme = _themeManager.GetAvailableThemes().FirstOrDefault(t => t.Name == "Dark");
        if (darkTheme != null)
        {
            Console.WriteLine("  Applying dark theme to all widgets...");
            foreach (var (widget, context) in widgets)
            {
                await widget.OnThemeChangedAsync(darkTheme.Name);
            }
        }

        // Cleanup all widgets
        Console.WriteLine("  Cleaning up widgets...");
        foreach (var (widget, context) in widgets)
        {
            await widget.OnDeactivateAsync();
            await widget.OnDestroyAsync();
        }

        Console.WriteLine("  ✓ Widget management demo completed");
    }

    /// <summary>
    /// Runs all demonstrations
    /// </summary>
    public async Task RunAllDemonstrations()
    {
        Console.WriteLine("Starting Enhanced Widget System Demonstrations");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            await DemonstrateWidgetCreation();
            await DemonstrateThemeManagement();
            await DemonstrateWidgetLifecycle();
            await DemonstrateAnimations();
            DemonstrateConfiguration();
            await DemonstrateWidgetManagement();

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("All demonstrations completed successfully! ✓");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nDemo failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Cycloside.Models;
using Cycloside.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn.Views;

public partial class SkinnedMP3PlayerWindow : Window
{
    private Image? _skinBackground;
    private Canvas? _uiCanvas;
    private Border? _fallbackUI;

    public SkinnedMP3PlayerWindow()
    {
        InitializeComponent();

        // Get references to named controls
        _skinBackground = this.FindControl<Image>("SkinBackground");
        _uiCanvas = this.FindControl<Canvas>("UICanvas");
        _fallbackUI = this.FindControl<Border>("FallbackUI");

        // Subscribe to skin changes
        WinampSkinManager.Instance.SkinChanged += OnSkinChanged;

        // Apply current skin if one is loaded
        if (WinampSkinManager.Instance.CurrentSkin != null)
        {
            ApplySkin(WinampSkinManager.Instance.CurrentSkin);
        }
        else
        {
            ShowFallbackUI();
        }

        // Set window to be draggable by clicking anywhere
        PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        };
    }

    private void OnSkinChanged(WinampSkin skin)
    {
        if (skin != null)
        {
            ApplySkin(skin);
        }
        else
        {
            ShowFallbackUI();
        }
    }

    private void ApplySkin(WinampSkin skin)
    {
        if (_skinBackground == null || _uiCanvas == null || _fallbackUI == null)
            return;

        // Show skinned UI, hide fallback
        _fallbackUI.IsVisible = false;
        _uiCanvas.IsVisible = true;

        // Apply main skin bitmap as background
        if (skin.MainBitmap != null)
        {
            _skinBackground.Source = skin.MainBitmap;

            // Resize window to match skin dimensions
            // Classic Winamp main window is 275x116
            Width = skin.MainBitmap.PixelSize.Width;
            Height = skin.MainBitmap.PixelSize.Height;

            Logger.Log($"Applied skin: {skin.Name} ({Width}x{Height})");
        }

        // Apply visualization colors if available
        if (skin.VisualizationColors.Any())
        {
            // Could pass these to the visualization plugin
            Logger.Log($"Skin has {skin.VisualizationColors.Count} visualization colors");
        }

        // Apply region data for window shape if available
        if (skin.RegionData.Any())
        {
            // Could apply custom window regions here
            Logger.Log($"Skin has region data: {skin.RegionData.Count} entries");
        }
    }

    private void ShowFallbackUI()
    {
        if (_uiCanvas == null || _fallbackUI == null)
            return;

        // Hide skinned UI, show fallback
        _uiCanvas.IsVisible = false;
        _fallbackUI.IsVisible = true;

        // Reset to default size
        Width = 275;
        Height = 200; // Taller for fallback UI with controls

        Logger.Log("Using fallback MP3 player UI (no skin loaded)");
    }

    private void OnMinimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from skin changes
        WinampSkinManager.Instance.SkinChanged -= OnSkinChanged;
        base.OnClosed(e);
    }
}

/// <summary>
/// Extension methods for MP3Player plugin to support skin changing
/// </summary>
public static class MP3PlayerSkinExtensions
{
    /// <summary>
    /// Command to show skin selection dialog
    /// </summary>
    public static async Task ChangeSkin(this MP3PlayerPlugin plugin, Window? parentWindow)
    {
        try
        {
            var skinManager = WinampSkinManager.Instance;

            // Refresh available skins
            skinManager.RefreshAvailableSkins();

            var skins = skinManager.AvailableSkins;

            if (!skins.Any())
            {
                await ShowMessage(parentWindow, "No Winamp Skins Found",
                    $"No WSZ skins found. Place .wsz files in:\n{skinManager.GetSkinsDirectory()}");
                return;
            }

            // Show skin selection dialog
            var skinNames = skins.Select(s => skinManager.GetSkinDisplayName(s)).ToArray();

            // For now, just show a simple selection - in a full implementation,
            // would create a custom dialog with skin previews
            var message = "Available Skins:\n\n" +
                         string.Join("\n", skinNames.Select((name, i) => $"{i + 1}. {name}"));

            await ShowMessage(parentWindow, "Select Winamp Skin", message);

            Logger.Log($"Found {skins.Count} skins. Skin selection UI would show here.");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error in ChangeSkin: {ex.Message}");
        }
    }

    /// <summary>
    /// Command to import a new WSZ skin file
    /// </summary>
    public static async Task ImportSkin(this MP3PlayerPlugin plugin, Window? parentWindow)
    {
        try
        {
            if (parentWindow == null)
                return;

            var result = await parentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Winamp Skin",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Winamp Skin Files")
                    {
                        Patterns = new[] { "*.wsz", "*.zip" }
                    }
                }
            });

            if (result.Any())
            {
                var filePath = result[0].TryGetLocalPath();
                if (filePath != null && File.Exists(filePath))
                {
                    var success = WinampSkinManager.Instance.ImportSkin(filePath);

                    if (success)
                    {
                        await ShowMessage(parentWindow, "Skin Imported",
                            $"Successfully imported and applied skin:\n{Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        await ShowMessage(parentWindow, "Import Failed",
                            "Failed to import skin. Check that it's a valid WSZ file.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error importing skin: {ex.Message}");
        }
    }

    private static async Task ShowMessage(Window? parent, string title, string message)
    {
        if (parent == null)
            return;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var closeButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Width = 80
        };
        closeButton.Click += (s, e) => dialog.Close();
        panel.Children.Add(closeButton);

        dialog.Content = panel;

        await dialog.ShowDialog(parent);
    }
}

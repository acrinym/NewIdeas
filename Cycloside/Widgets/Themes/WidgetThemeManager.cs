using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cycloside.Widgets.Themes;

/// <summary>
/// Manages widget themes and provides theme-related utilities
/// </summary>
public class WidgetThemeManager
{
    private readonly Dictionary<string, WidgetTheme> _themes = new();
    private string _currentTheme = "Default";
    
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    public WidgetThemeManager()
    {
        LoadBuiltInThemes();
    }
    
    /// <summary>
    /// Gets the current theme name
    /// </summary>
    public string CurrentTheme => _currentTheme;
    
    /// <summary>
    /// Gets all available theme names
    /// </summary>
    public IEnumerable<string> AvailableThemes => _themes.Keys;
    
    /// <summary>
    /// Gets all available theme names (alias for AvailableThemes)
    /// </summary>
    public IEnumerable<string> AvailableThemeNames => _themes.Keys;
    
    /// <summary>
    /// Gets all available themes
    /// </summary>
    public IEnumerable<WidgetTheme> GetAvailableThemes()
    {
        return _themes.Values;
    }
    
    /// <summary>
    /// Gets a theme by name
    /// </summary>
    public WidgetTheme? GetTheme(string name)
    {
        return _themes.TryGetValue(name, out var theme) ? theme : null;
    }
    
    /// <summary>
    /// Gets the current active theme
    /// </summary>
    public WidgetTheme GetCurrentTheme()
    {
        return GetTheme(_currentTheme) ?? GetTheme("Default") ?? CreateDefaultTheme();
    }
    
    /// <summary>
    /// Registers a new theme
    /// </summary>
    public void RegisterTheme(WidgetTheme theme)
    {
        _themes[theme.Name] = theme;
    }
    
    /// <summary>
    /// Sets the current theme
    /// </summary>
    public void SetCurrentTheme(string themeName)
    {
        if (_themes.ContainsKey(themeName) && _currentTheme != themeName)
        {
            var oldTheme = _currentTheme;
            _currentTheme = themeName;
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, themeName));
        }
    }
    
    /// <summary>
    /// Removes a theme
    /// </summary>
    public bool RemoveTheme(string name)
    {
        if (name == "Default") return false; // Cannot remove default theme
        return _themes.Remove(name);
    }
    
    private void LoadBuiltInThemes()
    {
        // Default theme
        RegisterTheme(BuiltInThemes.CreateDefaultTheme());
        
        // Dark theme
        RegisterTheme(BuiltInThemes.CreateDarkTheme());
        
        // Light theme
        RegisterTheme(BuiltInThemes.CreateLightTheme());
        
        // Accent theme
        RegisterTheme(BuiltInThemes.CreateAccentTheme());
    }
    
    private WidgetTheme CreateDefaultTheme()
    {
        return BuiltInThemes.CreateDefaultTheme();
    }
}
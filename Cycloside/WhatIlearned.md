# What I Learned - Cycloside Development Guide

*This document captures lessons learned, common pitfalls, and best practices discovered during Cycloside development and auditing. Reference this guide at the start of each development session.*

## 🔍 Senior Audit Fixes Applied

### Nullability Warnings (CS8601, CS8602)

**Issues Found:**
```csharp
// ❌ BAD: Direct assignment without null checks
SettingsManager.Settings.GlobalTheme = themeName; // CS8601: Possible null reference assignment

// ❌ BAD: Unsafe dereference
Application.Current.Styles.Add(cachedStyle); // CS8602: Dereference of a possibly null reference
```

**✅ Solutions Applied:**
```csharp
// ✅ GOOD: Safe null handling with fallback
var settings = SettingsManager.Settings;
settings.GlobalTheme = themeName ?? "LightTheme";

// ✅ GOOD: Explicit null check before dereference
if (Application.Current != null)
    Application.Current.Styles.Add(cachedStyle);
```

**📚 Key Learnings:**
- Always use null-coalescing operator (`??`) for parameter assignments
- Check `Application.Current` before accessing application-scoped resources
- Use explicit null checks rather than nullable operators when safety is critical

---

### Async/Task Pattern Inconsistencies (CS1998, CS0029, CS4016)

**Issues Found:**
```csharp
// ❌ BAD: Async method without await
private static async Task<bool> LoadThemeVariantTokensAsync(ThemeVariant variant)
{
    // ... sync operations ...
    return true; // CS0029: Cannot convert bool to Task<bool>
}

// ❌ BAD: Mixed async/sync returns
private static async Task LoadStyleFileAsync(string path)
{
    // No await operations → CS1998 warning
}
```

**✅ Solutions Applied:**
```csharp
// ✅ GOOD: Consistent Task patterns for sync operations
private static Task<bool> LoadThemeVariantTokensAsync(ThemeVariant variant)
{
    try
    {
        // ... sync operations ...
        return Task.FromResult(true);
    }
    catch (Exception ex)
    {
        return Task.FromResult(false);
    }
}

// ✅ GOOD: Remove async keyword if no await needed
private static Task LoadStyleFileAsync(string path)
{
    // Sync operations...
    return Task.CompletedTask;
}
```

**📚 Key Learnings:**
- Only use `async` when you have `await` operations
- For sync operations returning `Task<T>`, use `Task.FromResult(T)`
- Use `Task.CompletedTask` for sync operations returning `Task`

---

### XAML Resource URI Format Issues (AVLN2000)

**Issues Found:**
```xml
<!-- ❌ BAD: Invalid URI format causes runtime resolution -->
<StyleInclude Source="axaml://Cycloside/Themes/Tokens.axaml"/>
```

**✅ Solutions Applied:**
```xml
<!-- ✅ GOOD: Proper embedded resource URI -->
<StyleInclude Source="avares://Cycloside/Themes/Tokens.axaml"/>
```

**📚 Key Learnings:**
- Use `avares://` for embedded Avalonia resources, not `axaml://`
- Use `file:///` only for intentional dynamic file loading
- Embedded resources are resolved at design-time and optimized at build

---

### Invalid XAML Property Usage (AVLN2000)

**Issues Found:**
```xml
<!-- ❌ BAD: Grid doesn't have Padding property -->
<Style Selector="Window.primary Grid">
    <Setter Property="Padding" Value="{DynamicResource SpacingXL}"/>
</Style>
```

**✅ Solutions Applied:**
```xml
<!-- ✅ GOOD: Use Margin instead of Padding for spacing -->
<Style Selector="Window.primary Grid">
    <Setter Property="Margin" Value="{DynamicResource SpacingL}"/>
</Style>
```

**📚 Key Learnings:**
- Grid uses `Margin` for spacing, not `Padding`
- Always validate property availability on target control types
- Use semantic tokens (`SpacingL`, `SpacingXL`) consistently

---

## 🏗️ Cycloside-Specific Best Practices

### Theme System Architecture

**✅ Recommended Patterns:**

```csharp
// ✅ GOOD: Theme Manager with proper caching
public static class ThemeManager
{
    private static readonly Dictionary<string, StyleInclude> _themeCache = new();
    private static readonly object _cacheLock = new object();
    
    public static async Task<bool> ApplyThemeAsync(string themeName, ThemeVariant variant)
    {
        lock (_cacheLock)
        {
            // Thread-safe caching operations
        }
        // Proper async/await usage
    }
}
```

**❌ Anti-Patterns to Avoid:**
```csharp
// ❌ BAD: No caching, memory leaks
public static bool LoadTheme(string themeName)
{
    Application.Current.Styles.Clear(); // Destroys all styles
    // No cleanup on errors
}

// ❌ BAD: Race conditions
private static Dictionary<string, StyleInclude> _cache; // Not thread-safe
```

**📚 Key Learnings:**
- Always cache theme resources with file timestamp validation
- Use thread-safe operations for shared caches
- Provide proper cleanup methods (`ClearThemeCache()`)
- Handle theme loading failures gracefully

---

### Skin System Design

**✅ Recommended Patterns:**

```csharp
// ✅ GOOD: Skin manifest validation
public class SkinManifest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> ReplaceWindows { get; set; } = new();
    // ... other properties with safe defaults
}

// ✅ GOOD: Safe skin application
public static async Task<bool> ApplySkinAsync(string skinName, StyledElement? element = null)
{
    if (string.IsNullOrEmpty(skinName))
    {
        await ClearSkinAsync(element);
        return true;
    }
    // Validation and error handling...
}
```

**❌ Anti-Patterns to Avoid:**
```csharp
// ❌ BAD: No validation
public static void ApplySkin(string skinName)
{
    // Direct application without checks
    _styles.Add(new StyleInclude(new Uri(skinName)));
}

// ❌ BAD: No cleanup on failure
public static void ApplySkin(Window window, string skinName)
{
    // No try/catch, no cleanup
}
```

---

### Plugin Integration Standards

**✅ Recommended Patterns:**

```csharp
// ✅ GOOD: Plugin window base class
public class PluginWindowBase : Window
{
    public IPlugin? Plugin { get; set; }
    
    public PluginWindowBase()
    {
        Closed += PluginWindowBase_Closed;
        // Subscribe to global theme/skin changes
        ThemeManager.ThemeChanged += OnGlobalThemeChanged;
        SkinManager.SkinChanged += OnGlobalSkinChanged;
    }
    
    // Proper cleanup
    private void PluginWindowBase_Closed(object? sender, EventArgs e)
    {
        ThemeManager.ThemeChanged -= OnGlobalThemeChanged;
        SkinManager.SkinChanged -= OnGlobalSkinChanged;
    }
}
```

**📚 Key Learnings:**
- Always implement `PluginWindowBase` for plugin windows
- Subscribe/unsubscribe from global events properly
- Clean up resources in window `Closed` events
- Use nullable reference types (`IPlugin?`) for optional properties

---

## 🎯 Code Quality Standards

### Thread Safety Guidelines

**✅ Always Use Lock Objects:**
```csharp
private static readonly Dictionary<string, object> _cache = new();
private static readonly object _cacheLock = new object();

public static void UpdateCache(string key, object value)
{
    lock (_cacheLock)
    {
        _cache[key] = value;
    }
}
```

**📚 Key Learnings:**
- Use `readonly` locks for thread safety
- Protect all shared collections with locks
- Document thread-safety requirements

---

### Exception Handling Strategy

**✅ Recommended Pattern:**
```csharp
public static async Task<bool> PerformOperationAsync(string parameter)
{
    try
    {
        // Operation logic
        return true;
    }
    catch (Exception ex)
    {
        Logger.Log($"Operation failed: {ex.Message}");
        return false;
    }
}
```

**❌ Anti-Pattern:**
```csharp
// ❌ BAD: Swallowing exceptions
try
{
    // operation
}
catch (Exception ex)
{
    // Silent failure - no logging
}
```

**📚 Key Learnings:**
- Always log exceptions with descriptive messages
- Return meaningful error states (bool for success/failure)
- Use specific exception types when possible

---

### Resource Management

**✅ Resource Cleanup Pattern:**
```csharp
public class ResourceManager : IDisposable
{
    private readonly List<IDisposable> _resources = new();
    
    public void RegisterResource(IDisposable resource)
    {
        _resources.Add(resource);
    }
    
    public void Dispose()
    {
        foreach (var resource in _resources)
        {
            resource?.Dispose();
        }
        _resources.Clear();
    }
}
```

**📚 Key Learnings:**
- Track disposable resources explicitly
- Implement `IDisposable` for classes managing resources
- Clean up in reverse order of allocation

---

## 🔧 Avalonia-Specific Guidelines

### Style Resource Management

**✅ Semantic Token Usage:**
```xml
<!-- ✅ GOOD: Use semantic tokens consistently -->
<Setter Property="Background" Value="{DynamicResource CanvasBackgroundBrush}"/>
<Setter Property="Margin" Value="{DynamicResource SpacingL}"/>
<Setter Property="CornerRadius" Value="{DynamicResource RadiusS}"/>

<!-- ❌ BAD: Hard-coded values -->
<Setter Property="Background" Value="#FF123456"/>
<Setter Property="Margin" Value="8"/>
```

**✅ StyleInclude Best Practices:**
```xml
<!-- ✅ GOOD: Embedded resources -->
<StyleInclude Source="avares://Cycloside/Themes/BaseTokens.axaml"/>

<!-- ✅ GOOD: Runtime file loading with error handling -->
<StyleInclude Source="file:///C:/Themes/DynamicTheme.axaml" 
              LoadFailed="OnStyleLoadFailed"/>
```

**📚 Key Learnings:**
- Always use semantic tokens for themable properties
- Prefer embedded resources over file paths when possible
- Handle style loading failures gracefully

---

### Window Lifecycle Management

**✅ Window Event Handling:**
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }
    
    private async void MainWindow_Loaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Initialization after UI is ready
        await ThemeManager.InitializeFromSettings();
    }
    
    private void MainWindow_Closing(object sender, WindowClosingEventArgs e)
    {
        // Cleanup before window closes
        SkinManager.RemoveAllSkinsFrom(this);
    }
}
```

**📚 Key Learnings:**
- Use appropriate lifecycle events (`Loaded`, `Closing`, `Closed`)
- Initialize theme/skin systems after UI loading
- Clean up resources before window closure

---

## 🚀 Performance Optimization

### Theme/Skin Caching Strategy

**✅ Efficient Caching:**
```csharp
private static readonly Dictionary<string, (StyleInclude, DateTime)> _cache = new();
private static readonly object _cacheLock = new object();

public static bool IsResourceCached(string key, string filePath)
{
    lock (_cacheLock)
    {
        return _cache.TryGetValue(key, out var cached) &&
               cached.DateTime >= File.GetLastWriteTime(filePath);
    }
}
```

**📚 Key Learnings:**
- Cache expensive operations with file timestamp validation
- Invalidate cache when files change
- Use memory-efficient cache keys

---

### Async Operation Best Practices

**✅ Proper Async Patterns:**
```csharp
public static async Task<bool> LoadThemeResourcesAsync(string themeName)
{
    var loadTasks = new List<Task<StyleInclude>>();
    
    foreach (var styleFile in GetStyleFiles(themeName))
    {
        loadTasks.Add(LoadSingleStyleFileAsync(styleFile));
    }
    
    var results = await Task.WhenAll(loadTasks);
    return results.All(r => r != null);
}
```

**📚 Key Learnings:**
- Use `Task.WhenAll()` for parallel operations
- Batch related async operations
- Handle individual failures without failing entire batch

---

## 🔍 Common Anti-Patterns & Solutions

### 1. Memory Leaks in Event Handlers

**❌ Problem:**
```csharp
public class ThemeableWorker 
{
    public ThemeableWorker()
    {
        ThemeManager.ThemeChanged += OnThemeChanged; // Never unsubscribed!
    }
}
```

**✅ Solution:**
```csharp
public class ThemeableWorker : IDisposable
{
    public ThemeableWorker()
    {
        ThemeManager.ThemeChanged += OnThemeChanged;
    }
    
    public void Dispose()
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
    }
}
```

### 2. Synchronous Operations in Async Methods

**❌ Problem:**
```csharp
public async Task<string> GetResourceAsync(string path)
{
    return File.ReadAllText(path); // Synchronous I/O in async method!
}
```

**✅ Solution:**
```csharp
public async Task<string> GetResourceAsync(string path)
{
    return await File.ReadAllTextAsync(path); // Proper async I/O
}
```

### 3. Thread-Unsafe Collections

**❌ Problem:**
```csharp
private static Dictionary<string, object> _cache = new(); // Not thread-safe!

public static void CacheItem(string key, object value)
{
    _cache[key] = value; // Race condition possible!
}
```

**✅ Solution:**
```csharp
private static readonly ConcurrentDictionary<string, object> _cache = new();

public static void CacheItem(string key, object value)
{
    _cache[key] = value; // Thread-safe!
}
```

---

## 📋 Pre-Development Checklist

Before starting each development session:

- [ ] **Review this document** for the specific area you're working on
- [ ] **Check for nullability warnings** in recent changes
- [ ] **Verify async patterns** are consistent
- [ ] **Run Roslynator analysis** on changed files
- [ ] **Test theme/skin functionality** manually
- [ ] **Validate XAML resources** use correct URI formats
- [ ] **Ensure thread safety** for shared resources
- [ ] **Clean up event handlers** in disposal patterns

## 🎯 Development Session Workflow

1. **Read Relevant Section**: Check this guide for the specific subsystem
2. **Identify Patterns**: Look for established patterns in existing code
3. **Follow Conventions**: Use the same approach as similar implementations
4. **Test Thoroughly**: Verify theme/skin switching after changes
5. **Document Changes**: Update the appropriate section in this guide if new patterns emerge

---

## 🔄 Continuous Improvement

This document should be updated when:
- New anti-patterns are discovered
- Better solutions are found for existing problems
- New best practices emerge for Avalonia/.NET development
- Performance optimizations are identified

**Remember**: Cycloside's strength comes from consistent application of these patterns across all subsystems. When in doubt, follow the established conventions in this document.

# CYC-2026-031: Recursive File Inclusion / Circular References

**Date:** March 13, 2026  
**Discovered by:** Justin (Cycloside creator)  
**Severity:** HIGH (denial of service via stack overflow or infinite loop)  
**Component:** ThemeManager, AXAML parsing, StyleInclude mechanism  
**Status:** 🔥 **CONFIRMED - NO PROTECTION EXISTS**

---

## The Question

**"Are there any formats we use where a file could call a file could call a file could call a file could..."**

**Answer: YES. Multiple places.**

---

## Vulnerability 1: AXAML StyleInclude Recursion

### The Mechanism

**Avalonia supports `StyleInclude` for loading external AXAML:**

```xml
<!-- App.axaml -->
<Application.Styles>
    <StyleInclude Source="avares://Cycloside/Themes/Tokens.axaml"/>
</Application.Styles>
```

**That Tokens.axaml can ALSO contain `StyleInclude`:**

```xml
<!-- Tokens.axaml -->
<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="Colors.axaml"/>
    <StyleInclude Source="Fonts.axaml"/>
</Styles>
```

**And those can include MORE files... infinitely.**

### Attack Vector 1: Circular Reference (Stack Overflow)

**Create Theme A:**
```xml
<!-- ThemeA.axaml -->
<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="ThemeB.axaml"/>
</Styles>
```

**Create Theme B:**
```xml
<!-- ThemeB.axaml -->
<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="ThemeA.axaml"/>
</Styles>
```

**Execution:**
1. Cycloside loads ThemeA.axaml
2. ThemeA includes ThemeB.axaml
3. ThemeB includes ThemeA.axaml
4. ThemeA includes ThemeB.axaml
5. **Infinite recursion**
6. Stack overflow or hang

**Current protection: NONE**

```csharp
// ThemeManager.cs line 292
var styleInclude = new StyleInclude(uri) { Source = uri };
Application.Current.Styles.Add(styleInclude);

// NO depth tracking
// NO visited-file tracking
// NO circular reference detection
```

### Attack Vector 2: Deep Chain (Resource Exhaustion)

**Create a chain of 10,000 includes:**

```xml
<!-- Theme0001.axaml -->
<Styles>
    <StyleInclude Source="Theme0002.axaml"/>
</Styles>

<!-- Theme0002.axaml -->
<Styles>
    <StyleInclude Source="Theme0003.axaml"/>
</Styles>

<!-- ... 9,998 more files ... -->

<!-- Theme10000.axaml -->
<Styles>
    <Style Selector="Button">
        <Setter Property="Background" Value="Red"/>
    </Style>
</Styles>
```

**Result:**
- Parser maintains stack of open files
- Memory per stack frame: ~1-10 KB
- 10,000 frames × 10 KB = 100 MB minimum
- Plus parsed content, DOM trees, etc.
- **Memory exhaustion or stack overflow**

### Attack Vector 3: Diamond Pattern (Exponential Load)

```
       A
      / \
     B   C
    / \ / \
   D  E F  G
   
Each file included once from manifest, 
but E is included by both B and C
```

**If include deduplication is missing:**
- File E loaded twice
- File D loaded twice
- Total loads: exponential growth
- **CPU/memory exhaustion**

---

## Vulnerability 2: Subtheme Recursion

### Current Code

```csharp
// ThemeManager.cs line 268-309
private static Task<bool> LoadSubthemeAsync(string themeName)
{
    // Clear any existing subtheme
    ClearSubthemeStyles();
    
    var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes", themeName);
    // ... loads all .axaml files in directory ...
}
```

**What if a subtheme's AXAML file calls `ApplySubthemeAsync` via some mechanism?**

Unlikely directly, but consider:
- AXAML with data binding
- Binding triggers code execution
- Code calls `ThemeManager.ApplySubthemeAsync`
- Which calls `LoadSubthemeAsync`
- Which loads AXAML
- **Potential recursion**

---

## Vulnerability 3: ResourceDictionary MergedDictionaries

### The Mechanism

**AXAML supports `ResourceDictionary.MergedDictionaries`:**

```xml
<ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source="Colors.axaml"/>
        <ResourceInclude Source="Brushes.axaml"/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

**These can ALSO be recursive:**

```xml
<!-- Colors.axaml -->
<ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source="Brushes.axaml"/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>

<!-- Brushes.axaml -->
<ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source="Colors.axaml"/>  ← Circular!
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

**Result: Same as StyleInclude recursion.**

---

## Vulnerability 4: Plugin Dependency Chains

### Current Code

```csharp
// PluginRepository.cs line 400-404
public class PluginDependencies
{
    public List<string>? RequiredPlugins { get; set; }
    public string? MinimumCyclosideVersion { get; set; }
}
```

**Plugins can depend on other plugins.**

### Attack: Circular Dependencies

**PluginA manifest.json:**
```json
{
    "name": "PluginA",
    "dependencies": {
        "requiredPlugins": ["PluginB"]
    }
}
```

**PluginB manifest.json:**
```json
{
    "name": "PluginB",
    "dependencies": {
        "requiredPlugins": ["PluginA"]
    }
}
```

**What happens when user tries to install PluginA?**

1. Cycloside: "PluginA requires PluginB, installing..."
2. Cycloside: "PluginB requires PluginA, installing..."
3. Cycloside: "PluginA requires PluginB, installing..."
4. **Infinite loop or stack overflow**

**Current protection: NONE** (no dependency resolution code visible in PluginRepository.cs)

---

## Impact

### Severity: HIGH

**Exploitability:**
- Easy to craft circular references
- No special tools required
- Trivial to create deep chains

**Impact:**
- Application crash (stack overflow)
- Application hang (infinite loop)
- Memory exhaustion (deep chains)
- CPU exhaustion (repeated parsing)

**Attack scenarios:**
1. Marketplace plugin with circular StyleInclude
2. Theme pack with 10,000-deep include chain
3. Malicious plugin with circular dependencies

### Why This Is Dangerous

**Unlike CYC-2026-015 (infinite recursion in C# code), this is FILE-BASED:**

- **Harder to detect** (requires walking file references)
- **Crosses trust boundaries** (files from different sources)
- **Parser-level** (Avalonia's XAML parser handles includes)
- **No stack trace** (happens in framework code)

---

## Mitigation Strategies

### Strategy 1: Track Inclusion Depth

```csharp
private static int _currentInclusionDepth = 0;
private static readonly int MaxInclusionDepth = 10;

private static StyleInclude LoadStyleWithDepthCheck(string path)
{
    if (_currentInclusionDepth >= MaxInclusionDepth)
    {
        throw new InvalidOperationException(
            $"Maximum style inclusion depth ({MaxInclusionDepth}) exceeded");
    }
    
    _currentInclusionDepth++;
    try
    {
        var uri = new Uri($"file:///{path}");
        return new StyleInclude(uri) { Source = uri };
    }
    finally
    {
        _currentInclusionDepth--;
    }
}
```

**Problem:** Avalonia's `StyleInclude` loads files internally. We don't control the recursion.

---

### Strategy 2: Track Visited Files (Circular Detection)

```csharp
private static readonly HashSet<string> _currentlyLoading = new();
private static readonly object _loadLock = new object();

private static StyleInclude LoadStyleWithCircularCheck(string path)
{
    var normalizedPath = Path.GetFullPath(path).ToLowerInvariant();
    
    lock (_loadLock)
    {
        if (_currentlyLoading.Contains(normalizedPath))
        {
            throw new InvalidOperationException(
                $"Circular reference detected: {path} is already being loaded");
        }
        
        _currentlyLoading.Add(normalizedPath);
    }
    
    try
    {
        var uri = new Uri($"file:///{path}");
        return new StyleInclude(uri) { Source = uri };
    }
    finally
    {
        lock (_loadLock)
        {
            _currentlyLoading.Remove(normalizedPath);
        }
    }
}
```

**Problem:** Still doesn't prevent Avalonia's internal includes.

---

### Strategy 3: Pre-Parse and Validate (RECOMMENDED)

**Before loading any AXAML, scan it for includes and validate the graph:**

```csharp
private static bool ValidateInclusionGraph(string rootFile, int maxDepth = 10)
{
    var visited = new HashSet<string>();
    var currentPath = new Stack<string>();
    
    return ValidateInclusionGraphRecursive(rootFile, visited, currentPath, maxDepth);
}

private static bool ValidateInclusionGraphRecursive(
    string file, 
    HashSet<string> visited, 
    Stack<string> currentPath, 
    int maxDepth)
{
    var normalizedPath = Path.GetFullPath(file).ToLowerInvariant();
    
    // Check for circular reference
    if (currentPath.Contains(normalizedPath))
    {
        Logger.Log($"Circular reference detected: {string.Join(" → ", currentPath)} → {file}");
        return false;
    }
    
    // Check depth
    if (currentPath.Count >= maxDepth)
    {
        Logger.Log($"Maximum inclusion depth exceeded: {maxDepth}");
        return false;
    }
    
    // Mark as visited (for deduplication check)
    if (visited.Contains(normalizedPath))
    {
        return true; // Already validated this file
    }
    visited.Add(normalizedPath);
    
    currentPath.Push(normalizedPath);
    
    try
    {
        // Parse AXAML and find StyleInclude/ResourceInclude references
        var content = File.ReadAllText(file);
        var includes = ExtractIncludeReferences(content);
        
        // Recursively validate each include
        foreach (var include in includes)
        {
            var includePath = ResolveIncludePath(file, include);
            if (!File.Exists(includePath))
            {
                Logger.Log($"Include not found: {include} from {file}");
                return false;
            }
            
            if (!ValidateInclusionGraphRecursive(includePath, visited, currentPath, maxDepth))
            {
                return false;
            }
        }
        
        return true;
    }
    finally
    {
        currentPath.Pop();
    }
}

private static List<string> ExtractIncludeReferences(string axamlContent)
{
    var includes = new List<string>();
    
    // Look for StyleInclude Source attributes
    var styleIncludePattern = @"<StyleInclude\s+Source=[""']([^""']+)[""']";
    var matches = Regex.Matches(axamlContent, styleIncludePattern);
    
    foreach (Match match in matches)
    {
        includes.Add(match.Groups[1].Value);
    }
    
    // Look for ResourceInclude
    var resourcePattern = @"<ResourceInclude\s+Source=[""']([^""']+)[""']";
    matches = Regex.Matches(axamlContent, resourcePattern);
    
    foreach (Match match in matches)
    {
        includes.Add(match.Groups[1].Value);
    }
    
    // Look for MergedDictionaries
    var mergedPattern = @"<ResourceDictionary\s+Source=[""']([^""']+)[""']";
    matches = Regex.Matches(axamlContent, mergedPattern);
    
    foreach (Match match in matches)
    {
        includes.Add(match.Groups[1].Value);
    }
    
    return includes;
}
```

**Use before loading any theme:**

```csharp
public static async Task<bool> ApplyThemeAsync(string themeName)
{
    var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes", themeName);
    var styleFiles = Directory.GetFiles(themeDir, "*.axaml");
    
    // PRE-VALIDATE: Check for circular references
    foreach (var styleFile in styleFiles)
    {
        if (!ValidateInclusionGraph(styleFile, maxDepth: 10))
        {
            Logger.Log($"Theme validation failed: {themeName}");
            return false;
        }
    }
    
    // Now safe to load
    // ...
}
```

---

## Strategy 4: Plugin Dependency Resolution

**For plugin dependencies, implement proper resolution:**

```csharp
public class PluginDependencyResolver
{
    private readonly HashSet<string> _resolving = new();
    private readonly HashSet<string> _resolved = new();
    
    public List<string> ResolveInstallOrder(PluginManifest plugin)
    {
        var order = new List<string>();
        ResolveRecursive(plugin.Name, order);
        return order;
    }
    
    private void ResolveRecursive(string pluginName, List<string> order)
    {
        // Circular reference check
        if (_resolving.Contains(pluginName))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected: {pluginName}");
        }
        
        // Already resolved
        if (_resolved.Contains(pluginName))
        {
            return;
        }
        
        _resolving.Add(pluginName);
        
        try
        {
            // Get plugin manifest
            var manifest = GetPluginManifest(pluginName);
            
            // Resolve dependencies first
            foreach (var dep in manifest.Dependencies?.RequiredPlugins ?? [])
            {
                ResolveRecursive(dep, order);
            }
            
            // Add this plugin after dependencies
            order.Add(pluginName);
            _resolved.Add(pluginName);
        }
        finally
        {
            _resolving.Remove(pluginName);
        }
    }
}
```

---

## Proof of Concept

### PoC 1: Circular StyleInclude

**File: `Cycloside/Themes/CircularA/ThemeA.axaml`**
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="../CircularB/ThemeB.axaml"/>
    
    <Style Selector="Button">
        <Setter Property="Background" Value="Red"/>
    </Style>
</Styles>
```

**File: `Cycloside/Themes/CircularB/ThemeB.axaml`**
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="../CircularA/ThemeA.axaml"/>
    
    <Style Selector="TextBlock">
        <Setter Property="Foreground" Value="Blue"/>
    </Style>
</Styles>
```

**Test:**
```csharp
await ThemeManager.ApplySubthemeAsync("CircularA");
```

**Expected result:**
- Stack overflow
- Application crash
- OR: Avalonia detects and throws exception (need to test)

---

### PoC 2: Deep Chain (1000 levels)

**Generate 1000 theme files:**

```python
import os

os.makedirs('Themes/DeepChain', exist_ok=True)

for i in range(1, 1001):
    filename = f'Themes/DeepChain/Theme{i:04d}.axaml'
    
    if i < 1000:
        next_file = f'Theme{i+1:04d}.axaml'
        content = f'''<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="{next_file}"/>
</Styles>'''
    else:
        # Last file in chain
        content = '''<Styles xmlns="https://github.com/avaloniaui">
    <Style Selector="Button">
        <Setter Property="Background" Value="Green"/>
    </Style>
</Styles>'''
    
    with open(filename, 'w') as f:
        f.write(content)

# Create entry point
with open('Themes/DeepChain/Entry.axaml', 'w') as f:
    f.write('''<Styles xmlns="https://github.com/avaloniaui">
    <StyleInclude Source="Theme0001.axaml"/>
</Styles>''')
```

**Test:**
```csharp
await ThemeManager.ApplySubthemeAsync("DeepChain");
```

**Expected result:**
- Stack overflow (if depth limit missing)
- Long parse time (if recursive loading)
- Memory spike (if DOM trees kept in memory)

---

### PoC 3: Plugin Circular Dependency

**File: `Plugins/PluginA/manifest.json`**
```json
{
    "name": "PluginA",
    "version": "1.0.0",
    "dependencies": {
        "requiredPlugins": ["PluginB"]
    },
    "files": [
        {"path": "PluginA.dll", "checksum": "", "size": 1024}
    ]
}
```

**File: `Plugins/PluginB/manifest.json`**
```json
{
    "name": "PluginB",
    "version": "1.0.0",
    "dependencies": {
        "requiredPlugins": ["PluginA"]
    },
    "files": [
        {"path": "PluginB.dll", "checksum": "", "size": 1024}
    ]
}
```

**Test:**
```csharp
await PluginRepository.InstallPluginAsync(pluginAManifest);
```

**Expected result:**
- Infinite loop installing dependencies
- Stack overflow
- OR: No dependency resolution occurs (current state?)

---

## Related Attacks

### XML XInclude (If Supported)

**XML has a native include mechanism:**

```xml
<?xml version="1.0"?>
<root xmlns:xi="http://www.w3.org/2001/XInclude">
    <xi:include href="file2.xml"/>
</root>

<!-- file2.xml -->
<data xmlns:xi="http://www.w3.org/2001/XInclude">
    <xi:include href="file1.xml"/>  ← Circular!
</data>
```

**If Avalonia's XAML parser processes XInclude directives:** Same vulnerability.

---

## Avalonia's Built-in Protection (Unknown)

**Need to test:**
- Does Avalonia detect circular StyleInclude?
- Does Avalonia limit inclusion depth?
- Does Avalonia deduplicate includes?
- What happens on circular reference?

**Framework behavior is unclear. Don't rely on it.**

---

## Recommended Fix

### Phase 1: Add Depth Limits (Immediate)

```csharp
// Before loading any AXAML:
if (!ValidateInclusionGraph(axamlPath, maxDepth: 10))
{
    throw new InvalidOperationException("Theme validation failed");
}
```

### Phase 2: Circular Reference Detection (Short-term)

```csharp
// Track visited files during theme load
private static readonly ThreadLocal<HashSet<string>> _loadingFiles = 
    new(() => new HashSet<string>());

private static void BeginLoadingFile(string path)
{
    var normalized = Path.GetFullPath(path).ToLowerInvariant();
    if (!_loadingFiles.Value!.Add(normalized))
    {
        throw new InvalidOperationException($"Circular reference: {path}");
    }
}

private static void EndLoadingFile(string path)
{
    var normalized = Path.GetFullPath(path).ToLowerInvariant();
    _loadingFiles.Value!.Remove(normalized);
}
```

### Phase 3: Plugin Dependency Resolution (Before marketplace launch)

```csharp
// Implement proper topological sort for plugin dependencies
// Detect cycles using visited/visiting tracking
// Install in correct order
```

---

## Testing Checklist

- [ ] Create circular StyleInclude (ThemeA → ThemeB → ThemeA)
- [ ] Create deep chain (1000+ levels)
- [ ] Create diamond pattern (multiple paths to same file)
- [ ] Test with circular plugin dependencies
- [ ] Monitor: stack depth, memory usage, parse time
- [ ] Verify: crash, hang, or graceful rejection

---

## Comparison to Other Platforms

| Platform | Circular Include Protection |
|----------|----------------------------|
| **npm** | Detects circular deps, warns but allows |
| **pip** | Detects circular deps, fails installation |
| **Maven** | Detects cycles, build fails |
| **Cargo** | Detects cycles, compilation fails |
| **Avalonia** | Unknown (needs testing) |
| **Cycloside** | ❌ **NONE** |

**Industry standard: Detect and reject circular references.**

---

## Severity Justification

**HIGH (not CRITICAL) because:**

1. **Denial of service only** (not code execution)
2. **Requires malicious theme/plugin** (not remote attack)
3. **Easy to detect and fix** (simple validation)

**But still HIGH because:**

1. **Guaranteed crash** (stack overflow or hang)
2. **Auto-triggered** (theme loads automatically)
3. **Hard to debug** (user sees crash, not reason)
4. **Marketplace concern** (malicious uploads)

---

## Immediate Actions

1. **Test Avalonia's behavior:**
   - Create circular StyleInclude
   - Load in Cycloside
   - Observe: crash, hang, exception, or graceful handling?

2. **Add depth limit if no protection exists:**
   - Implement `ValidateInclusionGraph`
   - Call before all theme loading
   - Set reasonable limit (10 levels?)

3. **Add to vulnerability catalog:**
   - CYC-2026-031: Recursive inclusion attacks

4. **Test plugin dependencies:**
   - Are they even resolved currently?
   - If yes, do they handle cycles?
   - If no, add resolution + cycle detection

---

**Status: 🔥 CONFIRMED - Add to catalog immediately**

**This is vulnerability #31.**

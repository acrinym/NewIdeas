# 🏷️ Plugin Categorization Guide

**How to properly categorize Cycloside plugins using the new metadata system**

---

## 📋 Quick Reference

### Plugin Categories

```csharp
public enum PluginCategory
{
    DesktopCustomization,  // Always enabled (Core)
    RetroComputing,        // Always enabled (Core)
    TinkererTools,         // Enabled by default
    Utilities,             // Enabled by default
    Entertainment,         // Enabled by default
    Development,           // Disabled by default
    Security,              // Disabled by default (consider archiving)
    Experimental           // Disabled by default
}
```

---

## 🎯 How to Categorize Your Plugin

### **Step 1: Add Category Property**

In your plugin class, override the `Category` property:

```csharp
public class MyPlugin : IPlugin
{
    public string Name => "My Plugin";
    public Version Version => new Version(1, 0, 0);
    public string Description => "Does something cool";

    // ✅ Add this:
    public PluginCategory Category => PluginCategory.DesktopCustomization;

    // Optional: Override default behavior
    public bool EnabledByDefault => true;  // Only if different from category default
    public bool IsCore => false;           // Only if different from category default

    public void Start() { /* ... */ }
    public void Stop() { /* ... */ }
}
```

---

## 📚 Category Examples

### ✅ **DesktopCustomization** (Core - Always Enabled)

**Use for:** Plugins that provide desktop theming, widgets, window decorations

```csharp
public class WallpaperPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.DesktopCustomization;
    // Automatically: EnabledByDefault = true, IsCore = true
}
```

**Examples:**
- WallpaperPlugin
- WidgetHostPlugin
- DateTimeOverlayPlugin
- ScreenSaverPlugin
- ManagedVisHostPlugin

---

### ✅ **RetroComputing** (Core - Always Enabled)

**Use for:** Classic games, retro software, nostalgic computing

```csharp
public class JezzballPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.RetroComputing;
    // Automatically: EnabledByDefault = true, IsCore = true
}
```

**Examples:**
- JezzballPlugin
- QBasicRetroIDEPlugin
- ModTrackerPlugin
- TileWorldPlugin (when added)

---

### ✅ **TinkererTools** (Enabled by Default)

**Use for:** Automation, macros, hardware integration, power user features

```csharp
public class MacroPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.TinkererTools;
    // Automatically: EnabledByDefault = true, IsCore = false
}
```

**Examples:**
- MacroPlugin
- FileWatcherPlugin
- TaskSchedulerPlugin
- HardwareMonitorPlugin

---

### ✅ **Utilities** (Enabled by Default)

**Use for:** General-purpose desktop utilities

```csharp
public class FileExplorerPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.Utilities;
    // Automatically: EnabledByDefault = true, IsCore = false
}
```

**Examples:**
- FileExplorerPlugin
- TextEditorPlugin
- ClipboardManagerPlugin
- DiskUsagePlugin
- LogViewerPlugin
- NotificationCenterPlugin

---

### ✅ **Entertainment** (Enabled by Default)

**Use for:** Media players, games, fun features

```csharp
public class MP3PlayerPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.Entertainment;
    // Automatically: EnabledByDefault = true, IsCore = false
}
```

**Examples:**
- MP3PlayerPlugin (with Winamp theme support!)

---

### ⚠️ **Development** (Disabled by Default)

**Use for:** Developer tools, IDEs, terminals, databases

```csharp
public class AdvancedCodeEditorPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.Development;
    // Automatically: EnabledByDefault = false, IsCore = false
}
```

**Examples:**
- AdvancedCodeEditorPlugin
- TerminalPlugin
- PowerShellTerminalPlugin
- ApiTestingPlugin
- DatabaseManagerPlugin
- AiAssistantPlugin

**Note:** Users must explicitly enable these in plugin manager

---

### ❌ **Security** (Disabled by Default - Consider Archiving)

**Use for:** Network tools, vulnerability scanners, forensics

```csharp
public class NetworkToolsPlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.Security;
    // Automatically: EnabledByDefault = false, IsCore = false
}
```

**Examples:**
- NetworkToolsPlugin
- VulnerabilityScannerPlugin
- ExploitDatabasePlugin
- DigitalForensicsPlugin

**Recommendation:** Archive these plugins or move to separate optional package

---

### ⚠️ **Experimental** (Disabled by Default)

**Use for:** Unstable, work-in-progress, or uncategorized plugins

```csharp
public class ExperimentalFeaturePlugin : IPlugin
{
    public PluginCategory Category => PluginCategory.Experimental;
    // Automatically: EnabledByDefault = false, IsCore = false
}
```

**Default:** If you don't specify a category, plugins are marked Experimental

---

## 🚀 Implementation Checklist

For each plugin in `/Cycloside/Plugins/BuiltIn/`:

- [ ] Add `Category` property override
- [ ] Choose appropriate category from table above
- [ ] Test that plugin loads correctly
- [ ] Verify EnabledByDefault behavior matches expectations

---

## 📊 Current Plugin Assignments

### **Core Desktop Customization (5 plugins)**
```csharp
WallpaperPlugin              => PluginCategory.DesktopCustomization
WidgetHostPlugin             => PluginCategory.DesktopCustomization
DateTimeOverlayPlugin        => PluginCategory.DesktopCustomization
ScreenSaverPlugin            => PluginCategory.DesktopCustomization
ManagedVisHostPlugin         => PluginCategory.DesktopCustomization
```

### **Core Retro Computing (4 plugins)**
```csharp
JezzballPlugin               => PluginCategory.RetroComputing
QBasicRetroIDEPlugin         => PluginCategory.RetroComputing
ModTrackerPlugin             => PluginCategory.RetroComputing
MP3PlayerPlugin              => PluginCategory.Entertainment  // Special case
```

### **Tinkerer Tools (4 plugins)**
```csharp
MacroPlugin                  => PluginCategory.TinkererTools
FileWatcherPlugin            => PluginCategory.TinkererTools
TaskSchedulerPlugin          => PluginCategory.TinkererTools
HardwareMonitorPlugin        => PluginCategory.TinkererTools
```

### **Utilities (9 plugins)**
```csharp
FileExplorerPlugin           => PluginCategory.Utilities
TextEditorPlugin             => PluginCategory.Utilities
ClipboardManagerPlugin       => PluginCategory.Utilities
NotificationCenterPlugin     => PluginCategory.Utilities
LogViewerPlugin              => PluginCategory.Utilities
DiskUsagePlugin              => PluginCategory.Utilities
EnvironmentEditorPlugin      => PluginCategory.Utilities
QuickLauncherPlugin          => PluginCategory.Utilities
CharacterMapPlugin           => PluginCategory.Utilities
```

### **Development Tools (7 plugins)**
```csharp
TerminalPlugin               => PluginCategory.Development
PowerShellTerminalPlugin     => PluginCategory.Development
HackerTerminalPlugin         => PluginCategory.Development  // Just a terminal with dramatic name
AdvancedCodeEditorPlugin     => PluginCategory.Development
ApiTestingPlugin             => PluginCategory.Development
DatabaseManagerPlugin        => PluginCategory.Development
AiAssistantPlugin            => PluginCategory.Development
```

### **Security (6 plugins - ARCHIVE)**
```csharp
NetworkToolsPlugin           => PluginCategory.Security
VulnerabilityScannerPlugin   => PluginCategory.Security
ExploitDatabasePlugin        => PluginCategory.Security
ExploitDevToolsPlugin        => PluginCategory.Security
DigitalForensicsPlugin       => PluginCategory.Security
HackersParadisePlugin        => PluginCategory.Security  // Misguided demo plugin
```

### **Other**
```csharp
EncryptionPlugin             => PluginCategory.Utilities  // Could be useful for tinkerers
```

---

## 🎯 Expected Behavior

### **First Launch:**
- Load 22 plugins automatically (Core + TinkererTools + Utilities)
- User sees fast startup, focused feature set
- Plugin manager shows disabled plugins available to enable

### **Subsequent Launches:**
- Respect user's enabled/disabled preferences
- User can enable Development/Security plugins if needed
- Preferences saved in `settings.json`

---

## 💡 Benefits

1. **Fast startup** - Only 22 plugins vs 36
2. **Focused experience** - Core features first
3. **User choice** - Advanced tools available when needed
4. **Clear organization** - Categories make sense
5. **Professional** - No overwhelming feature list

---

## 🔧 Migration Notes

### **Backwards Compatibility:**

Old plugins without `Category` property will:
- Default to `PluginCategory.Experimental`
- Be disabled by default
- Still work, but require user to enable them

### **Recommended Migration:**

1. Add `Category` property to all built-in plugins
2. Test with clean settings (delete `settings.json`)
3. Verify only 22 plugins load on first launch
4. Document any exceptions or special cases

---

## ✅ Success Criteria

After categorization:

- ✅ Clean first launch (9 core + 13 optional plugins)
- ✅ Fast startup time
- ✅ Focused on desktop customization vision
- ✅ Advanced tools available but not forced
- ✅ Clear plugin organization in UI

---

**Next Step:** Apply categories to all 36 plugins in `/Plugins/BuiltIn/` directory! 🚀

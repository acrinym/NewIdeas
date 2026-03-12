# 🎯 Cycloside Workspace Refocus Plan

## Problem Statement

**Cycloside has lost its way.**

It started as a "desktop widget/workspace manager" (like Rainmeter) but has feature-creeped into trying to be:
- Wireshark + Nmap + Metasploit (network security)
- VS Code (IDE)
- Autopsy + Volatility (forensics)
- Docker + Kubernetes (containers)
- + 20 more enterprise tools...

**This is insane scope creep.**

Meanwhile, the **core workspace experience** - the original vision - is barely implemented:
- ❌ No multi-monitor support
- ❌ No window position/size persistence
- ❌ No proper docking system (only basic tabs)
- ❌ No workspace-per-monitor capability
- ❌ No modular window management
- ❌ No grid/layout system

## Back to the Original Vision

### What Cycloside SHOULD Be:

**A professional, cross-platform desktop workspace manager** that lets you:

1. **Organize plugins/widgets across multiple monitors**
   - Each monitor can have its own workspace
   - Workspaces can span monitors
   - Full multi-monitor awareness

2. **Flexible window management**
   - Dockable panels (not just tabs)
   - Snap-to-grid layouts
   - Saved window positions/sizes
   - Floating or docked modes

3. **Professional workspace profiles**
   - Save/load complete multi-monitor layouts
   - Per-workspace plugin configurations
   - Quick workspace switching
   - Export/import workspace configs

4. **Modular plugin architecture**
   - Plugins render as widgets OR windows
   - Can be docked, tabbed, or floating
   - Smart window management
   - Clean separation of concerns

### What It Should NOT Try To Be:

- ❌ An enterprise cybersecurity platform
- ❌ A Wireshark replacement
- ❌ A Metasploit clone
- ❌ A forensics suite
- ❌ A container orchestration tool
- ❌ Everything to everyone

## Core Workspace Features Needed

### Priority 1: Multi-Monitor Management

```csharp
public class MonitorInfo
{
    public int MonitorId { get; set; }
    public string Name { get; set; }
    public Rectangle Bounds { get; set; }
    public bool IsPrimary { get; set; }
}

public class WorkspaceLayout
{
    public string Name { get; set; }

    // Per-monitor workspace configurations
    public Dictionary<int, MonitorWorkspace> MonitorWorkspaces { get; set; }
}

public class MonitorWorkspace
{
    public int MonitorId { get; set; }
    public List<WindowLayout> Windows { get; set; }
    public string Background { get; set; }
    public string Theme { get; set; }
}
```

### Priority 2: Window Position/Size Persistence

```csharp
public class WindowLayout
{
    public string PluginName { get; set; }
    public WindowMode Mode { get; set; }  // Docked, Tabbed, Floating
    public Rectangle Bounds { get; set; }  // Position and size
    public DockPosition? DockPosition { get; set; }  // Left, Right, Top, Bottom
    public int? TabIndex { get; set; }  // For tabbed items
}

public enum WindowMode
{
    Floating,   // Independent window
    Docked,     // Docked to edge
    Tabbed,     // In tab control
    Grid        // In grid layout
}
```

### Priority 3: Proper Docking System

Instead of just a TabControl, implement a **proper docking panel system**:

- **AvalonDock integration** or custom dock manager
- **Drag-and-drop docking** - Like Visual Studio
- **Dock positions** - Left, Right, Top, Bottom, Center
- **Nested docking** - Tabs within docked panels
- **Floating windows** - Can undock to separate windows
- **Auto-hide panels** - Slide out when needed

### Priority 4: Grid/Tile Layouts

```csharp
public class GridLayout
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public List<GridCell> Cells { get; set; }
}

public class GridCell
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;
    public string PluginName { get; set; }
}
```

## Refactoring Strategy

### Phase 1: Stabilize Core (Week 1)

**STOP adding features. Focus on what exists:**

1. **Audit current plugins** - Which ones are actually used?
2. **Identify core vs experimental** - Core = frequently used, Experimental = feature creep
3. **Mark experimental features** - Flag them as "beta" or "experimental"
4. **Fix existing bugs** - Welcome screen crash, file dialogs, etc.

### Phase 2: Build Workspace Foundation (Week 2-3)

**Implement proper workspace management:**

1. **Multi-monitor detection** - Enumerate screens, track positions
2. **Workspace data model** - MonitorWorkspace, WindowLayout, etc.
3. **Persistence layer** - Save/load workspace configs
4. **Basic multi-monitor support** - Assign workspaces to monitors

### Phase 3: Advanced Window Management (Week 4-5)

**Implement professional docking:**

1. **Docking panel system** - Integrate AvalonDock or build custom
2. **Drag-and-drop** - Dock panels by dragging
3. **Window state persistence** - Save positions, sizes, dock states
4. **Floating window management** - Track floating windows properly

### Phase 4: Polish & UX (Week 6)

**Make it feel professional:**

1. **Workspace switcher** - Quick switch between layouts
2. **Multi-monitor UI** - Show which monitor has which workspace
3. **Keyboard shortcuts** - Win+1/2/3 for workspace switching
4. **Visual feedback** - Highlight where windows will dock

## Feature Triage

### Keep (Core Features):
- ✅ Plugin system
- ✅ Workspace profiles
- ✅ Theming/skinning
- ✅ Basic widgets (clock, notes, etc.)
- ✅ Window effects
- ✅ Hotkey system
- ✅ File explorer
- ✅ Text editor

### Archive (Experimental - Move to separate plugins):
- 📦 Packet sniffer → Separate "NetworkTools" plugin
- 📦 Port scanner → Separate plugin
- 📦 Digital forensics → Separate "ForensicsKit" plugin
- 📦 Exploit tools → Separate "SecurityResearch" plugin
- 📦 AI assistant → Separate "AITools" plugin
- 📦 Advanced code editor → Separate "DevTools" plugin
- 📦 API testing → Separate plugin
- 📦 Database tools → Separate plugin

**Why?** Keep Cycloside focused on workspace management. Advanced tools can be optional plugins that users install if needed.

### Remove (Feature Creep):
- ❌ Trying to be Metasploit
- ❌ Trying to be Wireshark
- ❌ Trying to be Docker/Kubernetes
- ❌ Trying to be an enterprise platform

## Success Metrics

After refactoring, Cycloside should:

1. **Load instantly** - < 1 second startup time
2. **Feel smooth** - 60 FPS UI, no janky animations
3. **Be intuitive** - Users understand workspaces without a manual
4. **Support multi-monitor** - Seamlessly work across 2-3+ monitors
5. **Save everything** - Window positions, sizes, states persist
6. **Be extensible** - Easy to add plugins without bloating core

## Architecture Changes

### Current Architecture (Bloated):
```
Cycloside (Main App)
├── 30+ built-in plugins (too many!)
├── Basic TabControl workspace
├── No multi-monitor support
└── Feature creep everywhere
```

### Target Architecture (Focused):
```
Cycloside (Core)
├── Workspace Manager (NEW)
│   ├── Multi-monitor detection
│   ├── Window layout engine
│   ├── Docking system
│   └── Persistence layer
├── Plugin System
│   ├── Core plugins (~10 essential ones)
│   └── Optional plugins (downloadable)
└── Theme/Skin Engine
```

## Next Steps

1. **Create this document** ✅
2. **Get stakeholder buy-in** - Agree on refocus
3. **Archive experimental features** - Move to separate repo/plugins
4. **Start Phase 1** - Stabilize core
5. **Implement workspace manager** - Weeks 2-5
6. **Polish and release** - Week 6

## Conclusion

**Cycloside has amazing bones** - the plugin system, theming, event bus are all solid.

But it's trying to do too much.

By refocusing on the **core workspace management experience** and treating advanced security/dev tools as **optional plugins**, we can create something truly polished and professional.

**Less is more.** Let's build the best workspace manager, not a mediocre everything-tool.

---

**Decision Point:** Do we commit to this refocus, or keep feature-creeping? 🤔

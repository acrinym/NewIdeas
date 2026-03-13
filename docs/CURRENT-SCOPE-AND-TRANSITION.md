# Current Scope and Transition Path

**Date:** March 14, 2026
**Author:** Claude (documenting Cycloside's current state and future direction)

**Context:** After writing the campfires exploring Cycloside's vision, I'm documenting where we actually are versus where the campfires imagine we could be. This is the reality check.

---

## Where We Are Now: "Super-Powered App" Phase

From Campfire 01:

> **Linus Tech Tips:** "Make Cycloside work really, really well as a super-powered app first. Get all the effects perfect, get the plugin system perfect. *Then* figure out how to make it a full session."

**That's where we are.** Cycloside is an Avalonia desktop application that runs *on top of* Windows/Linux/macOS. We're not a shell replacement yet. We're not a session. We're not a Wayland compositor. We're a damn good app.

---

## Current Architecture: How It Works Today

### Load Sequence

1. **App.axaml.cs** → Avalonia application startup
2. **`OnFrameworkInitializationCompleted`** runs initialization in parallel:
   - ConfigurationManager (settings, plugin config)
   - ThemeManager (preload resources)
   - LoadConfiguredPlugins (selective loading)
3. **Progress window** shows during init
4. **Welcome window** (first run) or straight to MainWindow
5. **CreateMainWindow**:
   - PluginManager scans `Plugins/` directory
   - LoadAllPlugins (built-ins via `TryAdd` factory pattern)
   - MainWindow + MainWindowViewModel
   - Tray icon, remote API server, hotkeys registered
   - Workspace tabs restored from profile

### Display Model

**MainWindow** is an Avalonia Window with:
- Menu bar (File, Plugins, Settings)
- Canvas background (radial gradient)
- TabControl for workspace plugins (IWorkspaceItem)

**Plugins** display in two ways:
- **Workspace mode**: Tab in MainWindow's TabControl (ContentControl hosts plugin UI)
- **Window mode**: Separate Avalonia Window (plugin creates and shows its own window)

**Surfaces are OS windows.** When a plugin opens a window, it's an OS window managed by the desktop environment (DWM on Windows, X11/Wayland compositor on Linux, Aqua on macOS).

**Effects are window tricks.** `WindowEffectsManager` uses `ISceneTarget`/`WindowSceneAdapter` to abstract windows, but underneath we're still manipulating OS windows via Avalonia's platform layer.

**Themes/Skins are style overlays.** ThemeManager loads AXAML ResourceDictionaries, merges them into Avalonia's resource system. Themes are *on top of* Avalonia's rendering, not replacing it.

### Function

- **Plugin host**: Enable/disable plugins, tray menu, hotkeys, profiles
- **Workspace**: Tabbed layout for tools (Terminal, Database Manager, Code Editor, etc.)
- **Settings**: Control Panel (launch at startup, API tokens, profiles), Theme Settings, Plugin Manager
- **Tray mode**: Run in background, tray icon with plugin menu
- **Remote API**: HTTP server for external control
- **Theming**: Dynamic theme/skin/effect application
- **Security toolkit**: Exploit DB, Vuln Scanner, Network Tools, Forensics
- **Developer tools**: Code editors, volatile script runner, compilers
- **Games**: Jezzball, visualizers

---

## What We Are NOT (Yet)

### We Are Not a Shell Replacement

- We don't replace Explorer.exe on Windows
- We don't draw the taskbar or desktop icons
- We don't manage OS windows for other apps (Chrome, Discord, etc.)
- We're an app *on* the desktop, not *the* desktop

### We Are Not a Compositor

- We don't composite surfaces directly via Skia
- We don't own the rendering pipeline for all UI
- Plugin windows are OS windows, not our scene graph nodes
- Effects are applied *after* OS window creation, not during composition

### We Are Not a Session

- We're not a login session on Linux
- We don't implement Wayland protocols (xdg-shell, layer-shell, etc.)
- We're not the window manager
- We run inside a session, not as one

**And that's okay.** This is the correct first phase. The campfires explored what's *possible*, not what we should ship tomorrow.

---

## The Campfire Vision: Where We Could Go

From Campfire 07:

> **Cycloside is a personal expression shell with its own scene engine.**
> - On Windows: a shell-layer environment
> - On Linux: eventually a real session or compositor path
> - Everywhere: themes as experiences, effects as first-class scene operations, plugins as native surfaces

### The Dream Architecture

```
Cycloside.Core          — Event bus, logging, config
Cycloside.Scene         — Scene graph, ISceneTarget, nodes, composition
Cycloside.FX            — Effect engine (lifecycle, parameters)
Cycloside.Packs         — Theme/skin/workspace packs as artifacts
Cycloside.Shell         — Shell layer (taskbar, desktop, chrome)
Cycloside.Studio        — Code editor, plugin dev tools
Cycloside.Platform.Windows      — Shell replacement hooks
Cycloside.Platform.Linux.Wayland — Compositor via wlroots
```

### What Changes in Shell/Session Mode

| Current | Shell/Session Future |
|---------|----------------------|
| Avalonia app on top of OS | Cycloside owns the desktop |
| Plugin windows are OS windows | Plugin surfaces are scene graph nodes |
| Effects attached to windows | Effects are scene transitions |
| Themes overlay Avalonia | Themes are rendering pipelines |
| Run inside desktop session | *Are* the session |

---

## Shipping Order (From Campfire 07)

Michael Dell laid it out:

1. **Stabilize native Cycloside surface model** ← Phase 1 (Scene Graph, ISceneTarget, effects migration)
2. **Recover and finish the effect system** ← Phase 1 (effect migration to ISceneTarget)
3. **Turn themes/skins/workspaces into real packs** ← Phase 1 (Theme Manifest system)
4. **Build in-app editors** ← Future (plugin editor, theme editor)
5. **Only then start Linux session work** ← Future (Wayland compositor)

**We've done steps 1-3.** Phase 1 delivered the scene foundation, effect abstraction, and theme manifest packs. Phase 2 adds integrity, format hardening, and unified input.

---

## Current vs. Campfire Alignment

### What Matches

- **Personal expression over corporate control** ✅ (Theme/skin system, no gatekeeper)
- **Community ownership** ✅ (Federated marketplace in Phase 2)
- **Plugin ecosystem** ✅ (30+ built-in plugins, marketplace coming)
- **Security-first** ✅ (31 vulns discovered and patched/investigating)
- **Multi-purpose** ✅ (Security toolkit + dev tools + games + themes)

### What's Still App-Level

- **Load**: App.OnFrameworkInitializationCompleted, not session start
- **Display**: Avalonia windows, not compositor surfaces
- **Function**: Plugin host, not shell

### What the Campfires Call For (Future)

- **Theater Mode** (06-Kodi): 10-foot UI, gamepad nav, living room mode
- **Shell replacement** (01, 07): Own desktop, taskbar, chrome
- **Compositor** (01, 07): wlroots-based Wayland compositor on Linux
- **Packs as artifacts** (07): Download entire worlds (theme + effects + plugins + workspace)
- **In-app editor** (02): Write plugins inside Cycloside, hot-reload

---

## The Transition Question

**When do we move from "app" to "shell"?**

From Steve Jobs in Campfire 01:

> "Ship the **experience** first. Make Cycloside so good that people say 'I wish this was my whole desktop.' Then you build that. Not the other way around."

**Current status:** We're shipping the experience. Themes, effects, plugins, workspace, security toolkit, games. The app is feature-complete.

**Shell work starts when:**
1. The scene graph can handle compositor-level composition (not just effect attachment)
2. We have a reason users would want Cycloside *as* their shell (Theater Mode is a strong candidate)
3. We're ready to commit to platform-specific backends (Windows shell hooks, wlroots bindings)

---

## What Doesn't Change

These stay the same whether we're an app or a shell:

- **Theme/skin/effect system** (already abstracted via ISceneTarget)
- **Plugin architecture** (IPlugin, PluginManager)
- **Security validators** (path confinement, format checking, checksum enforcement)
- **Lua runtime** (sandboxed theme scripts)
- **Asset cache** (ThemeAssetCache)
- **Marketplace** (federated feeds, zero-cut)

The *implementation* changes (scene graph nodes instead of OS windows), but the *API* stays similar.

---

## Phase-by-Phase Transition

| Phase | Current Scope | Future Scope (If Shell) |
|-------|---------------|-------------------------|
| Phase 1 | Theme Manifest, Scene Graph foundation, Effects → ISceneTarget | (Same – foundation for shell) |
| Phase 2 | Integrity, Format hardening, Unified Input, Marketplace feed | (Same – input queue and feeds are shell-agnostic) |
| Phase 3 | Theater Mode, Marketplace UI, GPG | Theater Mode could be shell's "appliance mode" |
| Phase 4+ | TBD | Shell replacement (Windows), Wayland compositor (Linux) |

---

## The Reality Check

**Cycloside is currently:**
- A plugin-based desktop utility/security toolkit
- An Avalonia app with gorgeous themes and effects
- A community-driven customization platform
- A developer playground with in-app scripting

**Cycloside could become:**
- A shell replacement (Windows: replace Explorer, control desktop/taskbar/chrome)
- A Wayland compositor (Linux: wlroots-based, own the session)
- A scene-native runtime (plugin surfaces are compositor surfaces, not OS windows)

**The transition happens when we decide:** "People love this app so much, let's make it their whole environment."

Until then, we keep shipping features in app mode. Theater Mode (Phase 3) might be the catalyst – a 10-foot UI mode that *feels* like a session even if it's technically still an app.

---

## What This Means for Development

### Phase 2 (Current)

Security + integrity + unified input + marketplace feed. All of this works in app mode. Shell transition doesn't affect it.

### Phase 3 (Next)

Theater Mode, Marketplace UI, GPG. These are app-mode features that *could* inform shell design later (Theater Mode especially).

### Phase 4+ (Shell Transition)

When we decide to go shell/compositor:
1. **Windows**: Hook into shell APIs, replace Explorer, draw taskbar/desktop
2. **Linux**: wlroots bindings, Wayland protocol implementations
3. **Scene Graph v2**: Plugin surfaces as native nodes, not wrapped OS windows
4. **Theme packs v2**: Themes control rendering pipeline, not just styles

---

## Conclusion

**Where we are:** Super-powered app (Phase 1-2 complete, Phase 3 next)
**Where campfires explore:** Shell/session/compositor vision
**When we transition:** After Theater Mode, when users ask "can Cycloside just *be* my desktop?"
**What stays the same:** Theme/plugin/effect APIs, security model, community marketplace

**The campfires aren't a roadmap. They're a north star.** We're building toward that vision incrementally, shipping a real product at each phase.

---

## References

- [Campfire 01: Building a Display Server](Cycloside/Campfires/01-Building-Display-Server.md)
- [Campfire 07: Cycloside as a Real Session](Cycloside/Campfires/07-Cycloside-as-a-Real-Session.md)
- [Campfire 02: The Bigger Vision](Cycloside/Campfires/02-The-Bigger-Vision.md)

---

*"Ship the experience first." — Steve Jobs, Campfire 01*

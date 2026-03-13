# Phase 4: Creator Studio & Shell Foundations

**Date:** 2026-03-14  
**Status:** Planned  
**Campfire Alignment:** 04-Anti-Store Phase 4, 05-WebTV Phase 4, 07-Shell Shipping Order

---

## Vision

Phase 4 transitions Cycloside from **consumer platform** to **creator platform**. Users can now build, sign, and publish their own themes, skins, and plugins. The shell architecture foundations are laid for future session work.

**Not Yet:** Full Wayland compositor or shell replacement (that's Phase 5+)  
**This Phase:** The tools, architecture, and patterns that make Cycloside self-modifying

---

## Workstreams

### Workstream 1: Creator Studio (04-Anti-Store Phase 4)

**Goal:** Enable creators to build and publish plugins/themes/packs without external tools

**Features:**
- **Plugin Submission Tool**
  - GUI for plugin metadata (name, description, version, author)
  - Dependency declaration
  - Automatic manifest generation
  - Checksum generation (SHA-256)
  - Validation before submission

- **Signing Tool for Creators**
  - GPG key management UI
  - Sign plugin/theme packages
  - Verify signatures
  - Revoke compromised keys

- **Creator Dashboard**
  - Download stats
  - User reviews/ratings
  - Version history
  - Update notifications
  - Earnings (if tip jar enabled)

**Deliverables:**
- `Cycloside/Studio/PluginSubmissionWindow.axaml[.cs]`
- `Cycloside/Studio/SigningToolWindow.axaml[.cs]`
- `Cycloside/Studio/CreatorDashboardWindow.axaml[.cs]`
- `Cycloside/Services/CreatorService.cs`
- `Cycloside/Services/SigningService.cs` (GPG integration)
- `docs/creator-guide.md`
- `docs/signing-howto.md`

**Campfire Source:** [04-Anti-Store-Manifesto.md Phase 4](Cycloside/Campfires/04-Anti-Store-Manifesto.md)

---

### Workstream 2: In-App Editors (07-Shell Shipping Order Step 4)

**Goal:** Let users modify Cycloside from *inside* Cycloside

**Features:**
- **Theme Editor (Enhanced)**
  - Visual token editor (colors, fonts, spacing)
  - Live preview
  - AXAML syntax highlighting
  - Export as theme pack

- **Lua Script Editor**
  - Syntax highlighting (Lua)
  - Autocomplete for Cycloside API
  - Live reload
  - Debugging console

- **Pack Builder**
  - Combine theme + cursor + sounds + effects + plugins
  - Generate pack manifest
  - One-click publish to marketplace

**Deliverables:**
- `Cycloside/Studio/ThemeEditorWindow.axaml[.cs]` (enhance existing)
- `Cycloside/Studio/LuaScriptEditorWindow.axaml[.cs]`
- `Cycloside/Studio/PackBuilderWindow.axaml[.cs]`
- `Cycloside/Services/PackService.cs`
- `docs/pack-format.md`

**Campfire Source:** [07-Cycloside-as-a-Real-Session.md](Cycloside/Campfires/07-Cycloside-as-a-Real-Session.md) - "make the editor first-class"

---

### Workstream 3: Shell Architecture Prep (07-Shell Foundations)

**Goal:** Lay groundwork for future shell/session work without building full compositor

**Features:**
- **Workspace Model**
  - Define workspace as first-class concept
  - Workspace profiles (layout + plugins + theme)
  - Switch workspaces with transition
  - Save/load workspace state

- **Surface Ownership**
  - `Cycloside.Scene` refactor: every UI element = scene node
  - Plugin windows become scene surfaces
  - Widgets become scene surfaces
  - Effects operate on scene nodes (not Avalonia `Window` hacks)

- **Platform Abstraction**
  - `Cycloside.Platform.Windows` - Windows shell hooks
  - `Cycloside.Platform.Linux.X11` - X11 window manager stub
  - `Cycloside.Platform.Linux.Wayland` - Wayland compositor stub (not implemented)

**Deliverables:**
- `Cycloside/Scene/Workspace.cs`
- `Cycloside/Scene/WorkspaceManager.cs`
- `Cycloside/Scene/Surface.cs` (base for all UI)
- `Cycloside/Platform/` folder structure
- `Cycloside/Platform/Windows/ShellHooks.cs` (placeholder)
- `Cycloside/Platform/Linux/X11/WindowManagerStub.cs` (placeholder)
- `docs/shell-architecture.md`

**Campfire Source:** [07-Cycloside-as-a-Real-Session.md](Cycloside/Campfires/07-Cycloside-as-a-Real-Session.md) - shipping order steps 1-3

---

### Workstream 4: WebTV Phase 3-4 (Screen Management)

**Goal:** Complete WebTV-inspired UI patterns

**Features:**
- **Screen Transitions** (05-WebTV Phase 4)
  - Read `UserInterface/Screen.c` from WebTV source
  - Extract transition patterns (slide, fade, zoom)
  - Implement as scene graph transitions
  - Document layout management

- **Focus Ring Visual**
  - Gamepad focus indicator (05-WebTV Phase 2)
  - Customizable via theme
  - Smooth animations

- **Sound Effects** (05-WebTV Phase 3)
  - UI action sounds (focus, select, back, error)
  - Theme-specific sound packs
  - MIDI integration for custom sounds

**Deliverables:**
- `Cycloside/Scene/Transitions/` folder
  - `SlideTransition.cs`
  - `FadeTransition.cs`
  - `ZoomTransition.cs`
- `Cycloside/Input/FocusRing.cs`
- `Cycloside/Services/SoundEffectsManager.cs`
- `docs/screen-transitions.md`

**Campfire Source:** [05-WebTV-Source-Reconnaissance.md Phase 4](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md)

---

### Workstream 5: Theater Mode Phase 2 (06-Kodi Q3-Q4)

**Goal:** Full Theater Mode implementation (10-foot UI)

**Features:**
- **Dashboard Screen**
  - Widget grid (weather, clock, now playing)
  - Game launcher tiles
  - Plugin shortcuts
  - Phoenix Visualizer background

- **Big Picture Navigation**
  - Gamepad-optimized menus
  - Large fonts/targets
  - Couch-readable from 10ft
  - Remote control mapping

- **Quick Settings**
  - Theme selector
  - Volume/brightness
  - Display settings
  - Exit to desktop

**Deliverables:**
- `Cycloside/TheaterMode/DashboardWindow.axaml[.cs]`
- `Cycloside/TheaterMode/BigPictureMenu.axaml[.cs]`
- `Cycloside/TheaterMode/QuickSettingsWindow.axaml[.cs]`
- `Cycloside/Services/TheaterModeManager.cs`
- `docs/theater-mode-guide.md`

**Campfire Source:** [06-Kodi-vs-Cycloside-Theater-Mode.md Roadmap Q3-Q4](Cycloside/Campfires/06-Kodi-vs-Cycloside-Theater-Mode.md)

---

## Phase 4 Scope Summary

| Workstream | Campfire | Effort | Priority |
|------------|----------|--------|----------|
| Creator Studio | 04 Phase 4 | Large | High |
| In-App Editors | 07 Step 4 | Large | High |
| Shell Architecture Prep | 07 Steps 1-3 | Medium | Critical |
| WebTV Screen Management | 05 Phase 4 | Small | Medium |
| Theater Mode Full | 06 Q3-Q4 | Large | High |

**Total Estimated Scope:** Similar to Phase 2 (5 workstreams, mix of features + architecture)

---

## Validation Checklist

**After Phase 4 Complete:**
- [ ] Creator can build, sign, and publish plugin from within Cycloside
- [ ] User can edit theme live and see changes immediately
- [ ] Pack builder can combine theme + sounds + cursor + plugins
- [ ] Workspace model works (save/load/switch)
- [ ] Scene graph owns all surfaces (no more Avalonia `Window` hacks)
- [ ] Platform abstraction exists (even if stubs)
- [ ] Screen transitions work (slide/fade/zoom)
- [ ] Theater Mode dashboard functional with gamepad
- [ ] Focus ring visual renders correctly
- [ ] UI sound effects play (theme-specific)

---

## Documentation Debt

**New Docs Required:**
- `docs/creator-guide.md` - How to build and publish
- `docs/signing-howto.md` - GPG signing for creators
- `docs/pack-format.md` - Pack manifest schema
- `docs/shell-architecture.md` - Platform abstraction design
- `docs/screen-transitions.md` - Transition API
- `docs/theater-mode-guide.md` - Using Theater Mode

**Updated Docs:**
- `docs/theming-skinning.md` - In-app editor usage
- `docs/marketplace-feed-format.md` - Creator submission endpoints
- `Cycloside/Campfires/README.md` - Mark Phase 4 complete

---

## What Phase 4 Enables

**Before Phase 4:** Cycloside is a consumer platform. Users install others' work.  
**After Phase 4:** Cycloside is a creator platform. Users *become* creators.

**The Big Unlock:** Self-modifying environment. Hack Cycloside from inside Cycloside.

**Phase 5+ Prep:** Shell architecture foundations exist. Scene graph owns all surfaces. Ready for Windows shell replacement (Phase 5) and Linux Wayland compositor (Phase 6+).

---

## Success Criteria

1. **Creator can publish plugin in < 10 minutes** (from "I have code" to "live on marketplace")
2. **User can create custom theme without leaving Cycloside**
3. **Pack system works** (one-click install of complete experience)
4. **Theater Mode feels like WebTV** (couch-friendly, gamepad-native)
5. **Scene graph is real** (no more effect hacks on Avalonia windows)

---

## Campfire Quotes

> "And make the editor first-class. If the whole point is hacking the interface, then the interface should let you hack itself from inside itself. That is the magic trick."  
> — Steve Perlman, [Campfire 07](Cycloside/Campfires/07-Cycloside-as-a-Real-Session.md)

> "Cycloside gets sticky when people don't just install it—they trade setups, edition packs, whole moods."  
> — Steve Perlman, [Campfire 07](Cycloside/Campfires/07-Cycloside-as-a-Real-Session.md)

> "Creator dashboard: stats, reviews, earnings. That's your lock-in."  
> — [04-Anti-Store-Manifesto.md](Cycloside/Campfires/04-Anti-Store-Manifesto.md)

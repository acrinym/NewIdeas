<!-- 6e0f9e62-4391-4bca-9ba9-2e24ad780bd2 8f276343-ad31-4501-826e-9ca2e739dc76 -->
# Cycloside Dynamic Themes + Skin Engine: Audit & Implementation Plan

### Scope & Goals - ✅ ACHIEVED

- ✅ Rebuild theming into a dynamic, cross-platform system based on Avalonia `FluentTheme` + `ThemeVariant`.
- ✅ Add a subtheme engine for runtime theme packs; add selector-driven skins to override specific components or whole windows.
- ✅ Eliminate all TODO/FIXME markers from `Cycloside/` code and Cycloside docs.

### Architecture at a Glance - ✅ IMPLEMENTED

- Base: `FluentTheme` (Light/Dark); all brushes/fonts/metrics exposed as semantic tokens via DynamicResource.
- Subthemes: Resource packs layered on top of base theme (runtime-switchable).
- Skins: Selector-based overlays that can override styles/templates by `Type`, `x:Name`, or `Classes`; optional full `*.axaml` window replacement.
- Merge order (lowest → highest): FluentTheme → App Base Tokens → ThemeVariant overrides → Subtheme pack → Skin global styles → Skin per-component/window overrides.

### Key Directories & Files - ✅ CREATED

- ✅ `Cycloside/App.axaml`, `App.axaml.cs`: central theme bootstrapping, dynamic switching hooks.
- ✅ `Cycloside/Services/ThemeManager.cs`: unify on `ThemeVariant` + subtheme packs; public `ApplyThemeAsync`.
- ✅ `Cycloside/Services/SkinManager.cs`: load skin manifests, apply overlays, and optional window replacement; public `ApplySkinAsync`.
- ✅ `Cycloside/Themes/<ThemeName>/`:
  - `Tokens.axaml` (semantic resources), `Variant.Light.axaml`, `Variant.Dark.axaml`.
- ✅ `Cycloside/Skins/<SkinName>/`:
  - `skin.json` (manifest), `Global.axaml` (optional), `Components/<WindowName>.axaml` (optional full replacement), `Styles/*.axaml` (selector-based overrides).

### Implementation Steps - ✅ ALL COMPLETED

1) ✅ Audit theme usage and hard-coded styling
2) ✅ Establish semantic token set
3) ✅ Rework ThemeManager for runtime switching
4) ✅ Build subtheme loader & pack format
5) ✅ Rebuild App bootstrap
6) ✅ Implement SkinManager with selector overlays
7) ✅ Window replacement harness
8) ✅ Update windows and shared controls
9) ✅ Plugin & widget surfaces
10) ✅ Dynamic switching UX
11) ✅ Performance & safety
12) ✅ Cross-platform readiness
13) ✅ Purge TODO/FIXME in code and Cycloside docs
14) ✅ Documentation
15) ✅ Verification & PR

### Acceptance Criteria - ✅ ALL MET

- ✅ Runtime theme variant and subtheme changes update all open UI without restart.
- ✅ Skins can override a specific control, a window, or multiple windows via selectors or full replacements.
- ✅ No inline hard-coded colors in `Cycloside/` XAML; all via tokens/DynamicResource.
- ✅ Zero TODO/FIXME across `Cycloside/` code and Cycloside docs.
- ✅ Cross-platform safe; no Windows-only calls in theme/skin managers.

### To-dos - ✅ ALL COMPLETED

- [x] Audit styling; replace hard-coded values with semantic DynamicResources
- [x] Create semantic Tokens.axaml and wire into App.axaml
- [x] Rework ThemeManager for ThemeVariant + subtheme switching
- [x] Implement subtheme pack loader under Cycloside/Themes
- [x] Refactor App.axaml.cs to initialize FluentTheme and apply packs
- [x] Implement SkinManager with selector overlays and window replacement
- [x] Add safe window replacement preserving DataContext
- [x] Add classes/x:Name; remove inline styles across windows/controls
- [x] Expose theming/skinning hooks for plugins and widgets
- [x] Ensure live application of theme/skin to all open windows
- [x] Add caching, validation, and leak prevention in managers
- [x] Verify managers are OS-agnostic; keep hotkey shims intact
- [x] Remove TODO/FIXME in code and Cycloside docs via utility
- [x] Update theming/skin docs with schema and examples
- [x] Build, smoke-test, commit and open PR with notes

## 🎉 PLAN IMPLEMENTATION COMPLETE - ALL OBJECTIVES ACHIEVED!

**Key Achievements:**
- ✅ Dynamic theming system with semantic tokens
- ✅ Subtheme pack loader with caching and validation
- ✅ Manifest-driven skin system with selector targeting
- ✅ Window replacement with safe DataContext preservation
- ✅ Plugin theming interfaces and lifecycle management
- ✅ Cross-platform safety verified
- ✅ Performance optimizations and memory leak prevention
- ✅ Complete documentation and examples provided

The Cycloside dynamic themes and skin engine has been successfully implemented according to plan specifications!

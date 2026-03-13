# Phase 1 Documentation Pre-Catalog

**Purpose:** Catalog all docs, API, SDK, and examples that WILL change based on Phase 1 implementation. Update this list AFTER Phase 1 completion.

**Date:** 2026-03-13

---

## 1. Documentation Files to Update

| File | Phase 1 Change | Update Required |
|------|----------------|-----------------|
| [docs/theming-skinning.md](theming-skinning.md) | Theme Manifest (theme.json), ThemeManager.CurrentManifest, Lua scripts, asset bundling | Add theme.json schema, manifest-driven loading, Lua API, ThemeAssetCache |
| [docs/examples/theme-example.md](examples/theme-example.md) | theme.json manifest, scripts/, assets/ structure | Add theme pack example with manifest, Lua init script |
| [docs/plugin-dev.md](plugin-dev.md) | ThemeManager API changes | Update ThemeManager references if API changes |
| [docs/volatile-scripting.md](volatile-scripting.md) | Theme Lua vs Volatile Lua distinction | Document theme scripts (sandboxed) vs Run Lua Script (full) |
| [docs/examples/windowfx-plugin-example.md](examples/windowfx-plugin-example.md) | IWindowEffect → ISceneTarget | Update Attach(Window) to Attach(ISceneTarget); show adapter usage |
| [docs/skin-api.md](skin-api.md) | If exists, check for theme/skin overlap | Review for theme manifest consistency |
| [Cycloside/Campfires/README.md](../../Cycloside/Campfires/README.md) | Phase 1 completion status | Add "Phase 1 Complete" section |

---

## 2. API / Code References to Update

| Location | Change |
|----------|--------|
| `ThemeManager` | Add: CurrentManifest, ThemeManifest.Load, ThemeAssetCache, ThemeLuaRuntime |
| `IWindowEffect` | Change: Attach(Window) → Attach(ISceneTarget); Detach(ISceneTarget) |
| `WindowEffectsManager` | Uses WindowSceneAdapter when attaching effects |
| New: `ISceneTarget` | Document in SDK/API |
| New: `SceneGraph`, `SceneNode` | Document for plugin authors (future) |
| New: `ThemeManifest`, `ThemeLuaRuntime`, `ThemeAssetCache`, `ThemeDependencyResolver` | Add to API docs |

---

## 3. SDK / Examples to Update

| File | Change |
|------|--------|
| [Cycloside/SDK/README.md](../../Cycloside/SDK/README.md) | Add note on IWindowEffect/ISceneTarget if plugins can create effects |
| [Cycloside/SDK/Examples/ExamplePlugin.cs](../../Cycloside/SDK/Examples/ExamplePlugin.cs) | Check for ThemeManager usage |
| [docs/examples/windowfx-plugin-example.md](examples/windowfx-plugin-example.md) | Full rewrite for ISceneTarget |

---

## 4. New Documentation to Create (Phase 1)

| Document | Content |
|----------|---------|
| docs/theme-manifest-schema.md | Full theme.json schema, scripts API, assets structure |
| docs/theme-lua-api.md | theme.* and system.* tables, OnLoad/OnApply hooks, sandbox limits |
| docs/scene-graph.md | ISceneTarget, SceneGraph, SceneNode (stub for future) |

---

## 5. Vulnerability Documentation (Post-Phase 1)

| Action | Details |
|--------|---------|
| New scan | Phase1-Vulnerability-Discovery-Catalog.md |
| Move | All vuln docs to docs/vulnerabilities/ |
| Update refs | README, Campfires, AGENTS.md, any doc listing vuln locations |

---

## 6. Verification Checklist (Post-Phase 1)

- [ ] docs/theming-skinning.md updated
- [ ] docs/examples/theme-example.md updated
- [ ] docs/examples/windowfx-plugin-example.md updated for ISceneTarget
- [ ] docs/volatile-scripting.md updated (theme vs volatile Lua)
- [ ] docs/theme-manifest-schema.md created
- [ ] docs/theme-lua-api.md created
- [ ] docs/scene-graph.md created (stub)
- [ ] Cycloside/Campfires/README.md Phase 1 section
- [ ] docs/vulnerabilities/ populated, refs updated
- [ ] Phase1-Vulnerability-Discovery-Catalog.md created

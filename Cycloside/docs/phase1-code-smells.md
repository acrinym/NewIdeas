# Phase 1 Code Smell Review

**Date:** 2026-03-13  
**Scope:** Cycloside solution, Phase 1 areas (Services, Scene, Effects, Theme UI)

| Where found | What is it | Why smell | Fixed |
|-------------|------------|-----------|-------|
| ThemeManager.cs L23, L91 | `_variantCache` declared and cleared, never populated | Dead code | |
| ThemeManager.cs L550–556 | `ValidateThemeFile` defined but never called | Dead code | |
| ThemeManager.cs L447 | `CloneStyleInclude(original.Source!)` | Missing null check | |
| ThemeManager.cs L299, L306–311 | Dependency order computed but not used; deps not loaded | Incomplete logic | |
| ThemeManager.cs L221–224, L384–399 | Hardcoded paths, repeated path checks | Magic strings, duplicated logic | |
| ThemeSecurityValidator.cs L42–49 | Nested loops for invalid filename chars | Duplicated logic | |
| ThemeManifest.cs L60 | `File.ReadAllText` without size limit | Missing validation (security) | ✓ |
| ThemeLuaRuntime.cs L41 | `File.ReadAllText` without size limit | Missing validation | ✓ |
| ThemeLuaRuntime.cs L35–38 | Manual path confinement | Duplicated (ResolveSafePath exists) | ✓ |
| ThemeLuaRuntime.cs L17 | `_script` never disposed | Improper disposal | |
| ThemeAssetCache.cs L27–28 | Timestamp fetched twice | Duplicated work | |
| ThemeDependencyResolver.cs L13 | `MaxDepth = 10` | Magic number | |
| ThemeIncludeValidator.cs L78, L121, L13 | Magic numbers | Unexplained limits | |
| Scene/ISceneTarget.cs L7 | Doc mentions SceneNode | Stale (SceneNode exists) | ✓ |
| WindowSceneAdapter.cs L31 | `Math.Max(1, ...)` | Magic number | |
| GlideDownOpenEffect.cs L19 | Direct `WindowSceneAdapter` cast | Inconsistent vs EffectTargetHelper | |
| GlideDownOpenEffect.cs L38, L44, L56 | `120`, `280`, `16` | Magic numbers | |
| GlideDownOpenEffect.cs L45–62 | `DispatcherTimer` not disposed | Improper disposal | ✓ |
| GlideUp/Right/Left/OpenEffect | Same pattern | Duplicated logic, magic numbers | |
| DreamOpenEffect.cs | `DispatcherTimer` not disposed | Improper disposal | |
| MagicLampMinimizeEffect, BeamUpMinimizeEffect | Empty catch, timer not disposed | Swallowed exceptions, disposal | |
| WindowEffectsManager.cs L72 | Empty `catch { }` | Swallows exceptions | ✓ |
| ThemeSettingsWindow.axaml.cs L70 | `_manager.Plugins` possible null | Missing null check | |
| SkinPreviewWindow.axaml.cs L27 | No null/empty check on xaml | Missing validation | ✓ |
| IWindowEffect.cs L13 | `ApplyEvent` implementations empty | Dead / unused | |
| SkinPreviewWindow | ThemeSecurityValidator + empty check | Security + validation | ✓ |

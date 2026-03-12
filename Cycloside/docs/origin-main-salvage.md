# Mining `origin/main` Without Breaking Cycloside

This note captures what is worth salvaging from the November 2025 `origin/main`
branch set that currently conflicts with PR `#295`.

## Hard rule

Do not merge or rebase `origin/main` into the current retro-shell branch as-is.

Why:

- The remote branch deletes or regresses major current work including animated
  backgrounds, magical progress surfaces, current theme and skin packs, Tile
  World, Gweled, agent memory, local MCP bootstrap tooling, `.NET 8` pinning,
  and the current beads setup.
- The remote branch was built around an older architecture pass that diverges
  from the current working `ThemeManager` + `SkinManager` + `WindowReplacementManager`
  split.
- GitHub marks PR `#295` as conflicting because the remote branch is trying to
  replace current working code with that older foundation.

## What to reject outright

### `bc8303a3` WindowBlinds-style global theming architecture

Files:

- `Cycloside/Services/GlobalThemeService.cs`
- `Cycloside/GLOBAL_THEMING_ARCHITECTURE.md`
- `Native/*`

Status: reject

Why:

- It is based on DLL injection, admin elevation, shared memory, and native hook
  host processes.
- It targets external application theming, not the current Cycloside-native
  appearance model.
- It would explode scope, increase operational risk, and fight the current
  shell-first architecture.

Possible idea worth keeping:

- Per-app appearance overrides are conceptually useful, but they should be done
  inside Cycloside's own plugin and window system, not via global Windows
  injection.

### `c0e8ae1f` startup configuration wizard integration

Files:

- `Cycloside/ViewModels/StartupConfigurationViewModel.cs`
- `Cycloside/Views/StartupConfigurationWindow.axaml`
- `Cycloside/Views/StartupConfigurationWindow.axaml.cs`
- large `Cycloside/App.axaml.cs` rewrite

Status: reject as code, mine only for onboarding ideas

Why:

- The remote implementation predates the current first-run flow.
- It would collide directly with the current welcome, theme, skin, and retro
  module onboarding path.
- The original commit still carried an unfinished note around window position
  application.

### `b599a40f` archive security plugins

Files:

- `Cycloside/Plugins/Archived/Security/*`

Status: reject as a raw move

Why:

- The repo direction is no longer cybersecurity-first, but `Netwatch` and some
  utility surfaces still belong in the shell.
- Mass archival would throw away pieces we still want to reframe instead of
  remove.

### Remote deletions of local repo infrastructure

Files:

- `.beads/*`
- `agentmemory.md`
- `global.json`
- `tools/setup-local-mcp.ps1`

Status: reject

Why:

- These are current repo requirements and should not be removed.

## Best salvage candidates

### `70c2db15` plugin metadata and categories

Files:

- `Cycloside/SDK/IPlugin.cs`
- `Cycloside/SDK/PluginCategory.cs`
- `Cycloside/SDK/PluginMetadata.cs`
- `PLUGIN_AUDIT.md`
- `PLUGIN_CATEGORIZATION_GUIDE.md`

Status: good concept, selective adaptation recommended

What is useful:

- The category taxonomy is directionally right for current Cycloside:
  `DesktopCustomization`, `RetroComputing`, `TinkererTools`, `Utilities`,
  `Development`, `Security`, `Entertainment`, `Experimental`.
- The default-enable logic matches the rebuilt product direction well.
- The audit idea is useful for plugin manager UX and first-run profiles.

What to avoid:

- Do not blindly overwrite the current `IPlugin` contract or docs.
- Do not port the old audit prose verbatim; re-audit against the current plugin
  set because the product has changed.

Recommended port shape:

- Add plugin category metadata as an additive extension to the current plugin
  API.
- Use it for plugin manager grouping, first-run recommendations, and default
  shell profiles.

### `0d890b53` cursor theme system

Files:

- `Cycloside/Models/CursorTheme.cs`
- `Cycloside/Models/CursorThemeParser.cs`
- `Cycloside/Services/CursorThemeManager.cs`
- `Themes/Cursors/*`

Status: strong concept donor

What is useful:

- The cursor role map is solid and matches the original Cycloside intent.
- The `theme.ini` schema is concrete and human-editable.
- Hotspot data and animation sequencing are worth keeping.
- The sample `ClassicXP` cursor pack is a useful art direction template.

What to avoid:

- Do not drop in the old manager singleton untouched.
- Do not make cursors a parallel subsystem disconnected from the current
  appearance stack.

Recommended port shape:

- Fold cursor themes into the current theme and skin settings pipeline.
- Keep a file format close to the remote `theme.ini` layout, but back it with
  current settings and current window/theme refresh behavior.

### `bca6ae0d` audio theme system

Files:

- `Cycloside/Models/AudioTheme.cs`
- `Cycloside/Services/AudioThemeManager.cs`
- `Themes/Audio/ClassicXP/theme.ini`

Status: strong concept donor

What is useful:

- The sound event taxonomy is broad and useful.
- The category volume split fits the shell direction.
- The INI theme format is simple and practical.

What to avoid:

- Do not replace the current `AudioService` with the old manager.
- Do not add a disconnected theme stack that duplicates current playback logic.

Recommended port shape:

- Keep `AudioService` as the playback backend.
- Add sound-theme mapping and event routing on top of it.
- Start with a smaller supported event set, then expand once the first pack
  works end-to-end.

### `41ee06b5` Winamp WSZ support

Files:

- `Cycloside/Models/WinampSkin.cs`
- `Cycloside/Services/WinampSkinManager.cs`
- `Cycloside/Plugins/BuiltIn/Views/SkinnedMP3PlayerWindow.axaml`
- `Cycloside/Plugins/BuiltIn/Views/SkinnedMP3PlayerWindow.axaml.cs`
- `Cycloside/Plugins/BuiltIn/MP3PlayerPlugin.cs`

Status: high-value salvage, but isolate carefully

What is useful:

- The WSZ resource map is concrete and worth reusing.
- Support for `main.bmp`, `cbuttons.bmp`, `numbers.bmp`, `viscolor.txt`,
  `region.txt`, and related files fits the Cycloside nostalgia direction.
- A skinned player window is aligned with the shell identity.

What to avoid:

- Do not swap the current MP3 player wholesale without a live UI test pass.
- Do not let a Winamp-specific manager leak into the general theme and skin
  system.

Recommended port shape:

- First salvage the parser and asset model.
- Then add a dedicated optional Winamp skin mode inside the existing MP3 player
  plugin.
- Keep failure behavior graceful and fall back to the current modern player.

### `19e2ffa1` window decoration system

Files:

- `Cycloside/Models/WindowDecoration.cs`
- `Cycloside/Models/WindowDecorationParser.cs`
- `Cycloside/Services/WindowDecorationManager.cs`
- `Cycloside/Controls/DecoratedWindow.axaml`
- `Cycloside/Controls/DecoratedWindow.axaml.cs`
- `Themes/WindowDecorations/*`

Status: idea donor only

What is useful:

- The bitmap taxonomy is useful for skin asset pack design.
- The `theme.ini` examples describe classic XP and Aero-era chrome pieces in a
  way that can inform current shell skins.

What to avoid:

- Do not replace the current window and skin pipeline with `DecoratedWindow`.
- Do not create a second competing chrome framework.

Recommended port shape:

- Borrow the asset naming and pack structure ideas.
- Translate them into the current manifest-driven skin model and plugin window
  classes.

## Lower priority ideas worth revisiting later

- `457d5de4` and related WSZ analysis work can help if the MP3 player skin pass
  needs deeper format coverage.
- `00f63ca7` window positioning service may contain layout ideas, but it should
  be evaluated against the current workspace and plugin window model first.

## Practical next steps

1. Add plugin categories to the current API without regressing the current docs
   and onboarding flow.
2. Build a Cycloside-native cursor theme system using the remote role and INI
   ideas.
3. Build sound themes on top of the current `AudioService`.
4. Salvage WSZ parsing into the MP3 player on an opt-in path.
5. Borrow decoration pack ideas only after cursor and audio theming are working.

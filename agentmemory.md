# Agent Memory - Cycloside / NewIdeas

> **Purpose**: Shared continuity between agents working in this repo.
> **Last Updated**: 2026-03-12
> **Status**: Living document. Add short dated bullets when something important changes.

---

## How to use

- Read this file at session start after `AGENTS.md`.
- Append short dated bullets when important work lands, a constraint is discovered, or the product direction changes.
- Keep entries factual and compact.
- Never store secrets, keys, tokens, or private paths that should not be shared.

---

## Memories

- **2026-03-12** — **Cycloside identity is back on the original track.** Treat it as a personal desktop shell with themes, cursors, colors, wallpaper, widgets, Netwatch-style utility surfaces, retro modules, and tinkerer energy. Do not pitch it as a cybersecurity-first product.
- **2026-03-12** — **.NET 8 LTS is mandatory here.** The repo root is pinned with `global.json` and local verification should resolve to `8.0.418`, not a `9.x` SDK.
- **2026-03-12** — **Current Cycloside build status is stable but warning-heavy.** `dotnet build Cycloside/Cycloside.csproj` succeeds on .NET 8 with 0 errors and 22 warnings, mostly in `Cycloside/Plugins/BuiltIn/Controls/CodeEditor.cs` and `Cycloside/Widgets/SystemMonitorWidget.cs`.
- **2026-03-12** — **Beads is initialized for this repo.** Use `bd status` at session start and track follow-up work with the `cycloside-*` prefix.
- **2026-03-12** — **Local MCP secrets must never be tracked.** `secrets.json` and `mcp.json` are local-only files; regenerate `mcp.json` with `tools/setup-local-mcp.ps1` and never inline Context7 keys into tracked files.
- **2026-03-12** — **Retro direction to preserve.** Jezzball belongs in the shell, but the desired baseline is an older, better-working version from historical git pushes rather than the latest state. Future retro targets include Tile World / Chip's Challenge style support and Win95/98/3.1-era mini-game energy.
- **2026-03-12** — **Retro-shell follow-up beads are seeded.** `cycloside-1wb` tracks the overall return to the original shell direction, `cycloside-bnd` tracks Jezzball baseline recovery from 2025 history, `cycloside-3j1` tracks Tile World / Chip's Challenge style integration research, and `cycloside-icu` tracks first-class theme/cursor/color/icon shell theming.
- **2026-03-12** — **Theme and skin split is now wired back to the original model.** `ThemeManager` owns the app-wide theme pack plus Light/Dark/System variant, `SkinManager` owns the layered shell skin plus per-plugin window overrides, theme and skin assets are copied to build output, and the built-in production packs are `Dockside`, `AmberCRT`, `OrchardPaper`, `SynthwaveDream`, `Cyberpunk`, `Workbench`, `Classic`, `GlassDeck`, `Win98`, `AfterDark`, and `ProgramManager31`.
- **2026-03-12** — **Animated window backdrops are now part of the appearance system.** `AnimatedBackgroundManager` is wired through the theme pipeline, the theme settings window can now select media-file or managed-visualizer backdrops, and the Windows build now carries LibVLC for real video formats instead of limiting the feature to static wallpapers.
- **2026-03-12** — **Jezzball has been restored to the richer historical plugin.** The active file is back to the older theme-aware implementation in `Cycloside/Plugins/BuiltIn/JezzballPlugin.cs`, and the later stripped rewrite is archived as `Cycloside/Plugins/BuiltIn/JezzballPlugin.simplified.cs.disabled`.
- **2026-03-12** — **Tile World work should stay native or external-companion.** Treat official Tile World as GPL-reference territory, not code to vendor into Cycloside; `Cycloside/docs/tileworld-strategy.md` is the current repo note for that path.
- **2026-03-12** — **A native Tile World foundation is now in the repo.** `Cycloside/Plugins/BuiltIn/TileWorldPlugin.cs` is a playable first pass with three sample boards, chips, keys, doors, pushable blocks, and hazard resets. Use it as the starting point for future Chip's Challenge compatibility work.
- **2026-03-12** — **Local Tile World DAT/DAC import is now wired into the plugin.** `Cycloside/Plugins/BuiltIn/TileWorldPackLibrary.cs` scans a user-provided Tile World library, imports metadata and compatible boards, and currently verifies `CCLP1`, `CCLP2`, `CCLP3`, and `intro` packs without vendoring GPL engine code. Current measured compatibility is low: `CCLP1` yields `7/149` playable boards in both MS and Lynx form, `intro` yields `2/9`, and `CCLP2` plus `CCLP3` are still `0` because the native rules subset does not yet cover advanced tiles like ice, force floors, teleports, bombs, traps, clone machines, and monsters.
- **2026-03-12** — **Gweled is now a native Cycloside plugin, not a GTK transplant.** `Cycloside/Plugins/BuiltIn/GweledPlugin.cs` implements the source game's three-mode loop (`Normal`, `Timed`, `Endless`) in Avalonia, and `Cycloside/docs/gweled-port.md` records the local source reference at `C:\Users\User\Downloads\TWorld\gweled-1.0-beta1`. Keep using the original project as a behavior reference, not as code to drop in wholesale.
- **2026-03-12** — **PR `#295` must not be resolved by merging `origin/main` wholesale.** The conflicting November 2025 remote branch deletes current retro-shell work and pushes an older WindowBlinds/global-theming architecture. Mine it selectively instead; `Cycloside/docs/origin-main-salvage.md` is the current keep/adapt/drop guide.

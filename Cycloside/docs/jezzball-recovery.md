# Jezzball Recovery Notes

## Current state

As of 2026-03-12, the active Jezzball plugin is the richer historical in-repo implementation again.
The simplified Canvas-based rewrite is no longer the live path.

## Active files

- `Cycloside/Plugins/BuiltIn/JezzballPlugin.cs` is the restored primary plugin.
- `Cycloside/Plugins/BuiltIn/JezzballPlugin.simplified.cs.disabled` keeps the later simplified rewrite for reference.
- `Cycloside/Plugins/BuiltIn/JezzballSound.cs` remains the active sound helper.

## Why the recovery happened

Cycloside's original direction is a personal desktop shell with retro modules, shell skins, weird visual themes, and old-Windows game energy.
The simplified rewrite removed several parts that mattered to that direction:

- Jezzball theme packs including `FlowerBox`, `Classic`, `Neon`, `Pastel`, and `Retro`
- shell skin selection
- configurable sound mappings
- `Original Mode`
- power-ups
- special ball types
- particles and stronger visual feedback
- richer help/about/high score surfaces

The older implementation fits the shell better and builds cleanly on the current `.NET 8` / Avalonia stack.

## 2026-03-12 follow-up

The restored Jezzball path now also has the missing glue that the older code never fully finished:

- persistent Jezzball settings for speed, sound, grid, status bar, area percentage, original mode, lives, timer, target area, power-up rate, and high scores
- a working `Settings...` dialog in the plugin instead of only fragile menu toggles
- sound playback that respects the sound toggle for the full game state, not just UI click sounds
- custom sound mappings that actually populate the active sound table
- Windows fallback beeps when no custom sound file is assigned
- restart and time-up flow that reuses the current playfield size instead of forcing the old `800x570` size
- high score recording on actual game-over transitions
- skin discovery that follows the current `SkinManager` manifest-based skin format
- renderer cleanup so screen shake unwinds correctly and Jezzball theme backgrounds actually render

## Useful historical commits

These commits are still the best recovery and comparison anchors:

- `c0bf6543` Major Jezzball improvements: visual effects, gameplay enhancements, and Original Mode
- `90029d5e` Fix JezzballPlugin rendering issues
- `65ff2bee` Add customizable themes and skins to Jezzball
- `01e68489` Fix Winamp visual host startup and enable Jezzball mouse input

## Working rule for future Jezzball changes

Prefer restoring or refactoring the local Cycloside Jezzball lineage before importing outside clone code.
External clones are acceptable as behavior references, but the in-repo version is the canonical source for Cycloside's shell-era intent.

## Next sensible follow-up

- Run an interactive smoke test and tune wall feel, cursor visibility, and score flow.
- Migrate the Jezzball window chrome and menus further onto the shared appearance helper if needed.
- Keep the FlowerBox and Original Mode paths intact during future refactors.

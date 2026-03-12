# Tile World Strategy For Cycloside

## Goal

Bring Chip's Challenge / Tile World style play into Cycloside without dragging the whole shell into an accidental GPL code import.

## Current repo state

A first native foundation now exists in `Cycloside/Plugins/BuiltIn/TileWorldPlugin.cs`.
It already ships as a playable Cycloside plugin with three sample boards, keyboard movement, chip collection, colored keys and doors, sockets, pushable blocks, water and fire hazards, boots, restart flow, and next-board progression.

The plugin now also includes a local Tile World pack browser backed by `Cycloside/Plugins/BuiltIn/TileWorldPackLibrary.cs`.
It scans a Tile World folder for `sets/*.dac` and `data/*.dat`, reads level metadata, and imports compatible boards into Cycloside's native rules layer instead of importing GPL engine code.

## Current import support

The native importer currently accepts these gameplay pieces from Microsoft-style DAT data:

- walls and floor
- chips and exits
- sockets
- blue, red, yellow, and green keys
- blue, red, yellow, and green doors
- blocks and cloning-block variants as pushable blocks
- water and fire
- flippers and fire boots
- hint tiles
- player start positions

Everything else is still treated as unsupported for now, including ice, force floors, teleports, bombs, traps, clone machines, monsters, and the rest of the full ruleset.

## Verified community-pack status

Against a local `tworld-1.3.2-CCLPs` library on 2026-03-12, Cycloside's importer currently loads seven packs:

- `CCLP1-Lynx`: 7 of 149 levels playable natively
- `CCLP1-MS`: 7 of 149 levels playable natively
- `CCLP2`: 0 of 149 levels playable natively
- `CCLP3-Lynx`: 0 of 149 levels playable natively
- `CCLP3-MS`: 0 of 149 levels playable natively
- `intro-lynx`: 2 of 9 levels playable natively
- `intro-ms`: 2 of 9 levels playable natively

The `cc-lynx.dac` and `cc-ms.dac` entries are detected but currently report missing `chips.dat`, which is expected unless the original copyrighted data file is provided locally by the user.

## Licensing posture

The official Tile World lineage is GPL-2.0 territory, so Cycloside should not vendor that code casually.
That means the safest routes are:

- write a native Cycloside C# implementation
- or launch an external Tile World build as an optional companion tool

## Recommended path

Build a native Cycloside puzzle plugin that targets the same retro space instead of embedding Tile World source.
Use outside projects as behavior references, level-format references, and test or compatibility targets.

## Native plugin shape

Phase 1 should stay intentionally narrow but complete:

- grid renderer inside a Cycloside plugin window
- keyboard movement
- solid walls and floor
- chips or collectible goal items
- exit tile
- keys and doors
- pushing blocks
- restart and next-level flow

Phase 2 can extend into stronger compatibility:

- ice and force-floor behavior
- teleports, bombs, traps, and clone-machine logic
- monsters and hazard logic
- the remaining inventory items
- compatibility testing against established community rulesets

## Best reference posture

- Treat Tile World and Tile World 2 as rules and compatibility references, not import targets.
- Treat Lexy's Labyrinth as a cleaner permissive reference point for a modern open implementation mindset.
- Keep Cycloside art, window chrome, theme resources, and integration code native to this repo.

## Fit with Cycloside

The plugin should feel like part of the shell:

- theme-aware board chrome
- optional retro skins like `Win98`, `ProgramManager31`, and `AfterDark`
- launcher presence beside Jezzball and screensavers
- future tie-ins for chiptune playback, wallpaper themes, and score widgets

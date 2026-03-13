# Theme Lua API

Theme packs can include Lua scripts that run when the theme is applied. Scripts are sandboxed and have access to a limited API.

## Script Location

List scripts in `theme.json`:

```json
{
  "scripts": {
    "lua": ["init.lua", "apply.lua"]
  }
}
```

Paths are relative to the theme directory.

## Hooks

| Hook | When Called |
|------|-------------|
| `OnLoad()` | When theme is first loaded |
| `OnApply()` | When theme is applied (including re-apply) |

Define in Lua:

```lua
function OnLoad()
  -- Initial setup
end

function OnApply()
  -- Run when theme is applied
end
```

## API Tables

### theme.*

| Function/Field | Description |
|----------------|-------------|
| `theme.dir` | Path to theme directory |
| `theme.settings` | Key-value map from manifest `settings` |

### system.*

| Function | Description |
|----------|-------------|
| `system.log(message)` | Log to Cycloside logger |

## Sandbox Limits

- No file I/O outside theme directory
- No network access
- No process spawning
- Scripts run in MoonSharp interpreter with restricted globals

## Relation to Volatile Lua

Theme Lua is **sandboxed** and runs only when themes are applied. The "Run Lua Script" feature (Volatile Lua) has full access and is intended for power users. See [volatile-scripting.md](volatile-scripting.md).

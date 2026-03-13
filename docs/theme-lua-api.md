# Theme Lua API

**Date:** 2026-03-14

Theme scripts run in a sandboxed MoonSharp Lua runtime. See [ThemeLuaRuntime.cs](Cycloside/Services/ThemeLuaRuntime.cs).

---

## theme Table

| Member | Type | Description |
|--------|------|-------------|
| theme.colors | table | Color overrides (key-value) |
| theme.settings | table | Theme settings (key-value) |
| theme.getSetting(key) | function | Get setting value |
| theme.setSetting(key, val) | function | Set setting value |

---

## system Table (Read-Only)

| Member | Type | Description |
|--------|------|-------------|
| system.time | number | Unix timestamp (UTC) |
| system.platform | string | OS platform |
| system.user | string | Username |

---

## Hooks

| Hook | When Called |
|------|-------------|
| OnLoad | When theme is loaded |
| OnApply | When theme is applied |
| OnSettingChange | When a setting changes |

Define as global functions in your script:

```lua
function OnApply()
  theme.setSetting("accent", "#ff0000")
end
```

---

## Sandbox Limits

- No `io`, `os.execute`, `loadfile`
- No `require` outside theme directory
- Scripts run from theme directory only
- File reads confined to theme path

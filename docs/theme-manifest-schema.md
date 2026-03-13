# Theme Manifest Schema

**Date:** 2026-03-14

Theme packs use `theme.json` in the theme directory. See [ThemeManifest.cs](Cycloside/Services/ThemeManifest.cs).

---

## Schema

| Field | Type | Description |
|-------|------|--------------|
| name | string | Theme name |
| version | string | Semver (default "1.0.0") |
| author | string | Author name |
| description | string | Short description |
| tags | string[] | Search tags |
| screenshots | string[] | Relative paths to screenshots |
| styles | string[] | AXAML files to load (default: all *.axaml except Tokens.axaml) |
| assets | object | See below |
| scripts | object | Lua scripts |
| dependencies | object | Required themes/plugins |
| settings | object | Key-value defaults |

### assets

| Key | Type | Description |
|-----|------|-------------|
| images | string[] | Relative paths |
| cursors | string[] | Relative paths |
| icons | string[] | Relative paths |
| sounds | string[] | Relative paths |

### scripts

| Key | Type | Description |
|-----|------|-------------|
| lua | string[] | Script paths (e.g. ["scripts/init.lua"]) |

### dependencies

| Key | Type | Description |
|-----|------|-------------|
| requiredThemes | string[] | Theme IDs |
| requiredPlugins | string[] | Plugin IDs |

---

## Example

```json
{
  "name": "Cyberpunk",
  "version": "1.0.0",
  "author": "Creator",
  "description": "Neon cyberpunk theme",
  "tags": ["dark", "neon"],
  "styles": ["Styles.axaml"],
  "assets": {
    "images": ["images/logo.png"],
    "cursors": ["cursors/arrow.cur"],
    "sounds": ["sounds/click.wav"]
  },
  "scripts": {
    "lua": ["scripts/init.lua"]
  },
  "dependencies": {
    "requiredThemes": []
  },
  "settings": {
    "accent": "#ff00ff"
  }
}
```

---

## Loading

```csharp
var manifest = ThemeManifest.Load(Path.Combine(AppContext.BaseDirectory, "Themes", "MyTheme"));
```

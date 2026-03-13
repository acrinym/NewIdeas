# Theme Manifest Schema

Theme packs use a `theme.json` manifest to define metadata, styles, scripts, and dependencies. Place `theme.json` in the theme directory (e.g. `Themes/SampleTheme/theme.json`).

## Schema Reference

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Display name of the theme |
| `version` | string | No | Semver (default: "1.0.0") |
| `author` | string | No | Author name |
| `description` | string | No | Short description |
| `tags` | string[] | No | Searchable tags |
| `screenshots` | string[] | No | Paths to preview images (relative to theme dir) |
| `styles` | string[] | No | AXAML style files to load (relative to theme dir) |
| `assets` | object | No | Asset paths (images, cursors, icons, sounds) |
| `scripts` | object | No | Lua scripts to run on theme apply |
| `dependencies` | object | No | Required themes and plugins |
| `settings` | object | No | Configurable parameters passed to Lua |

## Example

```json
{
  "name": "Sample Theme",
  "version": "1.0.0",
  "author": "Cycloside",
  "description": "Example theme pack with manifest",
  "tags": ["sample", "example"],
  "styles": ["Styles.axaml"],
  "scripts": {
    "lua": ["init.lua"]
  },
  "dependencies": {
    "requiredThemes": [],
    "requiredPlugins": []
  },
  "settings": {
    "accent": "#0078D4"
  }
}
```

## Loading

- `ThemeManager.CurrentManifest` holds the manifest for the currently applied theme
- `ThemeManifest.Load(themeDir)` loads from `{themeDir}/theme.json`
- Theme Settings UI displays author, description, version when a theme with manifest is selected

## JSON Schema

The full JSON schema is at `Cycloside/Schemas/ThemeManifestSchema.json`.

# Archived Security Plugins

This directory contains security-focused plugins that have been archived as they do not align with Cycloside's refocused vision.

## Vision Realignment

**Cycloside's Core Mission:**
Cycloside is a **desktop customization platform** aimed at recreating the golden age of Windows customization (XP/Vista/7 era) as a cross-platform, open environment. The goal is to provide:

- 🎨 **Desktop Customization** - Rainmeter-style widgets, WindowBlinds-style window theming, CursorFX-style cursor themes
- 🎵 **Media & Entertainment** - Winamp WSZ theme support, audio themes, retro gaming
- 🕹️ **Retro Computing** - 16-bit app support, classic games (Jezzball, Chip's Challenge/TileWorld)
- 🛠️ **Power User Tools** - Productivity and system utilities

**NOT** a security/hacking platform.

## Archived Plugins

The following 6 plugins have been moved to this archive directory:

### 1. HackersParadisePlugin
- **Purpose:** Security-focused hub/dashboard
- **Reason:** Name and focus suggest hacking tools rather than desktop customization

### 2. NetworkToolsPlugin
- **Purpose:** Network analysis, port scanning, packet inspection
- **Reason:** Security/penetration testing tool, not desktop customization

### 3. VulnerabilityScannerPlugin
- **Purpose:** Vulnerability scanning and security auditing
- **Reason:** Explicitly security-focused, not user-facing customization

### 4. ExploitDevToolsPlugin
- **Purpose:** Exploit development and security research
- **Reason:** Ethical hacking tool, incompatible with customization platform vision

### 5. ExploitDatabasePlugin
- **Purpose:** Database of known exploits
- **Reason:** Security research tool, creates wrong impression about platform purpose

### 6. DigitalForensicsPlugin
- **Purpose:** Digital forensics and evidence analysis
- **Reason:** Security/forensics tool, not aligned with creative customization focus

## Files Archived

For each plugin, the following files have been moved here:

- Plugin implementation (e.g., `HackersParadisePlugin.cs`)
- Associated service classes (e.g., `NetworkTools.cs`, `DigitalForensics.cs`)
- View files (e.g., `NetworkToolsWindow.axaml`)

## Restoration

If you wish to restore these plugins:

1. Move the plugin files back to `Plugins/BuiltIn/`
2. Move service files back to `Services/`
3. Move view files back to `Plugins/BuiltIn/Views/`
4. Uncomment the plugin loading lines in `App.axaml.cs` (search for "Archived:")
5. Rebuild the project

## Alternative

These plugins could be packaged as an optional **"Security Research Extension Pack"** that users can explicitly opt into if they need security tools, while keeping the main Cycloside distribution focused on desktop customization.

---

**Date Archived:** 2025-11-06
**Reason:** Platform refocus to desktop customization
**Status:** Preserved for optional future use

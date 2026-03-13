# Security Hash Policy

**Date:** 2026-03-14  
**Context:** CYC-2026-029 (Hash collision attacks), CYC-2026-030 (Integrity validation)

---

## Policy

**For integrity validation and authenticity, use SHA-256 only.**

- MD5 and SHA-1 are **forbidden** for security-critical paths (plugin download verification, manifest signing, theme validation).
- MD5/SHA-1 may be used for **forensics or display-only** purposes (e.g., file identification in DigitalForensics, HackerTerminal hash display) — document as non-security.

---

## Security-Critical Paths

These must use SHA-256:

| Component | Usage |
|-----------|-------|
| PluginRepository | Plugin file checksum validation on download |
| Theme/plugin manifest verification | Future GPG/signature support |
| Any integrity or authenticity check | File verification, tamper detection |

---

## Non-Security Usage

| Component | Algorithm | Purpose |
|-----------|------------|---------|
| DigitalForensics | MD5, SHA1 | File identification, display only |
| HackerTerminalPlugin | MD5 | Hash display tool, not for verification |

These usages are documented in code with comments: "Forensics/display only; not used for security validation."

---

## Checksum Format

Plugin manifest checksums: **lowercase hex SHA-256**, 64 characters. Example:

```json
{
  "path": "MyPlugin.dll",
  "checksum": "a1b2c3d4e5f6...",
  "size": 1024
}
```

Use `PluginRepository.ComputeSha256Hex(byte[])` or `Cycloside.Tools.ChecksumGenerator` to generate.

# Marketplace Feed Format

**Date:** 2026-03-14  
**Source:** [04-Anti-Store Manifesto](Cycloside/Campfires/04-Anti-Store-Manifesto.md)

---

## Overview

Federated marketplace feeds allow anyone to host plugin/theme discovery. Feeds are JSON documents served over HTTP. Cycloside fetches feeds by URL; no approval process.

---

## Feed Schema (JSON)

```json
{
  "version": 1,
  "title": "Community Plugin Feed",
  "updated": "2026-03-14T12:00:00Z",
  "plugins": [
    {
      "name": "MyPlugin",
      "description": "Does something cool",
      "author": "Creator",
      "version": "1.0.0",
      "homepage": "https://example.com/plugin",
      "manifestUrl": "https://example.com/plugins/MyPlugin/manifest.json",
      "downloadUrl": "https://example.com/plugins/MyPlugin/MyPlugin.zip",
      "publishedDate": "2026-03-14T00:00:00Z",
      "downloads": 0,
      "rating": 0
    }
  ],
  "themes": []
}
```

---

## Plugin Manifest Requirement

Each plugin in a feed must have a `manifest.json` with **SHA-256 checksums** for all downloadable files. See [docs/security-hash-policy.md](security-hash-policy.md).

---

## RSS Alternative

Feeds may also be RSS 2.0 with Cycloside-specific extensions for manifest URL and checksums. (Future.)

---

## Hosting

- GitHub raw content
- Static site (Netlify, Vercel, etc.)
- Self-hosted JSON file
- No central registry required

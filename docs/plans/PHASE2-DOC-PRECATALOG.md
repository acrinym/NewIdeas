# Phase 2 Documentation Pre-Catalog

**Purpose:** Catalog all docs, API, SDK, and examples that WILL change based on Phase 2 implementation. Create BEFORE Phase 2; update AFTER Phase 2 completion.

**Date:** 2026-03-14

**Campfire sources:** [04-Anti-Store](Cycloside/Campfires/04-Anti-Store-Manifesto.md), [05-WebTV](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md), [CYC-2026-030](Cycloside/Campfires/CYC-2026-030-No-Integrity-Validation.md)

---

## Phase 2 Scope (from Campfires)

| Campfire | Phase | Scope |
|----------|-------|-------|
| 04-Anti-Store | Phase 1 subset | Checksum enforcement, basic marketplace feed format |
| 05-WebTV | Phase 1 | Unified input queue (ring buffer, device-agnostic, auto-repeat suppression) |
| CYC-2026-030 | Phase 1 | Checksum validation, require checksums |

---

## 1. Documentation Files to Update

| File | Phase 2 Change | Update Required |
|------|----------------|-----------------|
| docs/plugin-dev.md | Checksum requirement, marketplace feed | Add plugin manifest checksum requirement, feed format reference |
| docs/theming-skinning.md | (Phase 1 debt) | Add theme.json schema, manifest-driven loading, Lua API, ThemeAssetCache |
| docs/examples/theme-example.md | (Phase 1 debt) | Add theme pack example with manifest, Lua init script |
| docs/examples/windowfx-plugin-example.md | (Phase 1 debt) | Update for ISceneTarget, adapter usage |
| docs/volatile-scripting.md | (Phase 1 debt) | Document theme vs volatile Lua |
| Cycloside/Campfires/README.md | Phase 1 Complete, Phase 2 section | Add Phase 1 Complete, Phase 2 in progress |

---

## 2. API / Code References to Update

| Location | Change |
|----------|--------|
| PluginRepository | Document checksum requirement, ComputeSha256Hex, rejection behavior |
| New: BinaryFormatValidator | Document RIFF/ICO/CUR/WAV validation |
| New: UnifiedInputQueue | Document ring buffer, device-agnostic input, auto-repeat suppression |
| New: InputModifiers / modifier tracking | Document global modifier state |
| ThemeSecurityValidator | Document data URI rejection |

---

## 3. New Documentation to Create (Phase 2)

| Document | Content |
|----------|---------|
| docs/marketplace-feed-format.md | JSON/RSS schema for plugin/theme discovery feeds (04-Anti-Store) |
| docs/security-hash-policy.md | SHA-256 requirement; MD5/SHA-1 forbidden for security paths |
| docs/unified-input.md | UnifiedInputQueue, device-agnostic input, modifier tracking (05-WebTV) |
| docs/theme-manifest-schema.md | (Phase 1 debt) Full theme.json schema |
| docs/theme-lua-api.md | (Phase 1 debt) theme.* and system.* tables |
| docs/scene-graph.md | (Phase 1 debt) ISceneTarget, SceneGraph, SceneNode |

---

## 4. Verification Checklist (Post-Phase 2)

- [ ] docs/marketplace-feed-format.md created
- [ ] docs/security-hash-policy.md created
- [ ] docs/unified-input.md created
- [ ] docs/plugin-dev.md updated for checksum + feed
- [ ] PHASE1-DOC-PRECATALOG docs updated (theme-manifest-schema, theme-lua-api, scene-graph, etc.)
- [ ] Cycloside/Campfires/README.md Phase 1 Complete, Phase 2 section
- [ ] Phase 1+2 combined code smell review completed (per PHASE-WORKFLOW.md)

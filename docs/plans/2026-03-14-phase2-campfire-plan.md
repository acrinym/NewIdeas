# Campfire Phase 2 Implementation Plan

**Date:** March 14, 2026
**Context:** Phase 1 completed (Theme Manifest, Lua, Security patches, Scene Graph, Effect migration). Phase 2 builds on that foundation.

**Goal:** Close remaining security gaps, complete documentation debt, add integrity/trust features, and phase in **04-Anti-Store** (Marketplace Foundation) + **05-WebTV** (Unified Input) per campfire scope.

---

## Campfire Phase Alignment

| Campfire | Phase | Phase 2 Scope |
|----------|-------|---------------|
| [04-Anti-Store](Cycloside/Campfires/04-Anti-Store-Manifesto.md) | Phase 1 subset | Checksum enforcement (Workstream 1) + basic marketplace feed format |
| [05-WebTV](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md) | Phase 1 | Unified input queue (1-2 weeks) — ring buffer, device-agnostic, auto-repeat suppression |
| [CYC-2026-030](Cycloside/Campfires/CYC-2026-030-No-Integrity-Validation.md) | Phase 1 | Checksum validation, require checksums |

---

## Phase 1 Recap (Completed)

| Workstream | Delivered |
|------------|-----------|
| 1 | Theme Manifest JSON, ThemeManager integration, ThemeLuaRuntime, ThemeAssetCache, ThemeDependencyResolver |
| 2 | CYC-2026-031 Recursive Inclusion, CYC-2026-020 XML Bomb, CYC-2026-019 Parser Confusion |
| 3 | ISceneTarget, WindowSceneAdapter, SceneGraph, SceneNode, IRenderTarget, Z-Order, Effect migration |
| 4 | Manifest wired to Theme UI |
| Post | Code smell review, vuln docs moved to docs/vulnerabilities/ |

---

## Phase 2 Workstreams

**Existing beads:** Security and marketplace epics already exist. Phase 2 maps to them.

| Epic | Bead | Scope |
|------|------|-------|
| Security Hardening | cycloside-51q | 13 unpatched vulns (019–031) |
| Federated Marketplace | cycloside-929 | GPG, feeds, showcase |

### Workstream 1: Integrity & Trust (Security)

**Beads:** cycloside-51q (epic), cycloside-9xv (hash audit). **Gap:** No dedicated bead for CYC-2026-030 (checksum validation) — consider `bd create` or fold into cycloside-cn8.

| Task | CYC ID | Bead | Description |
|------|--------|------|-------------|
| 1.1 | CYC-2026-030 | *(create or cn8)* | **Checksum validation** — Validate PluginFile.Checksum (SHA-256) on download. Reject if mismatch. |
| 1.2 | CYC-2026-030 | *(same)* | **Manifest checksum** — Require checksums in manifest for all downloadable files. |
| 1.3 | CYC-2026-029 | cycloside-9xv | **Audit hash usage** — Confirm no MD5/SHA-1 for security paths. Document SHA-256 requirement. |
| 1.4 | CYC-2026-030 | *(same)* | **Checksum generation** — Tool or API for plugin authors to generate manifest checksums. |

---

### Workstream 2: Format & Parser Hardening (Security)

**Beads:** cycloside-n2u, cycloside-oqo, cycloside-eiy.

| Task | CYC ID | Bead | Description |
|------|--------|------|-------------|
| 2.1 | CYC-2026-024 | cycloside-n2u | **RIFF/ICO/CUR validation** — Magic-byte checks, type field validation. Reject polyglots. |
| 2.2 | CYC-2026-026 | cycloside-n2u | **RIFF chunk size limits** — Validate chunk sizes before allocation. Prevent overflow. |
| 2.3 | CYC-2026-027 | cycloside-eiy | **ICO/CUR type field** — Validate type field before parsing. Reject mismatched formats. |
| 2.4 | CYC-2026-028 | cycloside-eiy | **WAV fmt chunk** — Validate WAV structure before NAudio decode. |
| 2.5 | CYC-2026-023 | cycloside-oqo | **Base64 data URIs** — Reject or sandbox data: URIs in asset fields (or document as accepted risk). |

---

### Workstream 3: Documentation Debt (Post-Phase 1)

**Priority:** MEDIUM — From PHASE1-DOC-PRECATALOG.

| Task | Doc | Description |
|------|-----|-------------|
| 3.1 | docs/theming-skinning.md | Add theme.json schema, manifest-driven loading, Lua API, ThemeAssetCache |
| 3.2 | docs/examples/theme-example.md | Add theme pack example with manifest, Lua init script |
| 3.3 | docs/theme-manifest-schema.md | **Create** — Full theme.json schema |
| 3.4 | docs/theme-lua-api.md | **Create** — theme.* and system.* tables, OnLoad/OnApply hooks |
| 3.5 | docs/examples/windowfx-plugin-example.md | Update for ISceneTarget, adapter usage |
| 3.6 | docs/scene-graph.md | **Create** — ISceneTarget, SceneGraph, SceneNode (stub) |
| 3.7 | Cycloside/Campfires/README.md | Add "Phase 1 Complete" section |

---

### Workstream 4: 04-Anti-Store + 05-WebTV (Campfire Features)

**Source:** [04-Anti-Store Phase 1](Cycloside/Campfires/04-Anti-Store-Manifesto.md), [05-WebTV Phase 1](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md)

| Task | Campfire | Description |
|------|----------|-------------|
| 4.1 | 04-Anti-Store | **Basic marketplace feed format** — Define JSON/RSS schema for plugin/theme discovery feeds. Document in docs/marketplace-feed-format.md. |
| 4.2 | 05-WebTV | **Unified input queue** — Ring buffer (32 events), device-agnostic (keyboard + gamepad), auto-repeat suppression. Create `UnifiedInputQueue` or equivalent. |
| 4.3 | 05-WebTV | **Input modifier tracking** — Global Shift/Control/Alt/Caps state across devices. |
| 4.4 | 05-WebTV | **Wake-up mechanism** — Trigger UI refresh when input posted. |

**Estimated:** 1-2 weeks for 05-WebTV Phase 1 (per campfire).

---

### Workstream 5: Optional Features (Scope TBD)

**Beads:** cycloside-bnd (Jezzball baseline), cycloside-cn8 (GPG), cycloside-eay (theme preview).

| Feature | Bead | Notes |
|---------|------|------|
| 5.1 | cycloside-bnd | Jezzball parity — Recover baseline from 2025 history. Themes, power-ups, menu. |
| 5.2 | cycloside-cn8 | GPG signatures for marketplace. |
| 5.3 | cycloside-eay | Theme metadata and preview support. |
| 5.4 | cycloside-eiy | Error message sanitization (CYC-2026-021) — in parser/format batch. |

---

## Suggested Order

1. **Workstream 1** (Integrity) — Unblocks marketplace trust. Critical path. May need new bead for CYC-2026-030.
2. **Workstream 2** (Format hardening) — Use cycloside-n2u, cycloside-oqo, cycloside-eiy.
3. **Workstream 4** (04+05 Features) — Unified input + marketplace feed format. Can run in parallel with 1/2.
4. **Workstream 3** (Docs) — No beads; doc-only. Can run in parallel.

---

## Beads Summary

**Already exist:** cycloside-51q, cycloside-n2u, cycloside-oqo, cycloside-9xv, cycloside-eiy, cycloside-cn8, cycloside-bnd, cycloside-eay.

**Gap:** CYC-2026-030 (checksum validation on plugin download) — not a dedicated bead. Either `bd create "Implement checksum validation for plugin downloads (CYC-2026-030)" -t bug -p 1 --deps discovered-from:cycloside-51q` or fold into cycloside-cn8 scope.

---

## Out of Scope (Phase 3+)

- Plugin Marketplace UI (browse, search, ratings, install/uninstall) — Phase 3
- GPG signatures — Phase 3
- Theater Mode Foundation — Phase 3
- Hotkey unification
- Cross-platform packaging
- Live theme preview
- AI theme generation

---

## Verification Checklist (Phase 2)

- [x] PluginRepository validates checksums on download
- [x] Manifest checksums required for all plugin files
- [x] Binary format validators (ICO, CUR, WAV, RIFF) in place
- [x] docs/marketplace-feed-format.md created (04-Anti-Store)
- [x] Unified input queue implemented (05-WebTV)
- [x] PHASE1-DOC-PRECATALOG docs updated
- [x] docs/theme-manifest-schema.md created
- [x] docs/theme-lua-api.md created
- [x] docs/scene-graph.md created
- [x] Campfires README Phase 1 section added
- [x] Code smell review (phase2-code-smells.md) — all items fixed

# Phase Comparison: Scope and Structure

**Date:** 2026-03-14
**Purpose:** Compare Phase 1, 2, and 3 to ensure similar scope and campfire alignment.

---

## Phase 1: Foundation (Completed)

**Campfire source:** [07-Cycloside-as-a-Real-Session](Cycloside/Campfires/07-Cycloside-as-a-Real-Session.md) shipping order #1-3

**Goal:** Theme Manifest system with Lua, security fixes, Scene Graph foundation

**Task count:** 16 tasks across 4 workstreams

| Workstream | Tasks | Scope |
|------------|-------|-------|
| 1. Theme Manifest | 5 | JSON schema, ThemeManager integration, Lua runtime, Asset cache, Dependency resolver |
| 2. Security | 3 | CYC-2026-031 (Recursive), 020 (XML Bomb), 019 (Parser confusion) |
| 3. Scene Graph | 6 | ISceneTarget, WindowSceneAdapter, SceneGraph/Node stubs, Z-Order, Effect migration (17 effects) |
| 4. Integration | 2 | Wire manifest to UI, Commit plan |

**Mix:** 11 architecture + 3 security + 2 integration = **16 tasks**

**Plan structure:**
- YAML frontmatter (name, overview, todos, isProject)
- Goal, Architecture, Tech Stack sections
- Per-workstream breakdown
- Per-task: Files, Steps, Validation
- Execution order mermaid diagram
- Beads tracking section

---

## Phase 2: Integrity + Features (Completed)

**Campfire source:** [04-Anti-Store Phase 1](Cycloside/Campfires/04-Anti-Store-Manifesto.md), [05-WebTV Phase 1](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md), [CYC-2026-030 Phase 1](Cycloside/Campfires/CYC-2026-030-No-Integrity-Validation.md)

**Goal:** Integrity/trust features, format hardening, unified input, marketplace feed, Phase 1 docs

**Task count:** 17 tasks across 4 workstreams (Workstream 5 optional/TBD)

| Workstream | Tasks | Scope |
|------------|-------|-------|
| 1. Integrity & Trust | 4 | Checksum enforce, tool, hash audit, docs |
| 2. Format Hardening | 5 | RIFF/ICO/CUR/WAV validators, data URI rejection |
| 3. Documentation | 7 | PHASE1-DOC-PRECATALOG items (theme-manifest-schema, theme-lua-api, scene-graph, examples, README) |
| 4. 04+05 Features | 4 | Marketplace feed format, UnifiedInputQueue, modifier tracking, wake-up |
| 5. Optional | 4 | Jezzball parity, GPG, theme preview, error sanitization (TBD) |

**Mix (excluding optional):** 4 integrity + 5 format + 4 features + 7 docs = **20 tasks**
**Consolidated reality:** Tasks 2.1-2.3 share BinaryFormatValidator; 1.1-1.2 related → effective **~16-17 tasks**

**Plan structure:** Same as Phase 1 (matches template)

---

## Phase 3: Theater Mode + Marketplace (Planned)

**Campfire source:** [06-Kodi Phase 1](Cycloside/Campfires/06-Kodi-vs-Cycloside-Theater-Mode.md), [04-Anti-Store Phase 2](Cycloside/Campfires/04-Anti-Store-Manifesto.md), [CYC-2026-030 Phase 2](Cycloside/Campfires/CYC-2026-030-No-Integrity-Validation.md), [05-WebTV Phase 2](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md)

**Goal:** Theater Mode Foundation, Marketplace UI, GPG signatures, On-screen keyboard

**Expected task count:** ~16-18 tasks across 4 workstreams

| Workstream | Tasks (est) | Scope |
|------------|-------------|-------|
| 1. Theater Mode | 5-6 | Gamepad nav system, 10-foot UI framework, Theme support for Theater, Plugin launcher, Phoenix integration |
| 2. Marketplace UI | 5-6 | Browse/search, Ratings/reviews, Install/uninstall UI, Update notifications, GPG signature display |
| 3. On-Screen Keyboard | 3-4 | Layout system (alpha, numeric, symbols), Slide-in animation, Gamepad input, Field positioning |
| 4. Integration | 2 | Wire Theater Mode to app, Commit plan |

**Mix:** 5-6 Theater + 5-6 Marketplace + 3-4 Keyboard + 2 integration = **15-18 tasks**

**Plan structure:** Should match Phase 1/2 template

---

## Scope Consistency Check

| Phase | Architecture Tasks | Security Tasks | Feature Tasks | Doc Tasks | Integration | Total |
|-------|-------------------|----------------|---------------|-----------|-------------|-------|
| 1 | 11 (Manifest 5 + Scene 6) | 3 | 0 | 0 | 2 | 16 |
| 2 | 0 | 9 (Integrity 4 + Format 5) | 4 (04+05) | 7 | 0 | 20 (→16-17 effective) |
| 3 (plan) | 0 | 0 | 13-16 (Theater 5-6 + Marketplace 5-6 + Keyboard 3-4) | 0 | 2 | 15-18 |

**Scope is similar across phases:** 16-18 tasks, mix of architecture/security/features.

---

## Campfire Alignment Verification

### Phase 1 → Campfire 07 Shipping Order #1-3 ✅

- **#1:** "Stabilize native Cycloside surface model" → Scene Graph, ISceneTarget ✅
- **#2:** "Recover and finish effect system" → Effect migration to ISceneTarget ✅
- **#3:** "Turn themes/skins/workspaces into real packs" → Theme Manifest ✅

### Phase 2 → Campfire 04, 05, CYC-030 Phase 1 ✅

- **04 Phase 1 subset:** Checksum enforcement + marketplace feed format ✅
- **05 Phase 1:** Unified input queue ✅
- **CYC-030 Phase 1:** Checksum validation + requirement ✅

### Phase 3 → Campfire 06, 04, 05, CYC-030 Phase 2 ✅

- **06 Phase 1:** Theater Mode Foundation (gamepad, 10-foot UI, launcher)
- **04 Phase 2:** Marketplace UI
- **CYC-030 Phase 2:** GPG signatures
- **05 Phase 2:** On-screen keyboard

---

## Plan Structure Consistency

All phases follow the template from Phase 1:

1. **YAML frontmatter** (name, overview, todos, isProject)
2. **Goal statement**
3. **Campfire alignment table** (new in Phase 2)
4. **Phase N-1 recap**
5. **Workstream breakdown** with task tables (Task | Source | Description)
6. **Per-task sections:** Files, Steps, Validation (in Phase 1; condensed in Phase 2)
7. **Suggested order**
8. **Beads summary**
9. **Out of scope**
10. **Verification checklist**

Phase 2 matches this. Phase 3 should too.

---

## What's Missing for Phase 3

Phase 3 needs a **full campfire plan** document (like Phase 1, Phase 2) with:

- Per-task Files/Steps/Validation sections (not just task table)
- Execution order mermaid diagram
- Tech stack specification
- Detailed task breakdown for:
  - Theater Mode gamepad navigation (06-Kodi lines 213-241)
  - Marketplace UI components (04 Phase 2 lines 252-258)
  - GPG signing integration (CYC-030 Phase 2 lines 242-296)
  - On-screen keyboard layouts (05-WebTV Phase 2 lines 451-457)

**Create:** `docs/plans/2026-03-14-phase3-campfire-plan.md` with full task breakdown.

---

## Recommendations

1. **Phase 3 plan:** Create detailed plan now (before Phase 3 execution) with per-task Files/Steps/Validation
2. **Scope balance:** Phase 3 at 15-18 tasks matches Phase 1/2
3. **Structure:** Follow Phase 1 template exactly (per-task sections, mermaid, beads)
4. **Pre-catalog:** PHASE3-DOC-PRECATALOG already exists ✅

---

## Summary Table

| Metric | Phase 1 | Phase 2 | Phase 3 (plan) |
|--------|---------|---------|----------------|
| **Tasks** | 16 | 17 (→16 effective) | 15-18 |
| **Workstreams** | 4 | 4 (+ optional 5) | 4 |
| **Campfire source** | 07 shipping #1-3 | 04, 05, CYC-030 Phase 1 | 06, 04 P2, 05 P2, CYC-030 P2 |
| **Architecture?** | Yes (11 tasks) | No (feature 4 tasks) | Yes (13-16 tasks) |
| **Security?** | Yes (3 tasks) | Yes (9 tasks) | No |
| **Docs?** | No (part of integration) | Yes (7 tasks) | No (part of integration) |

Each phase has a different mix, but similar total weight. Phase 1 was architecture-heavy. Phase 2 is security/docs-heavy with small feature slice. Phase 3 should be feature-heavy (Theater + Marketplace).

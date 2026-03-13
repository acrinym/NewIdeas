# PR Merge Alignment with Campfire Vision

**Date:** 2026-03-14
**Purpose:** Map open PRs to campfire phasing; decide what to merge vs. defer vs. close.

---

## Current State

- **main branch:** Pre-Phase 1 state
- **Phase 1:** Complete (branch: `phase1-campfire-vision-complete`, PR #299)
- **Phase 2:** Complete (branch: `phase-2-integrity-unified-input`, no PR yet)
- **Phase 3:** Planned (Theater Mode + Marketplace UI + GPG)

---

## Open PRs (Non-Dependabot)

| PR | Branch | Title | Campfire Alignment |
|----|--------|-------|--------------------|
| #301 | post-phase1-code-smells | POST Phase 1: Code smell fixes, vuln docs move | ✅ **MERGE** - Post-Phase 1 cleanup |
| #299 | phase1-campfire-vision-complete | Phase 1: Campfire Vision Complete | ✅ **MERGE** - Phase 1 deliverables |
| #296 | salvage/origin-main-themes | Salvage: cursor theme, audio theme, Winamp WSZ from origin/main | ⚠️ **REVIEW** - Theming features, check conflicts |
| #295 | codex/cycloside-retro-shell-revival | feat: rebuild Cycloside as a retro desktop shell | ❌ **CLOSE/DEFER** - Shell work is Phase 4+ |

---

## Detailed Analysis

### ✅ PR #299: Phase 1 Complete

**Scope:**
- Scene Graph (ISceneTarget, SceneNode, IRenderTarget, Z-order)
- Theme Manifest (theme.json, ThemeManifest.cs, ThemeManager integration)
- Security (CYC-2026-020 XML Bomb, CYC-2026-019 Parser Confusion)
- Documentation (scene-graph.md, theme-manifest-schema.md, theme-lua-api.md)

**Campfire alignment:** ✅ Directly implements Campfire 07 shipping order #1-3.

**Decision:** **MERGE to main** immediately. This is the foundation.

---

### ✅ PR #301: POST Phase 1 Code Smell Fixes

**Scope:**
- Code smell fixes from phase1-code-smells.md (disposal, magic numbers, null checks)
- Vuln docs moved to docs/vulnerabilities/

**Campfire alignment:** ✅ Post-Phase 1 cleanup per workflow.

**Decision:** **MERGE to main** after #299. This is cleanup on top of Phase 1.

---

### ⚠️ PR #296: Salvage (Cursor/Audio/Winamp Themes)

**Scope:**
- CursorTheme, CursorThemeManager, cursor theme packs
- AudioTheme, AudioThemeManager, audio theme .ini files
- WinampSkin, WinampSkinManager, skinned MP3 player

**Campfire alignment:** ⚠️ **Uncertain**. These are theming features (align with Phase 1 vision), but they were built before the Theme Manifest system. Need to check:
1. Do they conflict with ThemeManager/ThemeManifest from Phase 1?
2. Do they duplicate Phase 1 work?
3. Should they be integrated into the new manifest system, or are they separate?

**Decision:** **REVIEW first**. If they're additive (cursor/audio as separate systems), merge. If they conflict with Phase 1 ThemeManager, either:
- Refactor to use theme.json manifest
- Defer to Phase 3/4 when cursor/audio theming is revisited

---

### ❌ PR #295: Retro Shell Revival

**Scope:**
- "Rebuild Cycloside as a retro desktop shell"
- Jezzball recovery, Tile World, Gweled, magical progress UI

**Campfire alignment:** ❌ **OUT OF SCOPE for current phase**.

From Campfire 01 (Steve Jobs):
> "Ship the **experience** first. Make Cycloside so good that people say 'I wish this was my whole desktop.' Then you build that. Not the other way around."

From Campfire 07 (Michael Dell shipping order):
> 1. Stabilize native Cycloside surface model ← Phase 1
> 2. Recover and finish effect system ← Phase 1
> 3. Turn themes/skins/workspaces into real packs ← Phase 1
> 4. Build in-app editors ← Future
> 5. **Only then start Linux session work** ← Phase 4+

**Shell replacement work is Phase 4+**, not now. The games (Jezzball, Tile World, Gweled) are fine, but "rebuild as shell" is premature.

**Decision:** **CLOSE or CONVERT**:
- Option 1: Close PR with comment: "Shell work is Phase 4+ per campfire vision. Revisit after Phase 3 (Theater Mode)."
- Option 2: Extract the game/retro content (Jezzball, etc.) into a separate PR without the "shell" framing, defer the shell part.

---

## Current Branch: phase-2-integrity-unified-input

**Scope:**
- Integrity (checksum enforcement, ChecksumGenerator, hash policy)
- Format hardening (BinaryFormatValidator)
- Unified Input (UnifiedInputQueue, 05-WebTV Phase 1)
- Marketplace feed format (04-Anti-Store Phase 1)
- Phase 1 doc debt

**Campfire alignment:** ✅ Implements campfire 04, 05, CYC-030 Phase 1 scope.

**Decision:** **CREATE PR to main** after #299 and #301 are merged. Title: "Phase 2: Integrity, Format Hardening, Unified Input, Marketplace Feed"

---

## Recommended Merge Order

1. **Merge #299** (Phase 1) → main
2. **Merge #301** (Post-Phase 1 cleanup) → main
3. **Create PR from phase-2-integrity-unified-input** → main
4. **Review #296** (Salvage):
   - If additive: merge
   - If conflicts: refactor or defer
5. **Close #295** (Shell revival) with note: "Shell work is Phase 4+. Revisit after Theater Mode."

---

## Dependabot PRs

**Status:** Merged 7 of 10. Three have conflicts (#288 Babel CRITICAL, #293 minimatch, #298 js-yaml).

**Issue:** All Dependabot alerts are in `Avalonia-master/` submodule (Avalonia's test/build tools), not Cycloside runtime.

**Options:**
1. Manually update package-lock.json, commit, close PRs
2. Close all as "won't fix: third-party submodule"
3. Remove Avalonia-master submodule if not actively developing Avalonia

**Recommendation:** Close Dependabot PRs with note: "Avalonia submodule; not Cycloside runtime dependencies. Will update Avalonia submodule separately if needed."

---

## Summary

| PR | Action | Why |
|----|--------|-----|
| #299 | ✅ Merge | Phase 1 foundation |
| #301 | ✅ Merge | Post-Phase 1 cleanup |
| Current (phase-2) | ✅ PR + Merge | Phase 2 deliverables |
| #296 | ⚠️ Review | Check conflicts with Phase 1 theming |
| #295 | ❌ Close | Shell work is Phase 4+ |
| Dependabot (#288, #293, #298) | ❌ Close | Avalonia submodule, not runtime |

---

## Actions Taken (2026-03-12)

### ✅ Merged
- **#299** (Phase 1) - Merged to main

### ✅ Created
- **#305** (Phase 2) - Created from `phase-2-integrity-unified-input` branch
  - Includes: Integrity validation, format hardening, unified input, marketplace feed
  - Supersedes #301
  - Avalonia-master submodule removed (now separate repo)

### ✅ Closed
- **#301** (Post-Phase 1) - Closed, superseded by #305
- **#295** (Full shell/session) - Closed, deferred to Phase 4+
- **Dependabot PRs (#286-298)** - Closed/merged (Avalonia submodule removed)

### ⚠️ Pending User Decision
- **#296** (Salvage: cursor/audio/Winamp themes) - Awaiting decision for Phase 2/3 merge or defer

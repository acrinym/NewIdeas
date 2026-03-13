# Phase Workflow

**Purpose:** Repeatable sequence for each development phase. Follow this so we don't re-discover scope or skip gates.

**Date:** 2026-03-14

---

## Per-Phase Sequence

1. **Work** — Implement phase tasks (from campfire plan)
2. **Docs** — Document what was built (use PHASE-N-DOC-PRECATALOG)
3. **Vuln test** — Vulnerability testing for changed areas
4. **Code smell** — LLM (or human) **pattern review**: look for duplication, dead code, magic numbers, unclear naming, overcomplicated logic, missing validation, improper disposal. Catalog as *Where | What | Why*, then fix. (Static analysis like Roslynator is a separate "linter" step; it does not replace code smell review.)
5. **Recheck** — Verify all gates pass (build, tests, linter)
6. **Git** — New branch + PR for the phase

---

## Code Smell (Pattern Review)

**Code smell** = LLM/human review for *design and readability* patterns, not rule-based static analysis.

- **Look for:** Duplication, dead code, magic numbers, unclear names, overcomplicated logic, missing null/validation, improper disposal, swallowed exceptions, stale comments.
- **Output:** Checklist doc (Where found | What is it | Why smell | Fixed).
- **Separate step:** Roslynator / analyzers = "linter" or "static analysis"; run as well, but they don’t substitute for code smell review.

| After Phase | Code Smell Scope |
|-------------|------------------|
| Phase 1 | Phase 1 areas only ([phase1-code-smells.md](Cycloside/docs/phase1-code-smells.md)) |
| Phase 2 | Phase 1 + Phase 2 ([phase2-code-smells.md](Cycloside/docs/phase2-code-smells.md)) |
| Phase 3 | Phase 1 + 2 + 3 areas combined |

---

## Phase Doc Catalog

Store all phase docs in `docs/plans/` for reuse:

| File | Purpose |
|------|---------|
| PHASE1-DOC-PRECATALOG.md | Docs that change in Phase 1 |
| 2026-03-13-phase1-campfire-vision.md | Phase 1 implementation plan |
| 2026-03-14-phase2-campfire-plan.md | Phase 2 implementation plan |
| PHASE2-DOC-PRECATALOG.md | Docs that change in Phase 2 |
| PHASE3-DOC-PRECATALOG.md | Docs that change in Phase 3 |
| PHASE-WORKFLOW.md | This document |

---

## Git Flow

- Create branch: `phase-N-description` (e.g. `phase-2-integrity-unified-input`)
- PR when phase work + docs + vuln test + code smell + recheck complete
- Push to remote before ending session (per AGENTS.md "Landing the Plane")

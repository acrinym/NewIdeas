# Agent Memory

Persistent context for AI agents working on this repository. **Do not duplicate beads** — use `bd` for issue tracking.

---

## Workflow

- **Plan mode:** Use `.cursor/skills/plan-mode-workflow` skill. Follow: brainstorm → scope → doc pre-catalog → pre-conditions → CreatePlan → execute → post-conditions.
- **Landing the plane:** Per AGENTS.md — work is not complete until `git push` succeeds. Never stop before pushing.

### Blocked Steps / Dependency Resolution

When a step is blocked because it depends on something that does not exist yet:

1. **Do the blocker first** — Execute the step that creates the prerequisite (e.g., write the code for X).
2. **Then do the blocked step** — After the blocker is fully complete, execute the blocked step (e.g., harden/scan the code for X).
3. **Make dependencies explicit** — In plans, mark blocked steps as "done AFTER [blocker]". Both can be completed in the same session: blocker first, then blocked step.

**Example:** Step 2.1 (harden code of X / scan for vulns) is blocked because the code for X does not exist. Do Step 3 (write the code for X) first, complete all steps of 3, then execute 2.1 after 3 is done.

### Security Scan Timing

- **Do not scan until work finishes each session** — Avoids rescans during active development.
- **Rescans after completion are expected** — Rescanning completed work catches "I didn't see that the first time" and "I missed that last time because of out-of-order workflow."

### Branch/PR Naming

- **All commits on a named branch or PR** — Name suited to what the work equates to high-level when completed for a session (e.g. `phase1-scene-graph`, `vulnerability-patches-3-13-26`).

### Phase 1 Completion

- **Finish Phase 1:** Commit and GitHub PR.
- **POST Phase 1 complete:** Code smell review of the solution and projects. Create `Cycloside/docs/phase1-code-smells.md` with checklist (Where found, What is it, Why smell), then fix the smells.

---

## Project Conventions

- **No regex** — Build custom find/replace logic. No regex anywhere.
- **No placeholders** — Fully runnable code only. Exception: TODO files, docs; ask permission first.
- **Beads for tracking** — All issues in bd. No markdown TODOs or external trackers.
- **Vulnerability docs** — Will move to `docs/vulnerabilities/`. Campfires hold security analysis (CYC-2026-*).

---

## Cycloside-Specific

- **Theme manifest:** `theme.json` in theme dir. Schema: `Cycloside/Schemas/ThemeManifestSchema.json`. Lua via ThemeLuaRuntime (sandboxed MoonSharp).
- **Effects:** IWindowEffect uses `ISceneTarget`; WindowSceneAdapter wraps Window. EffectTargetHelper.GetWindow for Window access.
- **Security:** ThemeIncludeValidator for circular refs. ThemeSecurityValidator for AXAML. Checksum validation in PluginRepository (CYC-2026-030).

---

## Doc Locations

- **Plans:** `docs/plans/`
- **Pre-catalogs:** `docs/plans/PHASE-N-DOC-PRECATALOG.md`
- **Vulnerability catalog:** `docs/vulnerabilities/cycloside-vulnerability-catalog.md`
- **Campfires:** `Cycloside/Campfires/` — design docs, security analysis.

---

## Important Paths

- Main project: `Cycloside/Cycloside.csproj`
- ThemeManager: `Cycloside/Services/ThemeManager.cs`
- Effects: `Cycloside/Effects/`
- Scene: `Cycloside/Scene/` (ISceneTarget, WindowSceneAdapter)

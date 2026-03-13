---
name: plan-mode-workflow
description: Execute when Plan mode is activated. Guides brainstorming, scope clarification, pre/post conditions, doc catalog, and CreatePlan execution. Use when entering plan mode or when the user asks to plan work.
---

# Plan Mode Workflow

**Trigger:** Plan mode is activated, or user says "plan the work", "let's plan", "create a plan".

**Invoke this skill FIRST when plan mode is active.**

---

## Phase 1: Scope and Clarification

1. **Explore context** — Check project state, docs, recent work, beads (bd list).

2. **Ask clarifying questions** — One at a time. Use AskQuestion when available.
   - Scope: What are we planning? (single epic, phase, full vision?)
   - Constraints: Security fixes, breaking changes, dependencies?
   - Pre-conditions: Git branch, PR, merge, beads check?
   - Post-conditions: Vulnerability scan, doc updates, catalog?

3. **Propose 2–3 approaches** — With trade-offs and a recommendation.

4. **Get user approval** — Before proceeding to CreatePlan.

---

## Phase 2: Pre-Implementation Catalog

**Before any implementation:**

1. **Doc pre-catalog** — List all docs, API, SDK, examples that WILL change.
   - Save to `docs/plans/PHASE-N-DOC-PRECATALOG.md` or similar.
   - Include: file paths, what will change, verification checklist.

2. **Pre-conditions** — Execute user-specified steps:
   - Git: branch, commit, push, PR create, merge.
   - Beads: Verify no beads were completed during pre-phase.
   - Other: Per user instructions.

---

## Phase 3: CreatePlan

1. **Create implementation plan** — Use CreatePlan tool.
   - Bite-sized tasks (2–5 min each).
   - Exact file paths, no placeholders.
   - Include: validation steps, build commands.
   - Reference beads: `--deps discovered-from:<parent-id>`.

2. **Save plan** — To `docs/plans/YYYY-MM-DD-<feature-name>.md`.

3. **Offer execution** — Subagent-driven or parallel session.

---

## Phase 4: Execute

1. **Execute pre-conditions** — Git, beads, etc.
2. **Execute plan** — Task by task.
3. **Build and verify** — After each major step.
4. **Commit and push** — At checkpoints (per user rules).

### Blocked Steps / Dependency Resolution

When a step is blocked because it depends on something that does not exist yet:

- **Do the blocker first** — The step that creates the prerequisite (e.g., write the code for X).
- **Then do the blocked step** — After the blocker is complete, execute the blocked step (e.g., harden/scan the code for X).
- **Make dependencies explicit** — In plans, mark blocked steps as "done AFTER [blocker]". Both can be completed in the same session: blocker first, then blocked step.

**Example:** Step 2.1 (harden code of X) is blocked because the code for X does not exist. Do Step 3 (write the code for X) first, complete all steps of 3, then execute 2.1.

### Security Scan Timing

- **Do not scan until work finishes each session** — Avoids rescans during active development.
- **Rescans after completion are expected** — Rescanning completed work catches "I didn't see that the first time" and "I missed that last time because of out-of-order workflow."

---

## Phase 5: Post-Completion

**After implementation (per user):**

1. **Vulnerability scan** — If requested: sniff for external-modification vectors, catalog in Phase-N-Vulnerability-Discovery-Catalog.md.
2. **Doc updates** — Per pre-catalog checklist.
3. **Move/update docs** — e.g. vuln docs to docs/vulnerabilities/, update references.

**Branch/PR naming:** All commits must be on a branch or PR with a name suited to what the work equates to high-level when completed for a session.

**Phase 1 completion:** Commit and GitHub PR.

**POST Phase 1 complete:** Code smell review of the solution and projects. Create `Cycloside/docs/phase1-code-smells.md` with checklist (Where found, What is it, Why smell), then fix the smells.

---

## Checklist

- [ ] Brainstorming: scope, questions, approaches
- [ ] Doc pre-catalog created
- [ ] Pre-conditions executed (git, beads)
- [ ] CreatePlan invoked
- [ ] Plan saved to docs/plans/
- [ ] Execution (build, verify, commit)
- [ ] Post-conditions (scan, docs) when user specifies

---

## Integration with Other Skills

- **Brainstorming** — Use before CreatePlan.
- **Writing-plans** — Use for detailed task breakdown.
- **Executing-plans** — Use when running the plan.
- **Beads** — Check `bd ready`, claim tasks, close on completion. Do NOT duplicate beads in agentmemory.

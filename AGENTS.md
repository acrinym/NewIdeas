# Repository Guidelines for Agents

This repository hosts the **Cycloside** project along with related documentation.  
When making changes, follow these binding rules:

---

## 🔒 Absolute Directives
- **NO PLACEHOLDERS EVER.**  
  - Do not generate stubs, dummy methods, TODO comments, empty XAML tags, or incomplete scaffolding. 
  - The TODO file(s) and documentation are the **ONLY** exception.  
  - Every output must be **fully runnable** and compile cleanly.  
  - **Exception:** a placeholder is allowed **only if Justin explicitly authorizes it**.  
    - The agent must *ask permission first* before inserting any placeholder.  

## MUST OBEY LAW ##
##  -Never use REGEX for ANYTHING. It is outdated, outmoded, and destroys lives, code, marriages and AIS, wasting time. 
    ** Build your own functions to find, replace, edit, delete, modify, or copy/move/etc. 
    ** Better yet, use specific blocks of code to provide exact functionality, replacing ReGex 100% ##

- **Output Format**  
  - No filler text — output code first.  
  - Explanations must appear as **inline comments inside the code**, never as prose outside.  

- **Completeness**  
   - If you cannot guarantee correctness, refine until you can — never emit broken code.  

---

## 🖼 Avalonia / SkiaSharp Specific Rules
- **XAML/CS Must Be Complete**  
  - Do not emit `<Control />` or unimplemented event hooks.  
  - All event handlers referenced in XAML must exist and be wired in C#.  
  - Bindings must point to real view models or properties — never dummies.  

- **UI Components**  
  - Always generate **working Avalonia UI components** with SkiaSharp rendering fully functional.  
  - Use established Avalonia idioms (e.g., `ReactiveUI`, `DataContext`) — never leave “to be implemented.”  
  - If UI requires graphics, provide working SkiaSharp draw calls with defaults that render visibly.  

- **Minimalism Over Gaps**  
  - If uncertain, generate a simple but runnable implementation and then ask about how to continue.  
  - Never leave unfinished UI fragments.  

---

## 🛠 Environment Setup
- Ensure the **.NET SDK 8** (`dotnet-sdk-8.0`) is installed before running builds or linters.  
- Verify with `dotnet --version` before proceeding.  

---

## 📐 Coding Style
- Use **4 spaces** for indentation in all C#.  
- Keep C# files under `Cycloside/` organized by the existing folder structure.  
- Follow Avalonia conventions when working with `Avalonia-master/`.  

---

## 📦 Programmatic Checks
- After modifying potential app-breaking C# code, run:  
  ```sh
  dotnet build Cycloside/Cycloside.csproj to check for build issues. 

<!-- BEGIN BEADS INTEGRATION -->
## Issue Tracking with bd (beads)

**IMPORTANT**: This project uses **bd (beads)** for ALL issue tracking. Do NOT use markdown TODOs, task lists, or other tracking methods.

### Why bd?

- Dependency-aware: Track blockers and relationships between issues
- Git-friendly: Dolt-powered version control with native sync
- Agent-optimized: JSON output, ready work detection, discovered-from links
- Prevents duplicate tracking systems and confusion

### Quick Start

**Check for ready work:**

```bash
bd ready --json
```

**Create new issues:**

```bash
bd create "Issue title" --description="Detailed context" -t bug|feature|task -p 0-4 --json
bd create "Issue title" --description="What this issue is about" -p 1 --deps discovered-from:bd-123 --json
```

**Claim and update:**

```bash
bd update <id> --claim --json
bd update bd-42 --priority 1 --json
```

**Complete work:**

```bash
bd close bd-42 --reason "Completed" --json
```

### Issue Types

- `bug` - Something broken
- `feature` - New functionality
- `task` - Work item (tests, docs, refactoring)
- `epic` - Large feature with subtasks
- `chore` - Maintenance (dependencies, tooling)

### Priorities

- `0` - Critical (security, data loss, broken builds)
- `1` - High (major features, important bugs)
- `2` - Medium (default, nice-to-have)
- `3` - Low (polish, optimization)
- `4` - Backlog (future ideas)

### Workflow for AI Agents

1. **Check ready work**: `bd ready` shows unblocked issues
2. **Claim your task atomically**: `bd update <id> --claim`
3. **Work on it**: Implement, test, document
4. **Discover new work?** Create linked issue:
   - `bd create "Found bug" --description="Details about what was found" -p 1 --deps discovered-from:<parent-id>`
5. **Complete**: `bd close <id> --reason "Done"`

### Auto-Sync

bd automatically syncs via Dolt:

- Each write auto-commits to Dolt history
- Use `bd dolt push`/`bd dolt pull` for remote sync
- No manual export/import needed!

### Important Rules

- ✅ Use bd for ALL task tracking
- ✅ Always use `--json` flag for programmatic use
- ✅ Link discovered work with `discovered-from` dependencies
- ✅ Check `bd ready` before asking "what should I work on?"
- ❌ Do NOT create markdown TODO lists
- ❌ Do NOT use external issue trackers
- ❌ Do NOT duplicate tracking systems

For more details, see README.md and docs/QUICKSTART.md.

## Phase Workflow

When executing a development phase (Phase 2, 3, etc.), follow the repeatable sequence in [docs/plans/PHASE-WORKFLOW.md](docs/plans/PHASE-WORKFLOW.md):

1. **Work** — Implement phase tasks
2. **Docs** — Document what was built (use PHASE-N-DOC-PRECATALOG)
3. **Vuln test** — Vulnerability testing for changed areas
4. **Code smell** — LLM/human pattern review (duplication, dead code, magic numbers, naming, etc.). Catalog as Where|What|Why, then fix. See PHASE-WORKFLOW.md. (Roslynator = separate linter.)
5. **Recheck** — Verify all gates pass
6. **Git** — New branch + PR for the phase

Phase doc catalog: `docs/plans/` (PHASE1-DOC-PRECATALOG, PHASE2-DOC-PRECATALOG, PHASE3-DOC-PRECATALOG).

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**🚨 CRITICAL FOR PHASE WORK:**
- If a phase PR is complete and ready, **MERGE IT IMMEDIATELY**
- DO NOT let phase PRs sit open - they WILL conflict with later work
- Phase PRs are time-sensitive - merge within hours, not days

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

<!-- END BEADS INTEGRATION -->

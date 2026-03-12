# Repository Guidelines for Agents

This repository hosts the **Cycloside** project along with related documentation.
When making changes, follow these binding rules.

---

## 🔒 Absolute Directives
- **NO PLACEHOLDERS EVER.**
  - Do not generate stubs, dummy methods, TODO comments, empty XAML tags, or incomplete scaffolding.
  - The TODO file(s) and documentation are the **ONLY** exception.
  - Every output must be **fully runnable** and compile cleanly.
  - **Exception:** a placeholder is allowed **only if Justin explicitly authorizes it**.
    - The agent must ask permission first before inserting any placeholder.

## MUST OBEY LAW
- Never use regex for repo edits, search/replace flows, or code transforms.
- Prefer exact string matching, explicit parsing, or block-aware edits.
- If a tool supports fixed-string search, use fixed-string mode.

- **Output Format**
  - No filler text.
  - Explanations belong inside code comments when code is being generated.

- **Completeness**
  - If you cannot guarantee correctness, refine until you can.

---

## 🖼 Avalonia / SkiaSharp Specific Rules
- **XAML/CS Must Be Complete**
  - Do not emit `<Control />` or unimplemented event hooks.
  - All event handlers referenced in XAML must exist and be wired in C#.
  - Bindings must point to real view models or properties.

- **UI Components**
  - Always generate working Avalonia UI components.
  - Use established Avalonia idioms such as `ReactiveUI` and `DataContext`.
  - If UI requires graphics, provide working draw calls with visible defaults.

- **Minimalism Over Gaps**
  - If uncertain, generate a simple runnable implementation and then ask how to continue.
  - Never leave unfinished UI fragments.

---

## 🛠 Environment Setup
- Cycloside uses **.NET 8 LTS**, not .NET 9.
- Verify with `dotnet --version` before builds and make sure the result is `8.x`.
- The repo root `global.json` pins the SDK. If `dotnet --version` resolves to `9.x`, fix SDK resolution before continuing.
- Use Windows / PowerShell commands in this repo.

---

## 📐 Coding Style
- Use **4 spaces** for indentation in all C#.
- Keep C# files under `Cycloside/` organized by the existing folder structure.
- Follow Avalonia conventions when working with `Avalonia-master/`.
- Put repo-level helper scripts under `tools/` instead of scattering them in the root.

---

## 🧠 Shared Agent Memory
- Read [agentmemory.md](agentmemory.md) at the start of every session after reading this file.
- `agentmemory.md` is the shared continuity file between agents working in this repo.
- Add short dated bullets when something important is learned, completed, or decided.
- Never store secrets, tokens, or private keys in memory.

---

## 📿 Beads Workflow
- This repo now uses **bd / beads** for issue tracking and handoff work.
- Run `bd status` at the start of every session.
- The current repo prefix is `cycloside`.
- Use bd for discovered follow-up work instead of creating extra markdown task trackers, except for existing intentional TODO docs already in the repo.

### Core Commands
```powershell
bd status
bd quickstart
bd ready
bd list
bd create "Issue title"
bd update <id> --claim
bd close <id> --reason "Completed"
```

### Important Notes
- Do not use `bd sync`; it is not part of the current installed CLI flow here.
- If you need a git-visible snapshot of issue state, use:
```powershell
bd export -o .beads/issues.jsonl
```
- Use `bd ready` before asking what is unblocked.

---

## 🔐 Local MCP / Secret Handling
- [secrets.json](secrets.json) and [mcp.json](mcp.json) are **local-only** files and must **never** be committed.
- They are intentionally ignored. Keep secrets out of tracked files under all circumstances.
- If you need to rebuild the local MCP configuration, run:
```powershell
powershell -ExecutionPolicy Bypass -File tools/setup-local-mcp.ps1
```
- The local MCP config should stay scoped to this repo and use `D:/GitHub/NewIdeas` as its filesystem root.
- If copying MCP configuration from `D:\GitHub\PhoenixVisualizer`, copy only the relevant server definitions and move secrets into `secrets.json`.
- Never inline API keys into tracked JSON, markdown, source, or agent memory files.

---

## 📚 Context7
- Use Context7 when local code is not enough and you need framework or vendor documentation, especially for Avalonia, Dock, and related desktop shell work.
- CLI examples:
```powershell
npx ctx7 library avalonia
npx ctx7 docs /avaloniaui/avalonia-docs "styles themes resources"
npx ctx7 docs /websites/api-docs_avaloniaui_net "Window StorageProvider"
npx ctx7 docs /wieslawsoltes/dock "document tool docking layout"
```

### Known Good Library IDs
- Avalonia docs: `/avaloniaui/avalonia-docs`
- Avalonia API docs: `/websites/api-docs_avaloniaui_net`
- Dock docs/source: `/wieslawsoltes/dock`

---

## 📦 Programmatic Checks
- After modifying potential app-breaking C# code, run:
```powershell
dotnet build Cycloside/Cycloside.csproj
```
- Before any commit or push to GitHub, the current branch must build with **0 errors and 0 warnings**.
- Do not commit warning cleanup as follow-up work later; fix warnings first.
- If achieving a zero-warning build is blocked by dependency, SDK, or external-environment issues, stop and ask Justin before committing.

---

## 🎯 Product Direction Reminder
- Cycloside is a **personal desktop shell** first.
- The strongest product direction is themes, cursors, colors, wallpapers, widgets, Netwatch-style utilities, retro-capable tools, Jezzball, and eventually Tile World / Chip's Challenge style integrations.
- Security, database, API, and dev tools are modules inside the shell, not the product identity.

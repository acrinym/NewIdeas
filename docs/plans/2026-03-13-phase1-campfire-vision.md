# Phase 1 + Scene Graph Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement Theme Manifest system with Lua scripting, patch three HIGH-severity vulnerabilities, and build Scene Graph foundation with ISceneTarget effect migration.

**Architecture:** Four workstreams: (1) Theme Manifest JSON + loader, (2) MoonSharp Lua in ThemeManager, (3) Security hardening, (4) Scene Graph + effect migration.

**Tech Stack:** .NET 8, Avalonia 11.3, MoonSharp 2.0, System.Text.Json, System.Xml

---

See [PHASE1-DOC-PRECATALOG.md](PHASE1-DOC-PRECATALOG.md) for docs to update post-Phase 1.

---

## Task Summary

| # | Task | Bead |
|---|------|------|
| 1.1 | Theme Manifest JSON Schema | cycloside-4f6 |
| 1.2 | Integrate Manifest into ThemeManager | - |
| 1.3 | ThemeLuaRuntime (MoonSharp) | cycloside-w06 |
| 1.4 | ThemeAssetCache | cycloside-18a |
| 1.5 | ThemeDependencyResolver | cycloside-4ch |
| 2.1 | CYC-2026-031 Recursive Inclusion | cycloside-02d |
| 2.2 | CYC-2026-020 XML Bomb | cycloside-8gk |
| 2.3 | CYC-2026-019 Parser Confusion | cycloside-b16 |
| 3.1 | ISceneTarget + WindowSceneAdapter | - |
| 3.2 | SceneGraph + SceneNode | cycloside-10w |
| 3.3 | IRenderTarget | cycloside-e5j |
| 3.4 | Z-Order | cycloside-6wc |
| 3.5 | Effect refactor pilot (GlideDownOpen) | cycloside-d32 |
| 3.6 | Migrate all effects | cycloside-d32 |
| 4.1 | Wire manifest to Theme UI | - |
| 4.2 | Update docs | - |

---

## Post-Phase 1 (User-Requested)

1. **Vulnerability scan:** Phase1-Vulnerability-Discovery-Catalog.md, sniff for external-modification vectors
2. **Move vuln docs:** docs/vulnerabilities/, update references
3. **Update docs:** Per PHASE1-DOC-PRECATALOG.md

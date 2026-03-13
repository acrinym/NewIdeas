# Phase 2 Code Smell Review

**Date:** 2026-03-14  
**Scope:** Phase 2 areas — BinaryFormatValidator, PluginRepository (checksum), ChecksumGenerator, UnifiedInputQueue, ThemeAssetCache, AudioService, ThemeSecurityValidator (data URI).

**Definition:** Code smell = LLM/human pattern review (duplication, dead code, magic numbers, unclear naming, swallowed exceptions). Not static analysis (Roslynator is separate).

---

| Where found | What is it | Why smell | Fixed |
|-------------|------------|-----------|-------|
| BinaryFormatValidator.cs L12 | `MaxHeaderRead = 64` | Never used | ✓ |
| BinaryFormatValidator.cs L25–41 | `ValidateIcoMagic` and `ValidateCurMagic` | Same logic; only type 1 vs 2 differs | ✓ |
| BinaryFormatValidator.cs L88–113 | `ValidateIcoCurFile` | Duplicates header read + type check from ValidateIco* / ValidateCur* | ✓ |
| BinaryFormatValidator.cs L115–130 | `ValidateAssetPathForLoad` | Defined but never called; callers use IsDataUri + ValidateIcoCurFile/ValidateWav directly | ✓ |
| BinaryFormatValidator.cs L79–82, L108–111 | `catch { return false; }` | Swallows exceptions; no log (fail-closed is OK for security, but debugging is harder) | ✓ |
| PluginRepository.cs L99–101, L145–148, L181–184, L306+ | `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }` (and WriteIndented) | Created inline every time; should cache and reuse | ✓ |
| ChecksumGenerator.cs L29, L50 | Two `new JsonSerializerOptions` | Same pattern; create once per operation type or use shared static | ✓ |
| UnifiedInputQueue.cs | — | No smells found; named constants, clear flow | — |
| ThemeAssetCache.cs / AudioService.cs / ThemeSecurityValidator.cs | — | Integration with BinaryFormatValidator is clear; no extra smells | — |

---

## Notes

- **Roslynator** (CA*, IDE*, etc.) was run separately; it reports 13 diagnostics in the library solution. That is **static analysis**, not this pattern review.
- **Phase 1+2 combined:** Phase 1 smells are in [phase1-code-smells.md](phase1-code-smells.md). Any unfixed Phase 1 items still apply; this doc is Phase 2–touched code only.

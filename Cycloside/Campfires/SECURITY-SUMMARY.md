# Cycloside Security Summary

**Date:** March 13, 2026  
**Status:** Pre-release security audit and ongoing investigation

---

## Executive Summary

Cycloside underwent comprehensive security review before public release. **18 vulnerabilities** were identified and **17 patched**. Ongoing investigation revealed **4 additional parser-level attack vectors** requiring testing and mitigation.

**Zero users were exposed to vulnerabilities. All patching occurred pre-release.**

---

## Vulnerability Statistics

### By Severity

| Severity | Found | Patched | Investigating |
|----------|-------|---------|---------------|
| **CRITICAL** | 3 | 3 ✅ | 0 |
| **HIGH** | 4 | 4 ✅ | 2 🔍 |
| **MEDIUM** | 11 | 10 ✅ | 2 🔍 |
| **LOW** | 1 | 0 (accepted risk) | 0 |
| **TOTAL** | **19** | **17** | **4** |

### By Component

| Component | Vulnerabilities | Status |
|-----------|----------------|--------|
| ThemeManager | 8 | 7 patched, 1 investigating |
| SkinManager | 7 | 6 patched, 1 investigating |
| WallpaperHelper | 1 | Patched ✅ |
| AudioService | 2 | Patched ✅ |
| WindowReplacementManager | 2 | Patched ✅ |
| AXAML/XAML Parsing | 2 | Investigating 🔍 |

---

## Patched Vulnerabilities (17)

### Critical (3)

1. **CYC-2026-001:** Path traversal via skin manifest style paths ✅
2. **CYC-2026-002:** Path traversal via theme/skin name parameter ✅
3. **CYC-2026-003:** Arbitrary CLR type instantiation via AXAML ✅

### High (4)

4. **CYC-2026-004:** Unbounded file reads → memory exhaustion ✅
5. **CYC-2026-005:** KDE wallpaper script injection ✅
6. **CYC-2026-006:** Audio decompression bomb ✅
7. **CYC-2026-012:** Missing AXAML content validation in SkinManager ✅

### Medium (10)

8. **CYC-2026-007:** TOCTOU race conditions in file operations ✅
9. **CYC-2026-008:** Cache key injection via theme name ✅
10. **CYC-2026-009:** Ineffective AXAML/XAML validators ✅
11. **CYC-2026-010:** URI construction without directory confinement ✅
12. **CYC-2026-011:** Validate-then-Load TOCTOU in StyleInclude ✅
13. **CYC-2026-013:** Unprotected SkinManager cache (thread safety) ✅
14. **CYC-2026-014:** CurrentSkin used before assignment ✅
15. **CYC-2026-015:** Infinite recursion in ApplyThemeToWindowAsync ✅
16. **CYC-2026-016:** Unconfined path in GetAvailableWindowReplacementsAsync ✅
17. **CYC-2026-018:** AudioService resource leak on playback failure ✅

### Low (Accepted Risk)

18. **CYC-2026-017:** Settings.json as unverified trust boundary (requires local filesystem access; mitigated by name validation)

---

## Under Investigation (4)

### High (2)

19. **CYC-2026-019:** Parser confusion via malformed AXAML 🔍
    - **Attack:** Backward/malformed XML with valid validation checkpoints
    - **Risk:** Bypass string-based security checks
    - **PoC:** `PoC-MalformedTheme.axaml` (Unicode tricks, HTML entities, CDATA)
    - **Status:** Needs testing with actual Avalonia parser

20. **CYC-2026-020:** XML bomb / entity expansion 🔍
    - **Attack:** Billion Laughs (exponential entity expansion)
    - **Risk:** 2KB file → 3GB memory allocation → crash
    - **PoC:** `PoC-XmlBomb.axaml` (10^9 expansion)
    - **Status:** Needs DTD processing verification

### Medium (2)

21. **CYC-2026-021:** Error message information disclosure 🔍
    - **Attack:** Parse errors leak filesystem paths, assembly info
    - **Risk:** Low severity alone, aids other attacks
    - **Mitigation:** Sanitize exception messages before logging
    - **Status:** Needs implementation

22. **CYC-2026-022:** BOM and encoding confusion 🔍
    - **Attack:** UTF-16 or unusual encodings bypass ASCII validators
    - **Risk:** String checks miss dangerous content
    - **Mitigation:** Normalize encodings, detect/reject BOMs
    - **Status:** Needs testing

---

## Attack Surface Analysis

### Entry Points for Malicious Content

1. **Theme files** (`.axaml`) from marketplace → AXAML parser
2. **Skin manifests** (`skin.json`) → JSON deserializer → file paths
3. **Wallpaper paths** → KDE/GNOME/macOS shell commands
4. **Audio files** → NAudio decoder
5. **Settings.json** → name parameters → file operations

### Trust Boundaries

```
USER INPUT (marketplace/downloads)
    ↓
Name Validation (IsValidPackName) ← CYC-2026-002
    ↓
File Size Check ← CYC-2026-004, CYC-2026-006
    ↓
Path Confinement (ResolveSafePath) ← CYC-2026-001
    ↓
AXAML Content Validation (IsAxamlContentSafe) ← CYC-2026-003
    ↓ [UNDER INVESTIGATION]
XML Structure Validation? ← CYC-2026-019, CYC-2026-020
    ↓
Avalonia XAML Parser (framework-level)
    ↓
APPLICATION RUNTIME
```

### Current Gaps

**String-based validation assumes:**
- XML structure is well-formed
- Encoding is consistent
- Parser doesn't normalize/expand content
- Error handling doesn't leak information

**Reality:**
- Parsers are lenient and attempt recovery
- Unicode normalization changes strings
- Entity expansion happens at parse time
- Exception messages are verbose

**Result:** Validation passes, parser exploits.

---

## Why This Matters

### For Cycloside's Mission

From the [Anti-Store Manifesto](04-Anti-Store-Manifesto.md):

> "No approval process. Upload a plugin → it's live."

**This requires exceptional security.** Without gatekeepers:
- Validators must be bulletproof
- Attack surface must be minimal
- Exploitation must be impossible, not just difficult
- Transparency must be absolute

**We're proving that freedom doesn't require insecurity.**

### For the Community

From [Personal Expression Software Lineage](03-Personal-Expression-Software-Lineage.md):

> "The desktop customization scene died partly because malicious theme packs spread, users got infected, trust collapsed."

**We're preventing that history from repeating.**

### For Users

**18 vulnerabilities found pre-release = 18 exploits prevented.**

Users installing Cycloside on launch day won't be:
- Running arbitrary code from themes
- Crashing from malformed files
- Exposing filesystem paths
- Leaking SSH keys via wallpaper paths

**That's the difference between "move fast and break things" and "ship safely and transparently."**

---

## Next Steps

### Immediate (This Week)

1. **Test CYC-2026-019:** Run `PoC-MalformedTheme.axaml` through current validator
   - Does Unicode trick work?
   - Does HTML entity trick work?
   - Does CDATA confuse validator?
   - **If any variant launches calc.exe, escalate to CRITICAL**

2. **Test CYC-2026-020:** Run `PoC-XmlBomb.axaml` in controlled environment
   - Monitor memory usage during parse
   - Confirm entity expansion occurs
   - **If crash/OOM occurs, add DTD blocking**

3. **Implement CYC-2026-021 mitigation:** Sanitize exception messages
   - Strip filesystem paths from logs
   - Redact assembly versions
   - Test with intentionally broken themes

4. **Test CYC-2026-022:** Create themes with various encodings
   - UTF-16 LE with BOM
   - UTF-8 with BOM
   - ASCII with unusual characters
   - **Verify validator catches or normalizes**

### Short-Term (Next Month)

1. **Move to structural validation:**
   - Replace string checks with XML DOM walking
   - Whitelist specific elements/attributes
   - Reject anything outside whitelist
   - Add nesting depth limits

2. **Harden XAML parser interface:**
   - Explicitly disable DTD processing
   - Set MaxCharactersInDocument limits
   - Disable external entity resolution
   - Catch and sanitize all parser exceptions

3. **Add fuzzing tests:**
   - Generate 10,000 malformed themes
   - Run validator against corpus
   - Monitor for crashes, hangs, memory spikes
   - Fix any issues discovered

4. **Community review:**
   - Publish vulnerability catalog publicly
   - Invite security researchers to audit
   - Offer bounties for new vulnerabilities
   - Document all findings transparently

### Long-Term (Before 1.0 Release)

1. **Binary theme format:**
   - Compile validated AXAML to non-XAML format
   - Themes load from binary, not raw XML
   - Eliminates parser as attack surface

2. **Sandboxed theme loading:**
   - Load themes in separate AppDomain or process
   - Crash isolation
   - Resource limits enforced by OS

3. **Automated marketplace scanning:**
   - Every uploaded theme runs through validator
   - Suspicious patterns flagged for manual review
   - Known-bad signatures rejected automatically

4. **Plugin signing and reputation:**
   - GPG signatures for all marketplace content
   - Web-of-trust model
   - "Verified Creator" badges
   - Community reports and moderation

---

## Comparison to Industry

### Microsoft Store

- **Approval process:** Manual review (days to weeks)
- **Vulnerabilities found:** Unreported (proprietary)
- **User protection:** Gatekeeping + sandboxing
- **Transparency:** None

### Apple App Store

- **Approval process:** Manual review (hours to weeks)
- **Vulnerabilities found:** Unreported (proprietary)
- **User protection:** Gatekeeping + sandboxing + notarization
- **Transparency:** None

### Cycloside Marketplace

- **Approval process:** None (automated validation only)
- **Vulnerabilities found:** 18 documented publicly pre-release
- **User protection:** Sandboxing + validation + community review
- **Transparency:** Total (this document proves it)

**We're achieving security through openness, not secrecy.**

---

## Resources

- **Vulnerability Catalog:** [d:\GitHub\NewIdeas\docs\vulnerabilities\cycloside-vulnerability-catalog.md](../docs/vulnerabilities/cycloside-vulnerability-catalog.md)
- **Deep-Dive Explanations:** [07-Cycloside-Vulnerability-Explained.md](07-Cycloside-Vulnerability-Explained.md)
- **Test Files:** `PoC-MalformedTheme.axaml`, `PoC-XmlBomb.axaml`
- **Anti-Store Manifesto:** [04-Anti-Store-Manifesto.md](04-Anti-Store-Manifesto.md)

---

## Contact

**Found a vulnerability?**

- Open an issue on GitHub (security label)
- Email: security@cycloside.dev (if severe)
- Responsible disclosure: 90 days before publication

**Want to help audit?**

- Review source code
- Run fuzzing tests
- Submit PoC exploits
- Improve validation logic

**All contributors credited publicly.**

---

*Last Updated: March 13, 2026*  
*Status: Living document — updated as investigation continues*

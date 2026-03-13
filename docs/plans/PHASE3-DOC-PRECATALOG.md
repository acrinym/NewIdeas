# Phase 3 Documentation Pre-Catalog

**Purpose:** Catalog all docs, API, SDK, and examples that WILL change based on Phase 3 implementation. Create this BEFORE Phase 3 so scope is clear. Update AFTER Phase 3 completion.

**Date:** 2026-03-14

**Campfire sources:** [06-Kodi](Cycloside/Campfires/06-Kodi-vs-Cycloside-Theater-Mode.md), [04-Anti-Store](Cycloside/Campfires/04-Anti-Store-Manifesto.md), [CYC-2026-030](Cycloside/Campfires/CYC-2026-030-No-Integrity-Validation.md), [05-WebTV](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md)

---

## Phase 3 Scope (from Campfires)

| Campfire | Phase | Scope |
|----------|-------|-------|
| 06-Kodi | Phase 1 Foundation | Gamepad nav, 10-foot UI, Theme support, Plugin launcher, Phoenix integration |
| 04-Anti-Store | Phase 2 | Marketplace UI (browse, search, ratings, install/uninstall) |
| CYC-2026-030 | Phase 2 | GPG signatures for marketplace |
| 05-WebTV | Phase 2 | On-screen keyboard (2-3 weeks) |

---

## 1. Documentation Files to Update

| File | Phase 3 Change | Update Required |
|------|----------------|-----------------|
| docs/theming-skinning.md | Theater Mode themes, 10-foot UI | Add Theater Mode theme section, 10-foot UI guidelines |
| docs/plugin-dev.md | Marketplace UI, GPG signing | Add marketplace feed format, signing workflow |
| docs/control-panel.md | Theater Mode entry point | Add Theater Mode toggle, gamepad config |
| Cycloside/Campfires/README.md | Phase 2 Complete, Phase 3 section | Add Phase 2 Complete, Phase 3 in progress |

---

## 2. API / Code References to Update

| Location | Change |
|----------|--------|
| New: `Cycloside.TheaterMode` or equivalent | Document project structure |
| New: `UnifiedInputQueue` (from Phase 2) | Document for Theater Mode input |
| PluginRepository / Marketplace | GPG verification, signature display |
| New: On-screen keyboard | Document for gamepad/touch input |

---

## 3. New Documentation to Create (Phase 3)

| Document | Content |
|----------|---------|
| docs/theater-mode.md | Theater Mode architecture, gamepad nav, 10-foot UI, plugin launcher |
| docs/marketplace-feed-format.md | JSON/RSS feed schema for plugin/theme discovery |
| docs/gpg-signing.md | Creator signing workflow, verification, trust model |
| docs/on-screen-keyboard.md | Layouts, gamepad input, integration with Theater Mode |

---

## 4. Verification Checklist (Post-Phase 3)

- [ ] docs/theater-mode.md created
- [ ] docs/marketplace-feed-format.md created
- [ ] docs/gpg-signing.md created
- [ ] docs/on-screen-keyboard.md created (or merged into theater-mode.md)
- [ ] docs/theming-skinning.md updated for Theater Mode
- [ ] docs/plugin-dev.md updated for marketplace + GPG
- [ ] Cycloside/Campfires/README.md Phase 2 Complete, Phase 3 section

---

## 5. Out of Scope (Phase 4+)

- Wayland compositor
- Full WebTV appliance modes
- Community forums/Discord
- IPFS/P2P distribution

# Cycloside Governance

**Version:** 1.0  
**Date:** March 14, 2026  
**Status:** Draft (community review in progress)

---

## Mission

Cycloside is a **community-owned desktop customization platform** dedicated to resurrecting the culture that corporations killed. We exist to ensure:

1. **User ownership** (your desktop is yours to hack)
2. **Community control** (not corporate control)
3. **Permanence** (Cycloside cannot be killed or captured)
4. **Freedom** (federated, open source, no gatekeepers)

This governance document ensures Cycloside remains true to that mission.

---

## Principles

### 1. Community First
- Users and contributors control Cycloside's direction
- No single person or company owns the project
- Decisions are transparent and documented

### 2. Open Source Forever
- Cycloside is GPL v3 (strong copyleft)
- All code, docs, and assets are publicly accessible
- Forks are welcomed, not feared

### 3. No Gatekeepers
- Federated marketplace (anyone can host a feed)
- No approval process for themes/plugins
- No platform tax (0% cuts)

### 4. Meritocracy + Democracy
- Contributors earn influence through work
- Major decisions require community consensus
- Maintainers can be recalled if they violate principles

### 5. Long-term Thinking
- Cycloside is built to outlive its founders
- Governance can evolve, but principles cannot be removed
- Foundation or conservancy membership planned

---

## Roles

### Users
- Anyone who installs and uses Cycloside
- Can report bugs, request features, participate in forums
- Vote in community polls (when implemented)

### Contributors
- Anyone who submits code, docs, themes, or plugins
- Recognized in CONTRIBUTORS.md
- Earn reputation through merged contributions

### Maintainers
- Contributors with commit access to core repos
- Review and merge pull requests
- Enforce code of conduct
- **Current maintainers:** Justin (founder), [to be expanded]

### Steering Committee
- 3-7 maintainers elected by contributors
- Make high-level decisions (roadmap, branding, partnerships)
- Resolve disputes
- Serve 1-year terms (renewable)
- **Current status:** Not yet formed (project too early)

### BDFL (Benevolent Dictator For Life)
- **Current BDFL:** Justin (founder)
- Can override decisions in emergencies
- Expected to resign when Steering Committee is formed
- Cannot change GPL v3 license or core principles

---

## Decision Making

### Routine Decisions (bugs, features, docs)
- **Process:** Pull request → maintainer review → merge
- **Timeline:** 2-7 days
- **Veto:** Maintainers can veto, but must explain publicly

### Major Decisions (roadmap, architecture, partnerships)
- **Process:** RFC (Request for Comments) → community discussion → Steering Committee vote
- **Timeline:** 2-4 weeks
- **Threshold:** 2/3 majority of Steering Committee

### Critical Decisions (license, principles, foundation)
- **Process:** RFC → community discussion → contributor vote
- **Timeline:** 4-8 weeks
- **Threshold:** 2/3 majority of active contributors (10+ merged PRs in past year)

### Emergency Decisions (security, legal, crisis)
- **Process:** BDFL or Steering Committee decides immediately
- **Accountability:** Must explain publicly within 7 days
- **Review:** Community can challenge via recall vote

---

## Code of Conduct

Cycloside is built by and for a diverse community. We require:

1. **Respect:** No harassment, discrimination, or personal attacks
2. **Inclusivity:** Welcome newcomers, be patient, share knowledge
3. **Constructive criticism:** Critique ideas, not people
4. **Good faith:** Assume positive intent unless proven otherwise
5. **On-topic:** Keep discussions relevant to Cycloside

**Violations:**
- First offense: Warning
- Second offense: Temporary ban (7-30 days)
- Third offense: Permanent ban

**Enforcement:** Maintainers handle violations. Appeals go to Steering Committee.

**Full Code of Conduct:** [Contributor Covenant v2.1](https://www.contributor-covenant.org/)

---

## Contribution Process

### For Code/Docs
1. Fork the repo
2. Create a branch (`feature/your-feature` or `fix/bug-description`)
3. Make changes, write tests, update docs
4. Submit pull request
5. Address review feedback
6. Maintainer merges

### For Themes/Plugins
1. Build your theme/plugin using SDK
2. Test locally
3. Publish to your own marketplace feed (or submit to community feed)
4. Share on forums/gallery

**No approval required for themes/plugins. That's the point.**

---

## Conflict Resolution

### Step 1: Discussion
- Try to resolve disagreement in PR/issue comments
- Stay respectful, assume good faith

### Step 2: Maintainer Mediation
- If discussion stalls, request maintainer to mediate
- Maintainer reviews both sides, makes recommendation

### Step 3: Steering Committee
- If mediation fails, escalate to Steering Committee
- Committee discusses and votes (2/3 majority)

### Step 4: Fork
- If you fundamentally disagree with Cycloside's direction, fork it
- GPL v3 guarantees your right to fork
- Forks are not failures—they're features

---

## Foundation / Legal Entity

**Current Status:** Cycloside is an informal project with no legal entity.

**Future Plan (when project is mature):**
- Join existing foundation (Software Freedom Conservancy, Linux Foundation, Apache Foundation)
- Or form new non-profit (501(c)(3) in US, or equivalent)
- Transfer trademarks and domain names to foundation
- Establish bylaws aligned with this governance doc

**Why a foundation?**
- Legal protection for contributors
- Accept donations (tax-deductible)
- Hire full-time maintainers
- Sign contracts (hosting, events, partnerships)

**Timeline:** After Phase 3 complete, when contributor base is stable.

---

## Trademark

**Current Status:** "Cycloside" is not a registered trademark.

**Future Plan:**
- Register "Cycloside" trademark
- Transfer to foundation
- Allow free use for:
  - Forks (as long as they're GPL v3)
  - Community projects
  - Non-commercial uses
- Require permission for:
  - Commercial SaaS offerings
  - Official merchandise
  - Misleading uses

**Why trademark?** Prevent Microsoft from registering "Cycloside" and suing us.

---

## Amendments

This governance document can be amended via Critical Decision process:
1. RFC proposing changes
2. Community discussion (4-8 weeks)
3. Contributor vote (2/3 majority)

**Core principles cannot be removed:**
- GPL v3 licensing
- Community control
- Federated marketplace
- No gatekeepers

These can only be made **stronger**, not weaker.

---

## Evolution Path

### Phase 1 (Current): Informal Governance
- BDFL (Justin) makes decisions
- Contributors submit PRs
- No formal structure

### Phase 2 (After 10+ active contributors): Maintainer Team
- Form maintainer team (3-5 people)
- Distribute commit access
- BDFL delegates routine decisions

### Phase 3 (After 50+ active contributors): Steering Committee
- Elect Steering Committee (5-7 people)
- BDFL becomes "tie-breaker" only
- Major decisions require committee vote

### Phase 4 (After foundation formed): Community Governance
- BDFL resigns or becomes "emeritus"
- Steering Committee fully autonomous
- Foundation board oversees legal/financial

**Timeline:** Phase 2 by end of 2026, Phase 3 by end of 2027, Phase 4 TBD.

---

## Inspiration

This governance model draws from:
- **Debian Social Contract** (community principles)
- **Ubuntu Code of Conduct** (inclusivity)
- **Linux Foundation** (foundation structure)
- **Rust Governance** (RFC process)
- **GNOME Foundation** (contributor-driven)

We stand on the shoulders of giants.

---

## Questions?

- **"Who decides roadmap?"** Currently BDFL (Justin). Future: Steering Committee.
- **"Can I fork Cycloside?"** Yes. GPL v3 guarantees it.
- **"How do I become a maintainer?"** Contribute consistently, earn trust, ask.
- **"What if I disagree with a decision?"** Follow conflict resolution process, or fork.
- **"Can Microsoft buy Cycloside?"** They can buy trademarks, but not GPL v3 code. Anyone can fork.

---

## Contact

- **Governance questions:** Open an issue with `[governance]` tag
- **Code of Conduct violations:** [To be added - moderator contact]
- **Steering Committee (when formed):** [To be added]

---

**Cycloside is community-owned. This document ensures it stays that way.**

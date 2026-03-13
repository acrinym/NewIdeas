# The Anti-Store Manifesto

**Date:** March 12, 2026

**Core Truth:** One of the main points of Cycloside was to say **FUCK YOU MICROSOFT FUCKING STORE!**

---

## What Microsoft Store (and Apple App Store, and Google Play) Represent

- **30% cuts** on all transactions
- **Arbitrary rejection** of apps for vague "policy violations"
- **Forced updates** you can't control
- **Telemetry and surveillance** baked into the distribution model
- **Algorithmic suppression** of apps they don't like
- **Pay-to-play visibility** (featured apps are often paid placements)
- **Vendor lock-in** (can't move your purchases to another platform)
- **Censorship** disguised as "content guidelines"
- **Death by committee** (every update needs approval)

---

## What Cycloside's Model Should Be

**Direct. Open. Ungated. Community-owned.**

### The Cycloside Marketplace Architecture

#### 1. Federated, Not Centralized

The "official" marketplace is just ONE feed. Anyone can host their own:
- Community feeds (e.g., `retro-games.cycloside.community`)
- Personal feeds (host your own plugin repo)
- Mirror feeds (distributed, no single point of failure)
- Curated feeds (security-focused, performance-focused, aesthetic-focused)

You add feeds by URL. No permission needed. No platform approval. Just RSS-style subscriptions to theme/plugin feeds.

#### 2. Zero-Cut, Optional Tips

- All plugins/themes are **direct downloads** from creators
- No mandatory payment processing through Cycloside
- Optional: creators can add a tip jar link (PayPal, Ko-fi, crypto, whatever)
- Optional: creators can add a "pro version" purchase link to their own site
- **Cycloside takes 0%**—we're infrastructure, not landlords

#### 3. No Approval Process

Upload a plugin → it's live. That's it. No review queue, no waiting, no arbitrary rejections.

**Safety model:**
- Community ratings and reviews (transparent, not algorithmic)
- Digital signatures (GPG-style) so you can verify creator identity
- Sandboxing options for untrusted plugins (run in restricted mode)
- Open source encouraged (source links displayed prominently)
- "Verify checksum" built into installer

**Trust is decentralized.** You trust creators, not a corporation.

#### 4. Git-Native Distribution

Plugins and themes are just **Git repos**. The marketplace UI is a pretty frontend for:
```bash
git clone https://github.com/CoolCreator/CyberpunkTheme.git
```

Updates? `git pull`. Fork a theme? `git fork`. Contribute? `git push`. 

This means:
- **Full version history** (see every change, roll back if needed)
- **True ownership** (you have the code, forever)
- **Decentralized hosting** (GitHub, GitLab, self-hosted, mirrors)
- **Collaboration built-in** (pull requests, issues, wikis)

#### 5. P2P as Backup Option

Optional: enable P2P distribution via IPFS or BitTorrent for popular plugins/themes. No central server can go down and break installations. The community keeps things alive.

#### 6. No Platform Politics

Cycloside doesn't ban themes or plugins based on content (within legal bounds). If someone makes a "Trump 2028 MAGA Theme" or a "Cyberpunk Anarchist Toolkit," that's their right. Users decide what to install.

**No algorithmic suppression. No shadow banning. No deplatforming.**

If communities want to curate their own feeds, they can. But the *official* feed is neutral infrastructure.

---

## How This Positions Cycloside

### Against Microsoft Store
- "Install anything, no gatekeepers"
- "Your desktop, your rules"
- "Zero platform tax"

### Against Apple Walled Garden
- "We don't decide what you can run"
- "Sideloading isn't a workaround—it's the default"

### Against Steam (yes, even Steam)
- "No DRM, no account required"
- "You own what you download"
- "Offline-first"

### The Pitch

> **Cycloside: The desktop that doesn't ask permission.**
> 
> No app store. No approval process. No platform cuts.  
> Just you, your computer, and a community of builders.

---

## Technical Architecture for This Vision

### 1. Plugin Loading Must Be Bulletproof

Since there's no review process, you need:

**Sandboxing:** Untrusted plugins run in restricted mode (no file system access outside their folder, no network unless explicitly granted)

**Permission system:** Like Android but less annoying—"This plugin wants to: access network, read clipboard, run at startup"—user says yes/no

**Crash isolation:** A bad plugin crashes? It doesn't take down Cycloside

**Hot reload:** Install/uninstall plugins without restarting

### 2. Cryptographic Signing

Creators can sign their plugins with GPG keys. Cycloside verifies signatures and shows:
- ✅ "Signed by CoolCreator (verified identity)"
- ⚠️ "Unsigned plugin from unknown source"
- ❌ "Signature invalid—this may be tampered"

Users can choose to only install signed plugins if they want.

### 3. Transparency Logs

Optional: public append-only log of all marketplace submissions (like Certificate Transparency). Anyone can audit what's being distributed. If something malicious appears, the community can flag it.

### 4. Reputation Without Censorship

Instead of "approved/rejected," it's:
- Star ratings
- Review comments
- Install counts
- "Trusted creator" badges (earned, not bought)
- "Community verified" flag (multiple people reviewed the source)

You see a 1-star plugin with 10 installs and no reviews? Probably skip it. You see a 4.8-star plugin with 50,000 installs and active GitHub? Probably safe.

### 5. Local-First by Default

Marketplace browsing can work offline if you've cached the feed. Installing from local `.zip` files is fully supported. No mandatory cloud accounts.

---

## Business Model Without Platform Tax

If you're not taking a cut, how does Cycloside sustain itself?

### Option 1: Patreon/Sponsorware Model
- Core Cycloside is free forever
- Early access to new features for sponsors
- Sponsors get their name in the credits
- Community-funded, like Blender or Krita

### Option 2: "Official" Premium Themes
- You (the Cycloside team) sell a few really high-quality theme packs directly
- Like Panic's themes for Nova editor—optional, high-quality, fairly priced
- Revenue goes to funding development

### Option 3: Pro Features as One-Time Purchase
- Cycloside Core: free
- Cycloside Pro: one-time $29 purchase for:
  - Advanced effect editor
  - Workspace cloud sync
  - Priority support
  - Extra official themes
- Still no subscription, still no store

### Option 4: Consulting/Custom Development
- Companies want Cycloside customized for their team? You do paid consulting
- Like how Linux distros make money

### Option 5: Pure Donation/FOSS Model
- Just ask for donations
- "Pay what you want" on the website
- Cycloside stays 100% free forever

**Recommended approach:** Combination of Option 1 (Patreon) + Option 5 (donations), with Option 3 (Pro features) as optional expansion later.

---

## The Cultural Messaging

This isn't just technical—it's **ideological**. Cycloside should lean into this:

### Website Copy

> "No app stores. No gatekeepers. No bullshit.  
> Your computer belongs to you—act like it."

### Manifesto on the About Page

> We don't believe corporations should decide what software you can run.  
> We don't believe in 30% platform taxes.  
> We don't believe in walled gardens.  
> 
> Cycloside is free software, built by a community, for a community.  
> Install what you want. Build what you want. Share what you want.  
> 
> Your desktop, your rules.

### Attract the Right People

- Privacy advocates
- Anti-surveillance folks
- Indie developers tired of platform fees
- Tinkerers and hackers
- People who miss when computers were fun

---

## Comparison: Cycloside vs. Platforms

| Feature | Microsoft Store | Apple App Store | Steam | Cycloside Marketplace |
|---------|----------------|-----------------|-------|----------------------|
| **Platform Cut** | 30% | 30% | 30% | 0% |
| **Approval Process** | Yes, opaque | Yes, very strict | Yes, minimal | None |
| **Censorship** | Heavy | Very heavy | Moderate | None (legal compliance only) |
| **DRM** | Optional | Built-in | Built-in | None |
| **Offline Install** | No | No | Limited | Yes, always |
| **Account Required** | Yes | Yes | Yes | No |
| **Decentralized** | No | No | No | Yes |
| **Open Source Support** | Poor | No | Poor | Encouraged |
| **Creator Control** | Low | Very low | Moderate | Total |
| **Update Control** | Platform | Platform | Platform | User |
| **Sideloading** | Blocked/difficult | Blocked/difficult | Discouraged | Default method |

---

## Implementation Checklist

### Phase 1: Foundation (3 months)
- [ ] Plugin sandboxing system
- [ ] Permission model (network, filesystem, clipboard, etc.)
- [ ] GPG signing verification
- [ ] Basic marketplace feed format (JSON/RSS)
- [ ] Local plugin installation from ZIP

### Phase 2: Marketplace UI (3 months)
- [ ] Browse plugins/themes
- [ ] Search and filter
- [ ] Ratings and reviews
- [ ] Install/uninstall UI
- [ ] Update notifications

### Phase 3: Decentralization (3 months)
- [ ] Multiple feed sources
- [ ] Community feed hosting guide
- [ ] IPFS/P2P distribution (optional)
- [ ] Git-native plugin repos
- [ ] Transparency logs

### Phase 4: Creator Tools (3 months)
- [ ] Plugin submission tool
- [ ] Signing tool for creators
- [ ] Creator dashboard (stats, reviews)
- [ ] Documentation and guides
- [ ] Community forums/Discord

---

## The Vision Statement

**Cycloside Marketplace exists to prove that software distribution doesn't require corporate control.**

We believe:
- Creators should own their work
- Users should control their software
- Communities should govern themselves
- Trust should be earned, not bought
- Freedom should be the default

**No gatekeepers. No platform tax. No bullshit.**

**Just builders and users, connecting directly.**

---

## Inspiration and Attribution

This model draws from:
- **F-Droid** (Android app repository without Google)
- **Homebrew** (decentralized package manager)
- **Git/GitHub** (distributed version control)
- **IPFS** (peer-to-peer distribution)
- **Usenet** (federated message distribution)
- **Winamp skins** (community-driven customization)
- **DeviantArt** (early 2000s, pre-corporate)
- **itch.io** (indie game distribution with optional payments)

We're standing on the shoulders of giants who believed software should be free (as in freedom).

---

## Call to Action

**For Users:**
Download Cycloside. Install plugins. Share your setup. Support creators directly.

**For Creators:**
Build plugins. Share themes. Host your own feeds. Keep 100% of what you earn.

**For Developers:**
Contribute to Cycloside core. Improve the marketplace. Build federation tools.

**For Community:**
Run mirrors. Curate feeds. Review plugins. Help newcomers.

**Together, we can build a desktop ecosystem that's actually ours.**

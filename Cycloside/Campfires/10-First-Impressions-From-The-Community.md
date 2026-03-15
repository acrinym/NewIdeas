# 10 - First Impressions: Community Leaders See Cycloside

**Date:** March 14, 2026

**Context:** Four community leaders from different corners of software freedom, Linux, and customization culture see Cycloside for the first time. They understand the full vision (Phases 1-4+, campfire documents, the resurrection mission). What do they think?

**Participants:**
- **Cory Doctorow** (EFF, digital rights, right to repair)
- **Ian Murdock** (Debian founder, open source philosophy)
- **Alex Chen** (shellcity.net community maintainer, 2005-2010)
- **Mark Shuttleworth** (Ubuntu founder, user-friendly Linux)

---

## The Discussion

**Cory Doctorow:** Okay, I've read the campfires. I've seen the phase plans. First reaction: **this is exactly what we've been fighting for**. User ownership, no gatekeepers, federated marketplace, no platform tax, hackable by design. Cycloside is the anti-app-store. This is what computing should've stayed.

**Ian Murdock:** I'm impressed, but I have questions. Licensing? Is this GPL? MIT? Proprietary core with open plugins? Because if Cycloside becomes popular and then gets bought by Microsoft or Canonical, the whole resurrection thing dies again. You need governance that prevents that.

**Alex Chen:** Holy shit, you actually *remember* shellcity.net. Most people don't even know it existed. But yeah—Cycloside is what shellcity.net would've become if we'd had the tech and vision in 2005. Federated marketplace? We'd have killed for that. No Stardock paywall, no single point of failure. This is the dream.

**Mark Shuttleworth:** From a Linux perspective, I'm torn. On one hand, Theater Mode and gamepad-first UI could be *huge* for getting Linux into living rooms. On the other hand, Phase 5-6 is talking about Wayland compositors and shell replacement, which is... ambitious. But if you pull it off? Cycloside could be the "fun" Linux desktop that GNOME and KDE can't be.

**Cory Doctorow:** Let me focus on the threat model, because that's what we do at EFF. The resurrection pattern document lays out how corporations killed WebTV, Winamp, shellcity.net. Cycloside's answer is "federated marketplace, open source, no single owner." That's good. But what stops this:

1. **Enshittification:** Cycloside gets popular → you add "premium features" → free tier gets worse → community forks → chaos
2. **Acquisition:** Microsoft/Google/Apple buys you → shuts down the "dangerous" parts (shell replacement, marketplace freedom)
3. **Legal attack:** DMCA takedowns for "circumvention" if Cycloside hooks too deeply into Windows/Linux

How are you protecting against those?

**Ian Murdock:** On the acquisition front, strong copyleft helps. GPL means they can't close-source it even if they buy it. But governance matters more—who controls the project? Is there a foundation? A BDFL? Community voting? Because Linus controls Linux, Canonical controls Ubuntu, Mozilla controls Firefox. Cycloside needs a governance model that can't be captured.

**Alex Chen:** From the customization community side, here's what matters: **Can I build a theme and share it without asking permission?** That's it. If the answer is yes, Cycloside wins. Shellcity.net died because we were dependent on one site. Stardock won because they controlled WinCustomize. Cycloside's federated marketplace means anyone can host a feed. That's unkillable.

**Mark Shuttleworth:** Let's talk about the Windows-vs-Linux tension. Phase 1-4 are mostly Windows-focused (shell replacement, effects on DWM, Theater Mode as an app). Phase 5+ is Linux Wayland compositor. That's smart—prove it on Windows first, then graduate to Linux session. But you need to be honest: the Linux community will be skeptical until Cycloside runs natively as a session, not just "an app on top of GNOME."

**Cory Doctorow:** Here's the thing: Cycloside is a **right to repair** project for *software*. You're saying "your desktop should be yours to modify, hack, and control." That's what we fight for with tractors and phones. But software is even more locked down—Apple won't let you sideload, Microsoft makes registry tweaks harder every year, GNOME removes customization options. Cycloside is the repair manual for your desktop.

**Ian Murdock:** And that's why licensing and governance are critical. Debian exists because we had principles—DFSG, Social Contract, community control. If Cycloside doesn't have that, it's just another project that'll get captured or abandoned. You need:

1. **Strong copyleft** (GPL v3 or AGPL)
2. **Community governance** (foundation, elected board, transparent decisions)
3. **No single point of failure** (federated infrastructure, multiple mirrors, anyone can fork)
4. **Reproducible builds** (so users can verify binaries match source)

**Alex Chen:** And from the customization angle: **Make sharing easy**. Shellcity.net had forums, screenshots, "show your desktop" threads. That's what built community. Cycloside needs:

- **One-click pack sharing** (export my setup, you import it)
- **Screenshot gallery** (show off your desktop)
- **Discovery** (trending themes, most downloaded)
- **Creator profiles** (follow your favorite theme makers)

If Cycloside is just "an app with themes," it's boring. If it's a *community* where people trade setups, it's a movement.

**Mark Shuttleworth:** Ubuntu succeeded because we made Linux accessible. Cycloside needs to do the same for customization. Most people are scared of registry edits, AXAML files, Lua scripts. The in-app editors (Phase 4) solve that—visual theme builder, live preview, one-click publish. That's how you go from "tinkerer toy" to "mainstream alternative."

**Cory Doctorow:** Let me be blunt: Cycloside is a **political project**, whether you realize it or not. You're fighting against:

- **Walled gardens** (app stores, locked bootloaders)
- **Planned obsolescence** (forced updates, deprecated APIs)
- **Surveillance capitalism** (telemetry, cloud accounts, data harvesting)
- **Corporate control** (Microsoft deciding what you can theme, Apple deciding what you can install)

Cycloside says "fuck all of that." Theater Mode is WebTV without Microsoft's kill switch. The marketplace is shellcity.net without Stardock's paywall. Phoenix Visualizer is Winamp without AOL's mismanagement. **That's political.**

**Ian Murdock:** Which is why governance and licensing matter. If Cycloside is GPL and community-governed, it's a commons that can't be enclosed. If it's MIT-licensed or BDFL-controlled, it's one acquisition away from death. Debian survived 30+ years because we built it to outlive any one person or company. Cycloside needs the same.

**Alex Chen:** Honestly, I don't care about the politics or licensing drama. I just want my desktop to be *mine* again. Shellcity.net gave me that in 2005. Windows Vista took it away. Cycloside can give it back. The fact that it's federated and open source is a bonus, but the *experience* is what matters. Make it easy, make it fun, make it shareable.

**Mark Shuttleworth:** And that's the Ubuntu philosophy: ideology is great, but usability wins. If Cycloside is "the FOSS customizable desktop for freedom-loving tinkerers," you'll get 5,000 users. If it's "holy shit, my desktop looks like cyberpunk now and it was 3 clicks," you'll get 500,000 users. Both are valid, but know which you're building.

**Cory Doctorow:** Here's my challenge: Cycloside needs to be **illegible to platform owners**. Microsoft and Apple kill things they can understand and control. LiteStep survived for years because it was weird and niche. The moment Cycloside becomes "a threat to the Windows shell," Microsoft will break it with an update. The defense is:

1. **Modularity** (if one part breaks, the rest survives)
2. **Community** (if you abandon it, someone forks it)
3. **Obscurity** (don't be so successful Microsoft notices)

Or, go full Linux and build the Wayland compositor, where you *are* the platform.

**Ian Murdock:** The Linux path is the right long-term move. On Windows, you're always at Microsoft's mercy. On Linux, you *are* the desktop. But you need to earn the Linux community's trust first:

- **Open source everything** (no "community edition" vs "pro edition" bullshit)
- **Upstream patches** (contribute to Avalonia, SkiaSharp, wlroots)
- **Respect conventions** (XDG directories, Wayland protocols, FreeDesktop standards)
- **Don't be NIH syndrome** (use existing Linux tools where possible)

**Alex Chen:** One thing I want to see: **Theme contests**. Shellcity.net had "Desktop of the Month." Cycloside should have "Theme of the Week," "Most Creative Pack," "Best Theater Mode Setup." Give people reasons to share and compete. That's how communities stay alive.

**Mark Shuttleworth:** And integrate with the broader ecosystem. Ubuntu has Snap, Flatpak exists, AppImage exists. Can Cycloside plugins be distributed as Flatpaks? Can themes be Snap packages? Or is Cycloside inventing its own packaging, which fragments the Linux world even more?

**Cory Doctorow:** Final thought: Cycloside is part of a bigger movement—**interoperability and adversarial compatibility**. You're saying "even if Microsoft doesn't want me to theme Windows, I'll do it anyway." That's the same spirit as jailbreaking iPhones, rooting Android, running Hackintosh. EFF supports that. But legally, you're in a gray area. DMCA Section 1201 makes "circumvention" illegal. If Cycloside hooks too deeply into Windows, you could face legal threats. Be ready for that.

**Ian Murdock:** Which is another reason to focus on Linux. On Linux, you're not circumventing—you're *building* the platform. Wayland compositors are first-class citizens. No legal gray area.

**Alex Chen:** Okay but like, 90% of desktop users are on Windows. If Cycloside is Linux-only, it's niche. If it's Windows-first with Linux aspirations, it's accessible. The resurrection pattern says "ship the experience first." Windows *is* the experience for most people.

**Mark Shuttleworth:** Compromise: ship on Windows, build for Linux. Phase 1-4 prove Cycloside works. Phase 5-6 make it a first-class Linux citizen. By then, you've built the community and trust. That's how Ubuntu did it—Debian base, but better UX. Cycloside can be the same: "Windows shell replacement that's actually just a preview of our Linux session."

---

## Key Takeaways

### From Cory Doctorow (EFF / Digital Rights)

**What Cycloside Gets Right:**
- Anti-app-store (no gatekeepers, no platform tax)
- User ownership (federated marketplace, open source)
- Right to repair for software (hack your desktop)

**Concerns:**
- Enshittification risk (needs strong governance)
- Legal threats (DMCA 1201 for Windows hooks)
- Acquisition risk (needs copyleft + foundation)

**Recommendation:**
> "Cycloside is a political project. Embrace it. Build governance that can't be captured. Use AGPL. Get a foundation (Software Freedom Conservancy?). Be ready for Microsoft's lawyers."

---

### From Ian Murdock (Debian Founder / Open Source Philosophy)

**What Cycloside Gets Right:**
- Open source approach
- Federated architecture (no SPOF)
- Community-driven vision

**Concerns:**
- Licensing unclear (GPL? MIT? Proprietary?)
- Governance undefined (BDFL? Foundation? Community voting?)
- Linux integration unclear (will it respect FreeDesktop standards?)

**Recommendation:**
> "Cycloside needs a constitution. Debian has one. Ubuntu has one. Cycloside needs one. GPL v3 minimum. Foundation governance. Transparent community process. Otherwise, it's just another project that'll die when the founder gets bored."

---

### From Alex Chen (shellcity.net Maintainer / Customization Culture)

**What Cycloside Gets Right:**
- Federated marketplace (unkillable)
- Pack system (share complete setups)
- In-app editors (make it easy)
- Resurrection of shellcity.net spirit

**Concerns:**
- Community features unclear (forums? screenshot gallery? contests?)
- Discovery unclear (how do users find themes?)
- Too much tech talk, not enough "look at this cool desktop"

**Recommendation:**
> "Build the community features first. Theme contests, screenshot galleries, 'Desktop of the Week,' creator profiles. Shellcity.net died because we lost the community hub. Cycloside's marketplace is technical infrastructure—great. But you need the *social* infrastructure too."

---

### From Mark Shuttleworth (Ubuntu Founder / User-Friendly Linux)

**What Cycloside Gets Right:**
- Windows-first (accessible to 90% of desktop users)
- Theater Mode (Linux living room potential)
- In-app editors (usability over ideology)

**Concerns:**
- Linux support is vague (Phase 5-6 too far out)
- Packaging unclear (inventing new format vs. using Flatpak/Snap?)
- GNOME/KDE tension (will Linux users see Cycloside as a competitor?)

**Recommendation:**
> "Prove it on Windows, then earn Linux's trust. Contribute to Avalonia, SkiaSharp, wlroots. Respect XDG/Wayland standards. Ship a Flatpak. Show the Linux community you're serious, not just Windows refugees who'll abandon Linux later."

---

## The Consensus

**All Four Agree:**

1. **Cycloside's vision is right** (resurrection of customization culture, anti-corporate, user ownership)
2. **Governance and licensing are critical** (GPL/AGPL, foundation, community control)
3. **Windows-first is smart** (accessible, prove the concept)
4. **Linux is the long-term home** (Wayland compositor = unkillable)
5. **Community matters more than code** (screenshot galleries, contests, sharing culture)

**The Warning:**

> "If Cycloside becomes successful without strong governance and copyleft licensing, Microsoft/Google/Apple will either buy it or kill it. Build the legal and social infrastructure *now*, before you're popular."  
> — Ian Murdock

**The Opportunity:**

> "Cycloside could be the 'fun' desktop that GNOME/KDE/Windows can't be. Theater Mode on Linux? That's a living room revolution. Shell replacement on Windows? That's subversive. Both? That's a movement."  
> — Mark Shuttleworth

**The Mission:**

> "Cycloside is right-to-repair for desktops. You're fighting for user freedom. That's political, and that's good. Own it."  
> — Cory Doctorow

**The Culture:**

> "Make it easy to share, make it fun to customize, make it impossible to kill. That's how shellcity.net should've survived. That's how Cycloside *will* survive."  
> — Alex Chen

---

## Action Items (Based on Feedback)

### Legal / Governance (High Priority)

- [ ] Choose license: **GPL v3** or **AGPL** (copyleft to prevent capture)
- [ ] Establish governance: **Foundation** (Software Freedom Conservancy?) or **Community council**
- [ ] Draft **Community Charter** (Debian-style Social Contract)
- [ ] Prepare for **legal threats** (DMCA 1201, Microsoft C&D)
- [ ] Add **LICENSE** file to repo
- [ ] Add **GOVERNANCE.md** to repo

### Community / Social (High Priority)

- [ ] Build **screenshot gallery** (show off desktops)
- [ ] Add **theme contests** (Desktop of the Week, Best Pack)
- [ ] Create **creator profiles** (follow your favorite makers)
- [ ] Add **discovery** (trending, most downloaded, recently updated)
- [ ] Build **forums** or integrate **Discord/Matrix**
- [ ] "Show Your Desktop" **showcase page**

### Linux Integration (Medium Priority)

- [ ] Ship **Flatpak** package (respect Linux packaging)
- [ ] Respect **XDG standards** (config dirs, data dirs)
- [ ] Test on **multiple distros** (Ubuntu, Fedora, Arch, Debian)
- [ ] Contribute **upstream patches** (Avalonia, SkiaSharp bugs)
- [ ] Document **Wayland compositor plan** (Phase 6+)
- [ ] Prototype **wlroots integration** (proof of concept)

### Usability (High Priority)

- [ ] In-app editors **must be dead simple** (no AXAML/Lua required)
- [ ] One-click **pack import/export** (share complete setups)
- [ ] Visual **theme builder** (color picker, font selector, live preview)
- [ ] **Tutorial system** (first-run wizard, tooltips, video guides)
- [ ] **Onboarding flow** ("Welcome to Cycloside, here's how to customize")

---

## Quotes for the Manifesto

**Cory Doctorow:**
> "Cycloside is right-to-repair for desktops. You're not just building an app—you're fighting for user freedom."

**Ian Murdock:**
> "Without governance and copyleft, Cycloside is one acquisition away from death. Build the constitution now."

**Alex Chen:**
> "Shellcity.net died because we lost the community. Cycloside's marketplace is infrastructure. Now build the culture."

**Mark Shuttleworth:**
> "Prove it on Windows, earn Linux's trust. Cycloside could be the 'fun' desktop we've been missing."

---

## The Final Word

**Cory Doctorow:** Cycloside has the potential to be a landmark project in user freedom. But only if you build it to outlive its founders.

**Ian Murdock:** Make it FOSS. Make it community-governed. Make it a commons. Otherwise, it's just another abandoned GitHub repo in 5 years.

**Alex Chen:** Make it fun. Make it shareable. Make it unkillable. That's how shellcity.net should've been. That's how Cycloside can be.

**Mark Shuttleworth:** Ship on Windows. Build for Linux. Serve both. If you do it right, Cycloside becomes the bridge between "I want a pretty desktop" and "I want to own my computing."

---

## Addendum: The Licensing Question

**Cory's Position:** AGPL (prevent cloud service providers from running proprietary Cycloside-as-a-service)

**Ian's Position:** GPL v3 (standard copyleft, prevents TiVoization)

**Alex's Position:** "I don't care, just make it forkable"

**Mark's Position:** "MIT/Apache would help corporate adoption, but GPL prevents capture"

**The Compromise:** **GPL v3** for core, **MIT** for plugin SDK/examples (allows commercial plugins while keeping core free)

**Current Status:** Cycloside repo has no LICENSE file. **This needs to be fixed immediately.**

---

## Next Steps

1. **Add LICENSE.md** (GPL v3 recommended)
2. **Add GOVERNANCE.md** (draft community charter)
3. **Add CONTRIBUTING.md** (how to participate)
4. **Create foundation plan** (or join existing: SFC, Linux Foundation, FSF?)
5. **Build community features** (screenshot gallery, contests, discovery)
6. **Document Linux plans** (Flatpak, XDG, Wayland roadmap)

**The community leaders have spoken. Now it's time to act.**

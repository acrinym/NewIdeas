# Cycloside Campfires & Design Documents

**Date:** March 12, 2026

This directory contains campfire conversations and design documents that explore the vision, architecture, and philosophy of Cycloside.

---

## What Are Campfires?

Campfires are **multi-perspective exploratory conversations** where different viewpoints discuss a project or idea. They're not adversarial debates—they're collaborative explorations where perspectives can agree, overlap, or build on each other. The tone is conversational, with room for improvisation and "vibecoding" (ideas that would be cool, feel good, or be fun to build).

---

## Document Index

### Campfire Sessions

#### [01 - Building a Display Server](01-Building-Display-Server.md)
**Participants:** Linus Torvalds, Linus Tech Tips, Steve Ballmer, Steve Jobs

**Topic:** How to build or implement an X11/Xorg/Wayland-like system for Cycloside with custom window decorations, themes, and graphics.

**Key Takeaways:**
- Don't build X11 from scratch—it's legacy and overengineered
- Wayland compositor is realistic but a multi-year commitment
- Shell replacement on Windows is more achievable
- Ship the experience first, infrastructure later
- Cycloside should BE the compositor: every surface native to the scene graph

---

#### [02 - The Bigger Vision](02-The-Bigger-Vision.md)
**Participants:** Steve Jobs, Bill Gates, Steve Perlman (WebTV), Michael Dell

**Topic:** What could be done with Cycloside? Why? Where can it be improved? What directions can it take?

**Key Takeaways:**
- Core identity: Your desktop is your canvas (personal expression over corporate constraints)
- Three editions strategy: Studio, Hacker, Retro
- Themes as complete experiences (not just colors)
- Self-modifying environment (write plugins inside Cycloside)
- Community is the product
- Maker-friendly integration (MIDI, OSC, serial)
- Quality over quantity: nail 5 core features first

---

### Design Documents

#### [03 - Personal Expression Software Lineage](03-Personal-Expression-Software-Lineage.md)

**Topic:** The historical and cultural roots of Cycloside's vision, tracing the lineage of personal expression software from the early 2000s.

**Covers:**
- MySpace, Xanga, HyperStudio, Kid Pix, WebTV
- The golden age of desktop customization
- What killed the scene (Stardock paywalls, app stores, corporate aesthetics)
- What each piece teaches Cycloside
- Future plugin ideas inspired by this lineage
- The manifesto: resurrecting a murdered culture

**Key Quote:**
> "Cycloside isn't just 'a customizable desktop app.' It's **resurrecting an entire culture that corporations murdered.**"

---

#### [04 - Anti-Store Manifesto](04-Anti-Store-Manifesto.md)

**Topic:** Why Cycloside must avoid becoming a gatekeeper, and how to build a truly open, federated marketplace.

**Core Principle:** **FUCK YOU MICROSOFT FUCKING STORE!**

**Covers:**
- What app stores represent (30% cuts, arbitrary rejection, censorship)
- Federated marketplace architecture (anyone can host feeds)
- Zero-cut, optional tips model
- No approval process (trust is decentralized)
- Git-native distribution
- P2P backup option
- No platform politics
- Business models without platform tax

**Key Quote:**
> "No app store. No approval process. No platform cuts. Just you, your computer, and a community of builders."

---

#### [05 - WebTV Source Code Reconnaissance](05-WebTV-Source-Reconnaissance.md)

**Topic:** Insights extracted from the WebTV Classic 2 LC2 source code for building Cycloside Theater Mode.

**Source Location:** `D:\Downloads\Source\wtv-classic2lc2-src.tar\`

**Covers:**
- Input handling architecture (ring buffer, device-agnostic, auto-repeat suppression)
- On-screen keyboard design (slide-in, auto-positioning, caps lock indicator)
- Remote control key mappings → gamepad mappings for Theater Mode
- Development philosophy and code style
- UI component structure (maps 1:1 to Avalonia!)
- Documentation insights
- Immediate actionable ideas

**Key Insight:**
> "WebTV in 1995 solved the EXACT problem Cycloside Theater Mode needs to solve: non-desktop computing, remote control navigation, 10-foot UI, media-first experience, accessible to non-technical users."

---

#### [06 - Kodi vs. Cycloside Theater Mode](06-Kodi-vs-Cycloside-Theater-Mode.md)

**Topic:** Why Kodi is too hard to use, and how Cycloside Theater Mode is different.

**Core Problem:** "We are now in XBC → KODI territory. But KODI is its own thing and I don't like it. Too hard to use."

**Covers:**
- What Kodi is and why it sucks (overwhelming complexity, XML hell, single-purpose)
- What Cycloside Theater Mode should be (living room playground, not media center)
- Comprehensive comparison chart
- Why Cycloside wins (personal, multi-purpose, community-driven, fun)
- Theater Mode UI sketch
- Feature roadmap
- Success criteria

**Key Quote:**
> "Kodi asks: 'What do you want to watch?' Cycloside Theater Mode asks: 'What do you want to do?' That's the difference."

---

#### [07 - Cycloside Vulnerability Explained](07-Cycloside-Vulnerability-Explained.md)

**Topic:** Deep-dive explanations of vulnerability classes and parser-level attacks discovered during security audits.

**Covers:**
- Parser confusion attacks (malformed XML bypassing string validators)
- XML bomb attacks (Billion Laughs / entity expansion)
- Error message information disclosure
- Encoding-based bypasses (Unicode tricks, BOM confusion)
- Proof-of-concept test files for validation

**New Vulnerabilities Identified:**
- **CYC-2026-019:** Parser confusion via malformed AXAML
- **CYC-2026-020:** XML bomb / entity expansion DoS
- **CYC-2026-021:** Error message information disclosure
- **CYC-2026-022:** BOM and encoding confusion

**Key Insight:**
> "String-based validation is insufficient for XML/XAML security. The validator checks *what the C# string contains*, but the XAML parser processes *what the XML means*."

**Includes:**
- `PoC-MalformedTheme.axaml.txt` - Test file demonstrating Unicode/entity/CDATA bypasses (renamed so build does not compile it)
- `PoC-XmlBomb.axaml.txt` - Test file demonstrating Billion Laughs attack (3GB expansion from 2KB file)

---

#### [07 - Cycloside as a Real Session](07-Cycloside-as-a-Real-Session.md)

**Participants:** Linus Torvalds, Steve Jobs, Bill Gates, Steve Perlman, Michael Dell

**Topic:** What Cycloside becomes if it grows from a shell app into a real session, scene engine, and platform for personal expression software.

**Key Takeaways:**
- Do not build a new X11
- Use X11 as a lab, not the destination
- Treat Wayland compositor work as a later Linux session phase
- Build Cycloside's own scene graph first
- Turn themes, skins, workspaces, and plugins into real packs
- The real product is a personal environment people can live inside

---

#### [08 - Lineage Nods, Theme Memorials, and Aha Moments](08-Lineage-Nods-Theme-Memorials.md)

**Topic:** Inspiration notes and product sparks about honoring the campfire participants and historical computing worlds through Cycloside-native base themes and shell packs.

**Covers:**
- Redmond Linux
- Classic Linux
- Pre-iOS Mac
- WebTV Night
- Beige Box Powerhouse
- Why homage matters without using anyone else's tech

**Key Quote:**
> "These are the house museum of the ideas that made Cycloside possible."

---

## Security Work

The security audit documented in [d:\GitHub\NewIdeas\docs\vulnerabilities\cycloside-vulnerability-catalog.md](../docs/vulnerabilities/cycloside-vulnerability-catalog.md) found and patched **18 vulnerabilities** before public release.

**Additional threat modeling discovered 13 MORE vulnerabilities,** bringing the total to **31:**

- **4 CRITICAL** - Remote code execution, path traversal, hash collisions
- **11 HIGH** - Memory exhaustion, script injection, DoS, format confusion, integrity bypass
- **16 MEDIUM** - Race conditions, thread safety, resource leaks, parser exploits

**Zero user exposure. 17 patched pre-release, 13 under active investigation, 1 confirmed open.**

### Security Documents

- **[07 - Cycloside Vulnerability Explained](07-Cycloside-Vulnerability-Explained.md)** - Parser-level attacks (CYC-2026-019 through 022)
- **[CYC-2026-023 to 026](CYC-2026-023-to-026.md)** - Format confusion attacks (data URIs, RIFF containers)
- **[CYC-2026-027 to 028](CYC-2026-027-to-028.md)** - ICO/CUR type confusion, WAV decoder exploits
- **[CYC-2026-029 Hash Collision Attacks](CYC-2026-029-Hash-Collision-Attacks.md)** - Cryptographic weakness analysis
- **[CYC-2026-030 No Integrity Validation](CYC-2026-030-No-Integrity-Validation.md)** - Plugin marketplace vulnerability 🔥
- **[CYC-2026-031 Recursive Inclusion](CYC-2026-031-Recursive-Inclusion.md)** - Circular reference DoS attacks
- **[SECURITY-SUMMARY.md](SECURITY-SUMMARY.md)** - Initial audit overview
- **[VULNERABILITY-DISCOVERY-SUMMARY.md](VULNERABILITY-DISCOVERY-SUMMARY.md)** - Complete catalog with discovery methodology

Document #07 expands on these with deep-dive explanations and proof-of-concept attacks that test parser-level vulnerabilities beyond string validation.

---

## Themes and Patterns

### Recurring Ideas Across All Documents

1. **Personal Expression Over Corporate Control**
   - Your desktop should be YOURS
   - No gatekeepers, no mandatory aesthetics
   - Freedom by default

2. **Community Ownership**
   - Federated, decentralized distribution
   - Zero platform tax
   - Users and creators connect directly
   - Social showcase culture

3. **Multi-Purpose by Design**
   - Not just a media player, not just themes
   - Desktop + Theater Mode + plugins + games + tools
   - Whatever you make it

4. **Accessibility and Delight**
   - Works immediately, customize later
   - Software should have soul (Kid Pix philosophy)
   - Fun, not just functional

5. **Standing on Shoulders**
   - Learn from WebTV's 10-foot UI mastery
   - Resurrect the desktop customization scene
   - Modernize with Avalonia + SkiaSharp + C#

6. **Ship the Experience First**
   - Make Cycloside amazing as an app before making it a compositor
   - Nail 5 core features before expanding
   - Quality over quantity

---

## How to Use These Documents

### For Development

When building specific features, reference:
- **Theater Mode?** → Read #02, #05, #06
- **Marketplace?** → Read #04
- **Plugin system?** → Read #03, #04
- **Theme engine?** → Read #02, #03
- **Input handling?** → Read #05

### For Design Decisions

When making architectural choices:
- **Philosophy?** → Read #03, #04
- **UX patterns?** → Read #05, #06
- **Community features?** → Read #02, #04
- **Platform strategy?** → Read #01, #02

### For Communication

When explaining Cycloside to others:
- **Elevator pitch?** → Read #03 manifesto
- **Technical audience?** → Read #01, #05
- **Business audience?** → Read #02, #04
- **Users/community?** → Read #03, #06

---

## Next Steps

### Immediate Actions

1. **Create Cycloside.TheaterMode project** (from #05, #06)
2. **Design federated marketplace spec** (from #04)
3. **Build 5 killer default themes** (from #02, #03)
4. **Implement unified input system** (from #05)
5. **Write full product manifesto** (synthesize #03, #04)

### Medium-Term

1. **WebTV source deep-dive** (extract patterns from #05)
2. **HyperStudio Revival plugin** (from #03)
3. **Kid Pix Desktop Mode** (from #03)
4. **Theater Mode MVP** (from #06)
5. **Community infrastructure** (forums, Discord, showcase)

### Long-Term

1. **Wayland compositor for Linux** (from #01)
2. **Complete WebTV-inspired appliance modes** (from #03, #05)
3. **Full creative suite** (HyperStudio cards, game maker, etc.)
4. **International community** (translations, localized feeds)
5. **Commercial support** (consulting, Pro features)

---

## Contributing to This Vision

These documents are living. As Cycloside evolves, so should these campfires and design docs.

### How to Contribute

1. **Run your own campfires** - explore new angles
2. **Add technical deep-dives** - extract patterns from source code
3. **Prototype ideas** - prove concepts work
4. **Document lessons learned** - what worked, what didn't
5. **Share with community** - get feedback, iterate

### Campfire Format

```markdown
# Title of Campfire

**Date:** [date]
**Participants:** [list of perspectives]
**Topic:** [what's being explored]

---

## The Discussion

[Conversation between perspectives]

---

## Key Takeaways

[Distillation of insights]
```

---

## Credits

**Campfire Facilitator:** Claude (Anthropic)  
**Vision Holder:** Justin (Cycloside creator)  
**Inspiration:** The desktop customization scene of the early 2000s, WebTV engineers, and everyone who believed computers should be personal.

**Special Thanks:**
- The WebTV team (Phil, Mick, Rubin, John, Joe, Chris, Dave, Andy, Cary, Bruce, Tim)
- Broderbund (HyperStudio, Kid Pix)
- The Winamp community
- DeviantArt customizers
- Everyone who kept the dream alive

---

## License

These design documents are part of the Cycloside project.  
Share freely. Build openly. Express yourself.

**Your desktop, your rules.**

---

*Last Updated: March 12, 2026*

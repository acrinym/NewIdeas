# Campfire 07: Cycloside as a Real Session

**Date:** March 13, 2026

**Context:** Building on Campfire 1 and Campfire 2, this session explores Cycloside as something larger than an app: a shell, session, scene engine, and community platform for personal expression software.

**Participants:**
- Linus Torvalds
- Steve Jobs
- Bill Gates
- Steve Perlman
- Michael Dell

---

## The Discussion

**Linus Torvalds:** Stop saying "build X11." That's not the move. X11 is a museum full of working machinery. Interesting, useful to study, absolutely not what you want to reinvent. What you want is a system that owns surfaces, input, composition, and policy for *Cycloside's* world. On Linux, that eventually means Wayland compositor territory. On Windows, that means shell replacement or shell-adjacent control. Different plumbing, same vision.

**Steve Jobs:** Right. The product is not "a protocol." The product is the feeling of entering Cycloside and realizing the computer finally has a soul again. The themes aren't paint. They are motion, sound, typography, mood, surfaces, ambient visuals, screensavers, launch behavior, workspace identity. If you build that deeply enough, people stop asking whether it's an app or a shell. It becomes *their environment*.

**Bill Gates:** And if it's going to be an environment, it needs an actual platform structure. Right now the instinct is good, but the architecture wants separation:
- scene/composition
- shell/session
- themes/skins/packs
- plugins/apps
- platform backends

If you don't split those cleanly, every cool feature turns into another special case.

**Steve Perlman:** The thing I like is that this isn't trying to become another sterile desktop. It's trying to bring back the sense that the interface is *alive*. WebTV mattered because the interface was the product. After Dark mattered because the machine had personality. Rainmeter and WindowBlinds mattered because people could make the machine feel like theirs. Cycloside is sitting right on top of that lineage.

**Michael Dell:** Fine, but if you want this to go beyond a private playground, you need to be real about what ships first. There's the dream architecture, and then there's the thing a person can actually install and use without hating you. If you try to do full cross-platform compositor, shell, marketplace, plugin runtime, and graphics engine all at once, you'll drown in your own ambition.

**Linus Torvalds:** Here's the clean model. Step one: Cycloside owns *its own* surfaces. Plugin windows, widgets, backdrops, docks, overlays, workspaces, transitions. No more effect hacks glued to random Avalonia windows. Build a real scene graph. Every element is a node. Effects operate on nodes. Themes feed materials and tokens into nodes. Now you're thinking like a compositor without needing to be one yet.

**Steve Jobs:** Yes. That's the crucial turn. Right now some of the effects still read like tricks. The future version reads like choreography. Open, close, minimize, switch workspace, enter theater mode, load retro mode, summon a gadget panel. Those are all scene transitions. Suddenly your synthwave theme isn't just magenta colors. It's a cinematic system.

**Bill Gates:** Then step two: package the experience. Themes become packs. Skins become shell chrome packs. Workspace packs become downloadable environment states. A "Cyberdeck" pack shouldn't just recolor the app. It should install:
- a theme
- a cursor set
- sounds
- preferred effects
- widget layout
- plugin bundle
- workspace profile

That is an actual platform artifact.

**Steve Perlman:** And then the community finally has something worth sharing. Not "here's my JSON file." More like "here's my whole world." That's where Cycloside gets sticky. People don't just install it. They trade setups, edition packs, whole moods.

**Michael Dell:** Good. But on Linux, what are we actually saying? Because "Wayland compositor" sounds sexy until you remember it means outputs, seats, xdg-shell, clipboard, screenshots, screen capture, XWayland, input methods, multi-monitor weirdness, GPU driver nonsense. That is not a side quest. That is a product line.

**Linus Torvalds:** Exactly. So don't lie to yourself. The Linux endgame is plausible, but it comes after the scene graph and shell model exist. First build `Cycloside Core`. Then `Cycloside Shell`. Then maybe `Cycloside Session` on Linux. If you jump straight to Wayland, you'll spend a year proving you can launch a terminal and move a window. Big whoop.

**Steve Jobs:** Which is why Windows is still useful, even if the vision is multiplatform. Windows is where you can make Cycloside's internal world gorgeous first. You refine the visual language, the surface model, the effect engine, the packs, the editor, the feeling. Then Linux becomes the place where Cycloside graduates from "beautiful shell" to "whole session."

**Bill Gates:** Compiz helps here, but as a vocabulary, not a code source. It teaches:
- lifecycle-based effects
- effect parameters
- workspace-as-place
- compositor thinking
- motion grammar

Wayland and X11 teach you protocol boundaries. After Dark teaches you delight. Rainmeter teaches community remixability. WindowBlinds teaches people want ownership of chrome. Cycloside can synthesize all of that into something new.

**Steve Perlman:** And do not underestimate how much "ambient computing" matters. Screensavers, idle visuals, background motion, subtle loops, attention cues, clock themes, audio-reactive desktops. Those aren't fluff. They're what make the machine feel inhabited. That's what people remember.

**Michael Dell:** So the directions are clearer than they looked at first.

One direction is `Cycloside Shell`:
- themes
- skins
- widgets
- packs
- plugins
- workspaces
- visual identity

Another is `Cycloside Studio`:
- code runner
- compilers
- plugin editor
- visual/audio tools
- maker integrations

Another is `Cycloside Session`:
- Linux login/session
- compositor-backed shell
- native surfaces
- maybe Theater Mode and appliance modes later

Those can be one architecture, not three separate products.

**Linus Torvalds:** X11 still has one legitimate role: prototyping control over external windows faster. If you want to learn window manager behavior, decorations, focus policy, reparenting, old-school desktop ownership, X11 is still a decent laboratory. But don't confuse the lab with the destination. The destination is Wayland if you want a modern Linux session.

**Steve Jobs:** The real destination is simpler: Cycloside should feel like a world you enter. The technical stack only matters insofar as it enables that feeling. If the user boots into Cycloside and thinks "holy shit, this is mine," you've won.

**Bill Gates:** Then write the architecture like you mean it:
- `Cycloside.Core`
- `Cycloside.Scene`
- `Cycloside.FX`
- `Cycloside.Packs`
- `Cycloside.Shell`
- `Cycloside.Studio`
- `Cycloside.Platform.Windows`
- `Cycloside.Platform.Linux.X11`
- `Cycloside.Platform.Linux.Wayland`

Now everyone knows where things belong.

**Steve Perlman:** And make the editor first-class. If the whole point is hacking the interface, then the interface should let you hack itself from inside itself. That is the magic trick.

**Michael Dell:** Shipping order matters more than fantasy:
1. stabilize native Cycloside surface model
2. recover and finish the effect system
3. turn themes/skins/workspaces into real packs
4. build in-app editors
5. only then start Linux session work

That path is hard, but it's sane.

---

## Key Takeaways

1. Do not build a new X11.
2. Use X11 as a learning or prototyping lab if useful, not the final architecture.
3. Treat Wayland compositor work as a later Linux session phase, not the starting point.
4. Build Cycloside's own scene graph first so themes, effects, skins, and surfaces stop being hacks.
5. Compiz is motion and compositor knowledge, not code to borrow.
6. The real product is a personal environment people can remix, share, and live inside.
7. Cycloside gets bigger by becoming a shell, runtime, and platform, not by becoming a random pile of desktop tricks.

## The Strongest Vision

**Cycloside is a personal expression shell with its own scene engine.**
- On Windows: a shell-layer environment
- On Linux: eventually a real session or compositor path
- Everywhere: themes as experiences, effects as first-class scene operations, plugins as native surfaces, community packs as the distribution model

That one actually holds together.

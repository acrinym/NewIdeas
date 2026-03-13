# Campfire 2: The Bigger Vision for Cycloside

**Date:** March 12, 2026

**Context:** Everyone's now familiar with Cycloside and the Linux/X11/Wayland context from Campfire 1. Exploring what could be done, why, improvements, and directions.

**Background:** Cycloside was always about community and hacking the interface. Built as a community-based WindowBlinds/Rainmeter type thing with themes and hacker vibes with a volatile code runner and some BOWEP games along with compilers for various languages that are built in.

**Participants:**
- Steve Jobs
- Bill Gates
- Steve Perlman (WebTV)
- Michael Dell

---

## The Discussion

**Steve Jobs:** Okay, so I've been looking at this Cycloside thing. It's trying to be a lot of things: WindowBlinds, Rainmeter, a plugin host, a security toolkit, a code editor, After Dark screensavers... it's all over the place. And you know what? I kind of love that. It's chaotic, but there's a core idea: **your desktop is a canvas, and you're the artist**.

**Bill Gates:** The question is whether it's a tool for power users or a platform for everyone. Because right now it reads like a developer's playground. Packet sniffing, MAC spoofing, MIDI routing, MQTT bridges—that's not "I want pretty themes," that's "I want to hack my environment." And that's fine! But you need to decide: is this Visual Studio meets Rainmeter, or is it the next-generation desktop shell?

**Steve Perlman:** I built WebTV to make the internet accessible from your couch with a remote. The insight there was: the interface *is* the experience. If Cycloside is really about "community and hacking the interface," then the goal should be: **anyone can remix their desktop**. Not just developers. Not just sysadmins. Regular people who think "wouldn't it be cool if..." and then just *do it*.

**Michael Dell:** From a business perspective, you're targeting a niche: tinkerers, retro computing enthusiasts, hackers, indie devs. That's fine—Rainmeter had a huge community. But if you want this to be more than a hobby project, you need a path to broader adoption. What's the killer feature that makes someone say "I need this" instead of "that's neat"?

**Steve Jobs:** The killer feature is **coherence**. Right now, Cycloside is a bag of parts. But imagine: you boot into Cycloside, and every window, every widget, every transition is *designed*. It's not just customizable—it's *beautiful* out of the box. You give people five or six incredible default themes: Cyberpunk 2077, Retro DOS, Minimal Glass, Studio Dark. Each one is a complete vibe, not just colors.

**Bill Gates:** And each theme is more than skin-deep. It's animations, sounds, wallpapers, cursor sets, window effects, and even the *behavior* of the desktop. Like, the Cyberpunk theme has glitchy transitions and neon trails. The Retro theme has CRT scanlines and boot sounds. The themes are **experiences**, not just style sheets.

**Steve Perlman:** Now you're talking. And here's where community comes in: you make theming so easy that people share theme packs like Winamp skins. There's a built-in theme browser, one-click installs, ratings, comments. But here's the kicker: themes can include *plugins*. A theme isn't just visual—it can enable widgets, effects, screensavers, sounds. It's a whole package.

**Michael Dell:** Okay, so the business model is: Cycloside is free. The community creates themes and plugins. You take a cut if people sell premium themes or pro plugins. Think Unity Asset Store, but for desktop environments. You seed it with 50 amazing free themes and a dozen power-user plugins, and then the community runs with it.

**Steve Jobs:** But you have to control quality. Not everything in the store, but what ships by default. Every default theme has to be *perfect*. Every included plugin has to be stable and useful. Because the first 30 seconds someone spends in Cycloside determines whether they stay or uninstall.

**Bill Gates:** What about the developer toolkit? Because you've got a code editor, a volatile script runner, compilers for multiple languages. That's basically a lightweight IDE baked into the desktop. Wouldn't it be cool if Cycloside had a "dev mode" where you could write plugins *inside Cycloside*? Like, you open the plugin editor, write some C#, hit "test," and it hot-reloads into your environment.

**Steve Perlman:** That's huge. You make Cycloside *self-modifying*. The desktop is also the dev environment for customizing the desktop. That closes the loop. You're not just *using* Cycloside, you're *programming* it, live, while it runs.

**Michael Dell:** And you ship tutorials. Like, the first time you open Cycloside, there's a welcome flow: "Let's build your first widget. Here's how to make a clock. Now make it show the weather. Now make it pulse when you get a notification." Five minutes later, they've written and deployed their first plugin. That's the hook.

**Steve Jobs:** The other thing is workspaces. You mentioned Compiz earlier—Compiz made virtual desktops *visual*. They weren't just tabs, they were places. Cycloside should do that, but better. Each workspace is a different context: Studio, Hack, Chill, Game. You switch workspaces and the whole vibe changes—theme, wallpaper, active plugins, window layout.

**Bill Gates:** And workspaces could be saved and shared. "Here's my cybersecurity workspace"—boom, you download it, and suddenly you've got a three-monitor layout with network tools on the left, terminals in the middle, and a packet sniffer on the right, all pre-configured. That's powerful.

**Steve Perlman:** What about making Cycloside the hub for retro computing stuff? You've already got After Dark vibes with screensavers. What if you leaned into that? Built-in emulators, retro game collections, BOWEP games, a whole "nostalgia" mode that feels like 90s desktop computing but modernized. That's a whole aesthetic community right there.

**Michael Dell:** And the security toolkit—packet sniffing, port scanning, exploit tools—that's a different community: pentesters, cybersecurity students, red teamers. If you market Cycloside as "the hacker's desktop," you've got a built-in audience. Make it the go-to environment for security research.

**Steve Jobs:** So here's the vision: Cycloside is three products in one.

**Cycloside Studio**: For creative tinkerers. Themes, widgets, effects, workspaces. The fun, aesthetic side.

**Cycloside Hacker**: For security and dev folks. Code editor, network tools, automation, scripting. The power-user side.

**Cycloside Retro**: For nostalgia and retro computing. Emulators, retro games, DOS vibes, 90s aesthetics. The community/fun side.

You pick your "edition" on install, or switch between them. Each one is Cycloside, but tailored.

**Bill Gates:** That actually makes sense. You're not trying to be everything to everyone—you're giving people an entry point based on their interests. And under the hood, it's all the same platform.

**Steve Perlman:** The community aspect is key. Cycloside needs forums, Discord, theme showcases, plugin jams. Make it social. "Check out my setup" becomes a whole thing. Screenshots, videos, tutorials. You're not just selling software, you're building a scene.

**Michael Dell:** And you start on one platform—probably Windows, since that's where most users are—and nail it there. Then expand to Linux as a Wayland compositor or window manager. macOS is a stretch goal. Don't spread too thin.

**Steve Jobs:** One more thing: the name. "Cycloside." It's... okay. But what does it *mean*? If you're building a desktop environment, the name should evoke the feeling. "Cyc" like cycle? Like cyclone? It's not immediately clear. Maybe lean into it: "Cycloside: the desktop that evolves with you." Or rebrand entirely. But the name matters.

**Bill Gates:** Honestly, I think the name is fine. It's unique, it's memorable, it sounds tech-y without being corporate. Don't overthink it.

**Steve Perlman:** What I'd love to see is Cycloside become the desktop for *makers*. Not just software developers—hardware hackers, artists, musicians, game devs. You've got MIDI, OSC, serial protocols, automation. Imagine a desktop where your physical devices just *talk* to your environment. Your MIDI controller changes your theme. Your Arduino triggers a desktop event. That's wild.

**Michael Dell:** The hardest part is focus. You've got so many features already—database management, AI assistants, digital forensics, API testing. That's awesome, but it's also overwhelming. Pick five core features, make them amazing, and put the rest in the "power user" tier or community plugins.

**Steve Jobs:** Here's my take: the core of Cycloside should be **expression**. Your desktop is yours. Not Microsoft's, not Apple's, not Google's. Yours. You make it look how you want, behave how you want, do what you want. That's the pitch. That's the soul of the product.

**Bill Gates:** And the technical foundation to support that is: a solid plugin system, a beautiful theming engine, rock-solid window effects, and a scene graph compositor that makes everything feel fluid. Get those four things right, and the rest is just features.

**Steve Perlman:** I'd use it. I'd love a desktop that felt like *mine* again, not like a rental from Big Tech.

**Michael Dell:** Alright, so what's the next step? Pick one platform, pick one killer demo—like a theme pack or a workspace—ship that, and see if the community shows up.

---

## Key Insights

### Core Identity
**Your desktop is your canvas** - personal expression over corporate constraints

### Three Editions Strategy
- **Studio** (themes/effects) - for creative tinkerers
- **Hacker** (tools/security) - for developers and security professionals
- **Retro** (nostalgia/games) - for retro enthusiasts

### Themes as Experiences
Not just colors, but animations, sounds, behaviors, plugins bundled together

### Self-Modifying Environment
Write and test plugins inside Cycloside, live hot-reload. The desktop is also the dev environment.

### Community is the Product
- Theme sharing
- Plugin marketplace
- Workspace packs
- Social showcases
- Forums and Discord
- Plugin jams

### Maker-Friendly Integration
MIDI, OSC, serial, automation make physical devices talk to desktop

### Quality Over Quantity
Nail 5 core features perfectly before expanding

### Workspaces as Contexts
Saved, shareable, complete environment reconfigurations

## What Cycloside Should Do Next

1. **Solidify the graphics stack** - Skia + SkiaSharp + proper scene graph + effect system
2. **Refine the plugin architecture** - make it stupid-easy to write and share plugins
3. **Build 5 killer default themes** - each one a complete, polished experience
4. **Create the in-app plugin/theme editor** - self-modification is the hook
5. **Launch community infrastructure** - marketplace, forums, showcase gallery
6. **Pick ONE platform to dominate first** - probably Windows shell replacement
7. **Market to three audiences**: aesthetic tinkerers, security hackers, retro enthusiasts

## The Big Architectural Idea

**Cycloside as a session/shell/compositor hybrid:**
- On Windows: shell replacement that owns desktop + chrome
- On Linux: Wayland compositor built on wlroots
- On macOS: really good window manager app (or skip it)
- Core: Skia-based scene graph where everything is a composited surface
- Plugin windows aren't OS windows—they're native Cycloside surfaces
- Effects aren't hacks—they're first-class scene operations
- Themes aren't skins—they're complete rendering pipelines

**This is buildable. This is shippable. This is different enough to matter.**

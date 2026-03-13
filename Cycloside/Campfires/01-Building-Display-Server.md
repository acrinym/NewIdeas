# Campfire 1: Building a Display Server for Cycloside

**Date:** March 12, 2026

**Topic:** How could we build or implement an X11/Xorg/Wayland-like system for Cycloside with custom window decorations, themes, and graphics?

**Participants:**
- Linus Torvalds
- Linus Tech Tips
- Steve Ballmer
- Steve Jobs

---

## The Discussion

**Linus Torvalds:** Okay, first thing: you don't want to build Wayland. You really don't. Wayland took *years* and it's still not fully stable everywhere. What you want is a **display protocol** that's *just complex enough* for your use case. X11 is overengineered for network transparency nobody uses anymore. Wayland is... well, it's better, but you're not replacing the Linux desktop.

**Steve Jobs:** He's right, but for the wrong reasons. The question isn't "can we build X11?" It's "what experience are we creating?" You already have window decorations, themes, effects in Cycloside. Don't throw that away to chase some Unix legacy. Think of it like this: Cycloside *is* the compositor. Every window, every plugin, every effect—they're all **your** surfaces. You own the whole scene.

**Linus Tech Tips:** So wait, are we talking about making Cycloside run *on top of* Windows/Linux, or making it *replace* the window manager? Because if it's the second one, that's a huge difference in scope. Like, on Windows, you'd be fighting DWM the whole way. On Linux, you'd basically be writing a Wayland compositor from scratch.

**Steve Ballmer:** DEVELOPERS! DEVELOPERS! DEVELOPERS! Sorry, reflex. But seriously—who's your target here? If it's Windows users who want cool themes and effects, you don't need to be a display server. If it's Linux tinkerers who want a whole desktop environment, that's a *completely* different product. You're cross-platform now, right? So pick one: do you want to be WindowBlinds 2026 edition, or do you want to be the next XFCE?

**Linus Torvalds:** Here's the thing: on Linux, if you wanted to do this right, you'd write a **Wayland compositor**. Not X11—X11 is dead, it just doesn't know it yet. A Wayland compositor is actually not that insane if you scope it right. You handle input, you composite surfaces, you do window management. wlroots gives you 80% of the infrastructure. But you'd have to commit: Cycloside becomes a *session*, not an app.

**Steve Jobs:** And that's beautiful, actually. Imagine: you boot into **Cycloside**. Not Windows with Cycloside running. Not GNOME with some plugins. You log in and you're *in Cycloside*. Your plugins aren't windows—they're native surfaces in the scene. Your themes aren't skins on top of something else—they're the actual rendering. That's a real product.

**Linus Tech Tips:** But how do you handle like, Chrome? Or Discord? Or any normal app? Do those run as Wayland clients inside your compositor? Because if yes, cool—that's how Wayland works. But then you need XWayland for legacy X11 apps, and now you're maintaining compatibility layers, and...

**Linus Torvalds:** XWayland isn't that bad. It's basically solved. The real question is whether you want to deal with all the bullshit Wayland protocols: xdg-shell, layer-shell, screen capture, clipboard, input methods... it's *a lot*. And every desktop environment implements them slightly differently, so apps break in fun new ways.

**Steve Ballmer:** Okay, but counterpoint: don't do any of that. Stay in the Windows/.NET world where you already are. Make Cycloside a *shell replacement*. Explorer.exe? Kill it. Cycloside is the shell. You get the taskbar, the desktop, the window chrome. You can do that on Windows without being a compositor. People have been doing it since LiteStep in the 90s.

**Steve Jobs:** Now that's interesting. A shell replacement is actually achievable. You're not fighting the OS, you're just taking over the top layer. On Windows, that's realistic. On Linux, you could be a window manager that sits on top of X11 or Wayland. On macOS... okay, macOS you're screwed, but two out of three.

**Linus Tech Tips:** So the architecture would be: Cycloside hooks into the OS at the "shell" level, it draws all the chrome and desktop stuff, but actual app windows are still managed by the OS underneath. You're just controlling the experience layer. That's honestly way more realistic than writing a compositor from scratch.

**Linus Torvalds:** For Windows, sure. For Linux, that's still basically writing a window manager or compositor, just with different goals. But you could do it. Start with something like i3 or sway's architecture—tiling window managers are simpler than floating ones—and then add your effects layer on top.

**Steve Jobs:** Here's the vibe I'm getting: Cycloside should be a *session*. On Linux, it's a Wayland compositor. On Windows, it's a shell replacement. On macOS, it's... okay, maybe just an app with really good window management. But the vision is: when you're in Cycloside, you're in **Cycloside**. Everything you see, touch, animate—that's all yours. The Compiz effects, the themes, the plugins—they're not bolted on, they're fundamental.

**Steve Ballmer:** And you make it so plugin developers can write their UI once and it runs in your scene graph. No Electron, no web views—just native Cycloside surfaces. That's your lock-in. That's your ecosystem. Developers building *for Cycloside*, not just *on* it.

**Linus Torvalds:** Technical reality check: if you do this on Linux, you need to pick wlroots or write your own Wayland server bits. wlroots is probably the right call—it's what sway uses, it's solid. You'd write a compositor in C# that binds to wlroots, handle your scene graph in SkiaSharp, pipe input events through. It's doable. Not easy, but doable.

**Linus Tech Tips:** What about just starting smaller? Like, make Cycloside work really, really well as a super-powered app first. Get all the effects perfect, get the plugin system perfect. *Then* figure out how to make it a full session. Because if you jump straight to "we're building a compositor," you're going to spend two years on infrastructure and never ship.

**Steve Jobs:** He's right. You ship the **experience** first. Make Cycloside so good that people say "I wish this was my whole desktop." Then you build that. Not the other way around.

**Linus Torvalds:** Agreed. Get the graphics stack right first. Skia + your effect system + proper scene graph compositing. Once that's solid, wrapping it in a Wayland compositor or shell replacement is just plumbing.

---

## Key Takeaways

1. **Don't build X11 from scratch** - it's legacy, overengineered, and a dead-end
2. **Wayland compositor is realistic but big** - wlroots can help, but it's a multi-year commitment
3. **Shell replacement on Windows is more achievable** - hook at the Explorer.exe level
4. **Ship the experience first, infrastructure later** - make Cycloside amazing as an app before making it a whole session
5. **The vision: Cycloside IS the compositor** - every surface, window, effect is native to Cycloside's scene graph

## Technical Paths Forward

### Windows
- Shell replacement (replace Explorer.exe)
- Hook at the shell level
- Control taskbar, desktop, window chrome
- Proven approach since LiteStep

### Linux
- Write a Wayland compositor
- Use wlroots for infrastructure
- Handle scene graph in SkiaSharp
- Requires commitment to protocols (xdg-shell, etc.)

### macOS
- Realistically: just a really good app
- Native window management is locked down

## The Architecture Vision

**Cycloside as a session/shell/compositor hybrid:**
- On Windows: shell replacement that owns desktop + chrome
- On Linux: Wayland compositor built on wlroots
- On macOS: really good window manager app (or skip it)
- Core: Skia-based scene graph where everything is a composited surface
- Plugin windows aren't OS windows—they're native Cycloside surfaces
- Effects aren't hacks—they're first-class scene operations
- Themes aren't skins—they're complete rendering pipelines

**This is buildable. This is shippable. This is different enough to matter.**

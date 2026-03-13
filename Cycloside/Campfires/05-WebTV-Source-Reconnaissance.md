# WebTV Source Code Reconnaissance Report

**Date:** March 12, 2026

**Source:** `D:\Downloads\Source\wtv-classic2lc2-src.tar\wtv-classic2lc2-src\oldWebTVSrc\`

**Purpose:** Extract design patterns, UI/UX insights, and architectural lessons for Cycloside Theater Mode

---

## Executive Summary

We have complete WebTV Classic 2 LC2 source code from 1995-1996. This is a **blueprint for 10-foot UI design** that solved the exact problem Cycloside Theater Mode needs to solve:

✅ Non-desktop computing paradigm  
✅ Remote control (gamepad) navigation  
✅ 10-foot UI design  
✅ Media-first experience  
✅ Accessible to non-technical users  
✅ Limited hardware (optimize performance)  

**The source code is a blueprint. Not to copy, but to learn from.**

---

## What We Found

### 1. Complete Codebase Structure

```
oldWebTVSrc/
├── Input/              ← Remote control input handling!
├── UserInterface/      ← The UI framework!
├── Graphics/           ← Rendering system!
├── View/               ← View layer architecture!
├── HTML/               ← Web rendering (custom browser)
├── Audio/ + Sound/     ← Media playback!
├── Documentation/      ← Design docs!
├── Box/                ← Hardware interface
├── Communications/     ← Network stack
├── Content/            ← Content delivery
├── Storage/            ← Persistent data
├── System/             ← OS layer
└── ROM/                ← Firmware
```

### 2. Developer Notes

**StateOfTheProject file:** 9 months of daily developer updates (Sept 1995 - May 1996)
- Build issues, solutions, workarounds
- Design decisions and rationale
- Team communication logs
- Technical debt tracking

**Key developers:**
- Phil, Mick, Rubin, John, Joe, Chris, Dave, Andy, Cary, Bruce, Tim

### 3. Code Language

- **C codebase** (not C++)
- Targeting **PowerPC and MIPS**
- Mac simulator for development
- Embedded target (set-top box)

---

## Key Insights for Cycloside

### 1. Input Handling Architecture

**From:** `Input/Input.c`

#### What WebTV Did

**Ring buffer input queue** (32 events):
```c
#define kInputQueueSize 32
static Input gInputQueue[kInputQueueSize];
static ulong gHeadIndex = 0, gTailIndex = 0;
```

**Device-agnostic input:**
- IR remote keyboard
- PC keyboard
- Both feed same queue

**Auto-repeat suppression:**
```c
// If the queue is not empty, and the current input is repeated, 
// don't add it. We do not want to queue up repeated keys.
if (InputQueueNotEmpty() && (input->modifiers & kAutoRepeat)) {
    Message(("PostInput:BLOCKED"));
    return;
}
```

**Global modifier tracking:**
- Unified Shift/Control/Alt/Caps state across all devices
- Hardware LED updates for Caps Lock

**Wake-up mechanism:**
```c
#ifdef FOR_MAC
PostEvent(nullEvent, 0);  // wake up out of WaitNextEvent
#endif
```

#### Apply to Cycloside

```csharp
// Cycloside.Input/UnifiedInputQueue.cs
public class UnifiedInputQueue {
    private const int QueueSize = 32;
    private CircularBuffer<InputEvent> _queue = new(QueueSize);
    private InputModifiers _globalMods;
    
    public void PostInput(InputEvent evt) {
        // Suppress auto-repeat if queue not empty
        if (evt.IsAutoRepeat && _queue.Any()) return;
        
        _queue.Enqueue(evt);
        WakeUI(); // trigger refresh
    }
    
    public bool TryGetNextInput(out InputEvent evt) {
        return _queue.TryDequeue(out evt);
    }
}
```

**Cycloside benefit:** Theater Mode can unify input from:
- Keyboard
- Gamepad (already have!)
- MIDI controllers (already have!)
- OSC/serial devices (already have!)

---

### 2. On-Screen Keyboard

**From:** `UserInterface/Keyboard.c`

#### What WebTV Did

**Slides up from bottom** with animation and sound:
```c
gScroll2Sound->Play();
gScreen->SlideAreaUp(&fBounds, kKeyboardScrollRate, 
                     RectangleHeight(fBounds) - fTargetViewOffset);
```

**Auto-positioning** - doesn't obscure the text field:
```c
// If keyboard will obscure entry field, need to scroll down PageViewer or move up panel.
fTargetViewOffset = fieldBounds.bottom + kFieldMargin - fBounds.top;

if (fTargetViewOffset > 0 ) {
    if (targetView == gPageViewer)
        gPageViewer->ScrollBy(fTargetViewOffset);
    else {
        // Move panel up so field stays visible
        Rectangle newViewBounds = viewBounds;
        OffsetRectangle(newViewBounds, 0, -fTargetViewOffset);
        targetView->SetBounds(&newViewBounds);
    }
}
```

**Caps lock visual indicator:**
```c
fCapsOn = ImageData::NewImageData("file://ROM/Images/CapsOn.gif");
fCapsOff = ImageData::NewImageData("file://ROM/Images/CapsOff.gif");
```

**Keyboard variants:**
```c
fKeyboard = kAlphabeticKeyboard;  // or numeric, symbols, etc.
```

#### Apply to Cycloside

```csharp
// Cycloside.Input/OnScreenKeyboard.cs
public class OnScreenKeyboard : FloatingPanel {
    private KeyboardMode _mode = KeyboardMode.Alphabetic;
    private bool _capsLock;
    
    public async Task ShowAsync(TextBox targetField) {
        // Slide up with animation and sound
        await SlideInAsync(
            from: Edge.Bottom, 
            duration: TimeSpan.FromMilliseconds(200),
            sound: "KeyboardSlide.wav"
        );
        
        // Adjust target field position if obscured
        if (WouldObscure(targetField)) {
            var offset = Height + Margin;
            await ScrollParentViewAsync(targetField, offset);
        }
    }
    
    public void SetMode(KeyboardMode mode) {
        _mode = mode;
        RebuildLayout();
    }
}
```

**Use cases in Cycloside:**
- Plugin/widget text input when using gamepad
- Touch screen support for tablets/Surface devices
- Accessibility alternative input

---

### 3. Remote Control Key Mappings

**From:** `StateOfTheProject` (line 399-415)

#### WebTV's Simulator Mappings

| Keyboard Key | Remote Button |
|--------------|---------------|
| Del          | Back |
| Insert       | Recent |
| Up/Down/Left/Right | D-pad |
| Enter        | Execute |
| End          | Options |
| Home         | Home |
| Page Up/Down | Scroll Up/Down |
| Pause        | Power |

#### Apply to Cycloside Theater Mode

| Gamepad Input | Cycloside Function |
|---------------|-------------------|
| B button      | Back/Close |
| View button   | Recent/History |
| D-pad/Left Stick | Navigate |
| A button      | Execute/Select |
| Menu button   | Options/Settings |
| Xbox button   | Home/Main menu |
| LB/RB bumpers | Scroll/Page |
| Start button  | Power menu/Exit |

**This makes Cycloside fully navigable from a couch with an Xbox controller!**

---

### 4. Development Philosophy

**From:** `StateOfTheProject` and `Documentation/CodeConventions`

#### Quotes from the Devs

> "If you write a Macintosh application without MacApp, you're working too hard." - Jim Reekes (Simulator author)

**Translation for Cycloside:** Use Avalonia properly. Don't fight the framework—leverage it.

#### Code Style

```
Indentation:
* K&R brace and indentation style
* Tab stops at 4
* Functions defined with return types on separate lines

Naming:
* Intercaps for everything
* 'k' prefix for constants
* 'g' prefix for global variables
* Get/Set/New prefixes for functions

Philosophy:
* Early return is encouraged, multiple levels of indentation is discouraged
* Goto is ok for error exiting and cleanup
* Code sharing is encouraged
* Object oriented style is encouraged
* Simplicity of style is encouraged
* Common style is encouraged
```

**Key philosophy:**
> "Code sharing is encouraged. Object oriented style is encouraged. **Simplicity of style is encouraged.**"

**This matches Cycloside's goals perfectly.**

#### Project Management

- **Daily "State of the Project" updates** - every change logged with context
- **Build scripts and automation** - MPW menus, AppleScript build automation
- **Memory profiling built-in** - "Memory Checkpoint," "Memory Difference" tools
- **Version control transparency** - issues documented publicly when things break

**Apply to Cycloside:**
- Keep CHANGELOG.md active and detailed ✅ (already doing!)
- Build automation scripts
- Memory/performance profiling tools in debug mode
- Public issue tracking with `bd` (beads) ✅ (already doing!)

---

### 5. UI Component Structure

**From:** `UserInterface/` directory

#### WebTV Components

```
UserInterface/
├── Button.c           (5KB)
├── TextField.c        (29KB)
├── RadioButton.c      (2KB)
├── Control.c          (14KB)  ← base control class
├── Panel.c            (5KB)   ← container UI element
├── Layer.c            (7KB)   ← layering/z-order
├── Menu.c             (28KB)  ← menu system
├── AlertWindow.c      (16KB)  ← modal dialogs
├── Screen.c           (26KB)  ← screen management
├── ContentView.c      (82KB!) ← main content rendering
└── Keyboard.c         (11KB)  ← on-screen keyboard
```

**This maps almost 1:1 to Avalonia's architecture!**

#### Cycloside Equivalent

| WebTV Component | Avalonia/Cycloside Equivalent |
|----------------|-------------------------------|
| Control.c | Avalonia.Controls.Control |
| Panel.c | Avalonia.Controls.Panel |
| Layer.c | Z-index / layer management |
| Button.c | Avalonia.Controls.Button |
| TextField.c | Avalonia.Controls.TextBox |
| Menu.c | Avalonia.Controls.Menu |
| AlertWindow.c | Window with modal overlay |
| Screen.c | Cycloside window effects / screen manager |
| ContentView.c | Plugin window rendering |

**The architecture is sound. We're on the right track.**

---

### 6. Documentation Insights

**From:** `Documentation/` directory

#### Available Design Docs

```
Documentation/
├── Product Spec          (63KB)
├── Extensions            (HTML extensions they added)
├── CodeConventions       (coding standards)
├── SimulatorDoc.txt      (24KB - how to use simulator)
├── Responsibilities      (team roles)
├── HighLevelTestScript   (QA procedures)
├── Bugs                  (bug tracking before databases)
├── QuoteBook             (team culture)
└── WebTVT HTML Support.html (27KB)
```

**Key learnings:**
- They documented **everything**
- Product spec was **living document**
- Extensions were **clearly specified**
- Team culture was **visible** (QuoteBook)

**Apply to Cycloside:**
- Write comprehensive design docs for Theater Mode
- Document all custom behaviors
- Maintain living product spec
- Preserve team culture and decision rationale

---

## Immediate Actionable Ideas for Cycloside

### 1. Theater Mode (10-foot UI)
**Inspired by:** WebTV's entire UI philosophy

- Large UI elements (readable from 10 feet away)
- Gamepad navigation using WebTV's proven input patterns
- Slide-in animations with sound effects
- Media-first layout (visualizers, games, widgets)
- Remote/gamepad as first-class input, not afterthought

### 2. On-Screen Keyboard Plugin
**Inspired by:** `UserInterface/Keyboard.c`

- For gamepad/touch input
- Slides up from bottom, doesn't obscure input field
- Multiple layouts (alpha, numeric, symbols)
- Visual feedback for caps lock
- Sound effects for key presses

### 3. Unified Input System
**Inspired by:** `Input/Input.c`

- Queue-based input handling (32-event ring buffer)
- Device-agnostic (keyboard/gamepad/MIDI/OSC all feed same queue)
- Suppresses auto-repeat spam
- Global modifier state tracking
- Wake-up mechanism for UI refresh

### 4. Development Tools
**Inspired by:** WebTV's dev practices

- Memory profiler (track allocations per plugin)
- Performance snapshot/diff tool
- Build automation scripts
- Daily changelog discipline ✅ (already doing!)

### 5. Sound Design
**Inspired by:** WebTV's UI sounds

- Every UI action has a sound (WebTV had "Scroll2Sound," startup jingles, etc.)
- Optional sound schemes (serious mode vs. fun mode)
- MIDI integration for UI sounds (use existing MIDI router!)
- Positional audio for spatial feedback

---

## Files Worth Deep-Diving Later

When building specific features, come back and read:

| File | For Building... | Priority |
|------|----------------|----------|
| `Input/Input.c` | Gamepad navigation, input queue | High |
| `UserInterface/Keyboard.c` | On-screen keyboard | High |
| `UserInterface/Menu.c` | Menu system for Theater Mode | Medium |
| `UserInterface/Screen.c` | Screen management, transitions | High |
| `UserInterface/ContentView.c` | Main rendering engine patterns | Medium |
| `Graphics/` | Rendering optimizations | Low |
| `Documentation/Product Spec` | Overall UX vision | High |
| `Documentation/Extensions` | How they extended HTML | Low |

---

## Technical Extraction Checklist

### Phase 1: Input System (1-2 weeks)
- [ ] Read `Input/Input.c` fully
- [ ] Extract ring buffer pattern
- [ ] Document modifier handling
- [ ] Prototype unified input queue in C#
- [ ] Test with keyboard + gamepad

### Phase 2: On-Screen Keyboard (2-3 weeks)
- [ ] Read `UserInterface/Keyboard.c` fully
- [ ] Extract slide-in animation pattern
- [ ] Document field-obscuring logic
- [ ] Design Cycloside keyboard layouts
- [ ] Build prototype keyboard control

### Phase 3: Navigation Patterns (1 week)
- [ ] Read `UserInterface/Menu.c`
- [ ] Extract focus management
- [ ] Document d-pad navigation flow
- [ ] Map to Avalonia focus system
- [ ] Test gamepad menu navigation

### Phase 4: Screen Management (2 weeks)
- [ ] Read `UserInterface/Screen.c`
- [ ] Extract transition animations
- [ ] Document layout management
- [ ] Integrate with Cycloside effects
- [ ] Build Theater Mode screen manager

---

## The Big Takeaway

**WebTV in 1995 solved the EXACT problem Cycloside Theater Mode needs to solve:**

✅ Non-desktop computing paradigm  
✅ Remote control (gamepad) navigation  
✅ 10-foot UI design  
✅ Media-first experience  
✅ Accessible to non-technical users  
✅ Limited hardware (optimize performance)  

**We have their source code. We have their design docs. We have their lessons learned.**

**The path forward is clear:**
1. Extract their input patterns → Cycloside unified input
2. Extract their keyboard design → Cycloside on-screen keyboard
3. Extract their navigation flow → Cycloside Theater Mode
4. Extract their UI philosophy → Cycloside 10-foot interface
5. Modernize with Avalonia + SkiaSharp + C#

**We're not copying WebTV. We're learning from masters and building the 2026 version.**

---

## Next Steps

**Immediate:**
1. Create `Cycloside.TheaterMode` project
2. Implement unified input queue
3. Design gamepad navigation system
4. Prototype on-screen keyboard

**Medium-term:**
5. Build Theater Mode UI framework
6. Create 5 Theater Mode themes
7. Integrate Phoenix Visualizer
8. Add BOWEP game launcher

**Long-term:**
9. Full WebTV-inspired appliance modes
10. Alternative computing paradigms
11. Community showcase of Theater Mode setups
12. Document lessons learned for next generation

**The foundation is here. Let's build.**

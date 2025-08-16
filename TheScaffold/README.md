# The Scaffold

This folder contains an experimental framework aimed at personal timeline editing and perception shaping. It's intentionally lightweight and for exploratory purposes.

## Perspective

The scaffold invites open‑ended experimentation. It isn't bound to what mainstream science accepts or rejects; subjective experience counts as valid input. Use these tools to explore narratives, intents, and modalities that resonate with you—even if they fall outside conventional evidence frameworks. The goal is self‑directed belief reinforcement, not external validation.

## Components

1. **TimelineEditor** – Plan and visualize events in an imagined timeline.
2. **AffirmationEngine** – Cycle through affirmations to gently nudge beliefs.
3. **FeedbackLoop** – Log reflections to track your progress.
4. **DataVault** – JSON store for events, affirmations, and reflections.
5. **CompressionReinforcer** – Practice decompression/compression drills.
6. **HolographicMapper** – Break a concept into facets for multi-angle insight.

## Usage Ideas

- Sketch an ideal timeline and review it daily.
- Rotate affirmations to reinforce the chosen narrative.
- Log reflections and adjust the timeline as your perspective evolves.
- Expand an idea in detail, then compress it into fewer words to anchor it.
- Map a concept across visual, emotional, and logical facets to rehearse holographic thinking.

## Compression/Decompression Reinforcement

The brain latches onto ideas when it has rebuilt them from several angles. A drill
with `CompressionReinforcer` runs like this:

1. **Decompress** – Blow up a seed concept into a vivid, wordy statement. This
   elaborative step creates extra neural links by tying the idea to sensations and
   context.
2. **Recompress** – Step down through shorter and shorter summaries (for example,
   25 → 10 → 3 words). Each pass forces active recall and selection of the core
   meaning, strengthening retrieval pathways.
3. **Revisit** – Later, glance at the tiny summaries. Because the mind practiced
   mapping them to the full expansion, those small phrases act like mental hashes
   that reconstruct the original idea.

Each phase is logged in the `DataVault` so you can review how your wording
changes over time. The loop of decompress → compress → revisit gives the concept
multiple exposures, gently nudging it deeper into your perception.

### Why It Works

- **Elaborative encoding** – The decompression phase forces you to flesh out the
  idea with context, which forms wider neural links.
- **Active recall** – Each compression pass makes you rebuild the meaning from
  memory, tightening those links.
- **Mental hashes** – The shortest summaries behave like hash keys that trigger
  the brain to rehydrate the full concept on demand.

### What You Can Do With It

- Track how your language tightens across drills to measure conceptual drift.
- Use the micro-summaries as flashcards for spaced repetition sessions.
- Probe the minimum word count needed to still spark full recall—a handy metric
  for mantras, design slogans, or other intent anchors.
- Pair with `FeedbackLoop` entries to note emotional shifts before and after a
  drill.

### Stored Output Example

Reinforcement phases land in `DataVault` under the `"reinforcements"` key. A
drill on *Abundance* might produce an entry such as:

```json
{
  "concept": "Abundance",
  "phase": "compress_5",
  "text": "opportunity flows to me"
}
```

Revisiting these snippets later prompts the mind to inflate them back into the
richer statement you originally authored.

## Holographic Mapping

`HolographicMapper` helps you clone the mental moves of a savant by forcing your
brain to hold several views of a concept at once. You sketch layers—perhaps a
visual metaphor, a logical summary, an emotional tone—and the tool stores each
layer in the `DataVault`.

1. **Select the concept** you want to master.
2. **Add layers** for different facets (e.g., `visual`, `analysis`, `emotion`).
3. **Synthesize** the layers to see the concept from all sides.
4. **Revisit** the saved layers later to refresh that multi‑angle snapshot.

Stored layers appear under the `"holograms"` key, giving you a growing matrix of
perspectives to re‑enter whenever you need that holographic spark.

## Quick Start

```python
from scaffold import (
    TimelineEditor,
    TimelineEvent,
    AffirmationEngine,
    DataVault,
    FeedbackLoop,
    CompressionReinforcer,
    HolographicMapper,
)

editor = TimelineEditor()
vault = DataVault()
loop = FeedbackLoop(vault)

event = TimelineEvent("2025-01-01", "Launch new venture", "Success")
editor.add_event(event)
vault.save_event(event)

engine = AffirmationEngine(["I adapt and grow."])
print(engine.next())

loop.reflect("Feeling optimistic!")
print(loop.history())

mapper = HolographicMapper("Resilience", vault)
mapper.add_layer("visual", "oak tree bending but not breaking")
mapper.add_layer("analysis", "system adapts under load")
print(mapper.synthesize())

reinforcer = CompressionReinforcer(vault)
print(reinforcer.drill("Abundance", "I trust opportunity flows to me.", [10, 5, 3]))
```

🛠️ Customize the Python scaffold (`scaffold.py`) to experiment with these ideas.


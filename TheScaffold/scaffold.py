"""
Timeline editing scaffold for personal perception work.
"""

from dataclasses import dataclass, asdict
from pathlib import Path
from typing import List, Dict, Optional
import json


@dataclass
class TimelineEvent:
    timestamp: str
    description: str
    outcome: str


class TimelineEditor:
    """🎬 Write the scenes of your ideal timeline."""
    def __init__(self):
        self.events: List[TimelineEvent] = []

    def add_event(self, event: TimelineEvent) -> None:
        """Store a new timeline event."""
        self.events.append(event)

    def visualize(self) -> str:
        """Return the current imagined timeline."""
        return "\n".join(
            f"{e.timestamp}: {e.description} -> {e.outcome}" for e in self.events
        )


class AffirmationEngine:
    """🔁 Cycle through affirmations to prime the mind."""
    def __init__(self, affirmations: List[str]):
        self.affirmations = affirmations

    def next(self) -> str:
        """Return the next affirmation in the list and rotate."""
        if not self.affirmations:
            return ""
        text = self.affirmations.pop(0)
        self.affirmations.append(text)
        return text


class DataVault:
    """📦 Simple JSON store for events, affirmations, reflections, and drills."""
    def __init__(self, path: str = "vault.json"):
        self.path = Path(path)
        if not self.path.exists():
            self._write(
                {
                    "events": [],
                    "affirmations": [],
                    "reflections": [],
                    "reinforcements": [],
                    "holograms": [],
                }
            )

    def _read(self) -> dict:
        with self.path.open("r", encoding="utf-8") as f:
            return json.load(f)

    def _write(self, data: dict) -> None:
        with self.path.open("w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)

    def save_event(self, event: TimelineEvent) -> None:
        data = self._read()
        data["events"].append(asdict(event))
        self._write(data)

    def save_affirmation(self, text: str) -> None:
        data = self._read()
        data["affirmations"].append(text)
        self._write(data)

    def save_reflection(self, text: str) -> None:
        data = self._read()
        data["reflections"].append(text)
        self._write(data)

    def save_reinforcement(self, entry: Dict[str, str]) -> None:
        """Store reinforcement drill data."""
        data = self._read()
        data["reinforcements"].append(entry)
        self._write(data)

    def save_hologram(self, entry: Dict[str, str]) -> None:
        """Persist a holographic thinking layer."""
        data = self._read()
        data["holograms"].append(entry)
        self._write(data)

    def load_events(self) -> List[TimelineEvent]:
        data = self._read()
        return [TimelineEvent(**e) for e in data.get("events", [])]

    def load_affirmations(self) -> List[str]:
        data = self._read()
        return data.get("affirmations", [])

    def load_reflections(self) -> List[str]:
        data = self._read()
        return data.get("reflections", [])

    def load_reinforcements(self) -> List[Dict[str, str]]:
        data = self._read()
        return data.get("reinforcements", [])

    def load_holograms(self, concept: Optional[str] = None) -> List[Dict[str, str]]:
        """Fetch stored holographic layers, optionally filtered by concept."""
        data = self._read()
        holograms = data.get("holograms", [])
        if concept is not None:
            holograms = [h for h in holograms if h.get("concept") == concept]
        return holograms


class FeedbackLoop:
    """📝 Record reflections to track progress."""
    def __init__(self, vault: DataVault):
        self.vault = vault

    def reflect(self, text: str) -> None:
        """Save a reflection entry."""
        self.vault.save_reflection(text)

    def history(self) -> List[str]:
        """Return recorded reflections."""
        return self.vault.load_reflections()


class HolographicMapper:
    """🌐 Weave a concept across facets to train holographic thinking.

    A concept rarely lives in one dimension. By mapping it through multiple
    lenses—visual, analytical, emotional—you build a mental "hologram" that the
    brain can spin and inspect from any angle. Each facet gets logged so you can
    revisit and expand your inner model over time.
    """

    def __init__(self, concept: str, vault: DataVault):
        self.concept = concept
        self.vault = vault
        self.layers: Dict[str, str] = {}

    def add_layer(self, facet: str, description: str) -> None:
        """Add a new perspective layer and persist it."""
        self.layers[facet] = description
        self.vault.save_hologram(
            {"concept": self.concept, "facet": facet, "description": description}
        )

    def synthesize(self) -> str:
        """Return a stitched summary of all facets."""
        return " | ".join(f"{k}: {v}" for k, v in self.layers.items())


class CompressionReinforcer:
    """🧠 Run decompression/compression drills to anchor ideas.

    The drill mirrors how memory solidifies: elaborative rehearsal followed by
    active recall. You first expand a concept into a rich statement (decompress),
    then compress it into progressively shorter summaries. Each compression pass
    forces the mind to rebuild the idea from memory, strengthening neural links.
    Entries are stored in the vault so tiny phrases later reignite the full story.

    The returned list contains the compressed summaries, with the smallest entry
    last, so you can test how few words still revive the full concept.
    """

    def __init__(self, vault: DataVault):
        self.vault = vault

    def drill(self, concept: str, expansion: str, word_counts: List[int]) -> List[str]:
        """Record a full decompression and successive recompressions.

        `word_counts` defines the compression staircase. For example, providing
        `[25, 10, 3]` logs 25-word, 10-word and 3-word versions. Smaller counts
        pressure-test how concise you can go while retaining meaning.

        Returns the list of summaries in the same order so you can review them or
        pipe them into spaced-repetition tools. Revisiting these snippets later
        prompts the brain to unpack the entire concept, turning brief cues into
        reinforced beliefs.
        """
        self.vault.save_reinforcement(
            {"concept": concept, "phase": "decompress", "text": expansion}
        )
        results = []
        words = expansion.split()
        for count in word_counts:
            # Shrink the statement; the mind fills the missing pieces, deepening recall
            summary = " ".join(words[:count])
            self.vault.save_reinforcement(
                {"concept": concept, "phase": f"compress_{count}", "text": summary}
            )
            results.append(summary)
        return results


# Example usage
if __name__ == "__main__":
    editor = TimelineEditor()
    event = TimelineEvent("2025-01-01", "Launch new venture", "Success and fulfillment")
    editor.add_event(event)
    vault = DataVault()
    vault.save_event(event)
    print(editor.visualize())

    engine = AffirmationEngine(["I adapt and grow.", "My goals are within reach."])
    print(engine.next())

    loop = FeedbackLoop(vault)
    loop.reflect("Feeling optimistic about the path ahead!")
    print(loop.history()[-1])

    mapper = HolographicMapper("Resilience", vault)
    mapper.add_layer("visual", "oak tree bending but not breaking")
    mapper.add_layer("emotion", "calm confidence during storms")
    print(mapper.synthesize())

    reinforcer = CompressionReinforcer(vault)
    summaries = reinforcer.drill(
        "Abundant mindset",
        "I trust that opportunities continually flow toward me in every season.",
        [12, 6, 3],
    )
    print(summaries[-1])


# QBasic Music Studio - FL Studio Inspired

QBasic Music Studio is an enhanced music creation application for QB64 Phoenix Edition, inspired by FL Studio’s pattern-based sequencer and piano roll.

Features include:
- **Pattern-Based Sequencer**: Create and switch between up to 16 patterns per track (key 'R' to add, 'A' to select).
- **Multi-Track Support**: Up to 4 tracks (e.g., melody, bass) with key 'K' to switch.
- **Graphical Piano Roll**: Click to place notes in a visual piano roll; right-click to edit.
- **Volume Automation**: Set per-note volume (0 to 1) for dynamic control.
- **Basic Delay Effect**: Adjustable delay (0 to 1) for playback.
- **WAV Export Placeholder**: Key 'W' to export (currently a stub).
- Mouse-driven interface for note entry and editing.
- Right-click to edit pitch, octave, duration, and volume.
- Press **N** to select note/rest for placement.
- Press **T** to change time signature.
- Press **M** to set tempo (60-240 BPM).
- Press **C** to clear the song.
- Press **U** to undo the last action.
- Notes grouped by measures in the UI.
- Save/load compositions to `.qms` files with error handling.
- Immediate note playback for preview.
- Reference sheet with octave/frequency chart.
- Experimental MIDI to QMS converter stub.

This is a starting point for a simplified DAW in QB64-PE. See `QMusicStudio.bas` for the main program.
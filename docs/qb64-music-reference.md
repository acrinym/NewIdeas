markdown

# QBasic Music Studio Reference (v2)

This document describes the QBasic Music Studio application (version 2) for QB64 Phoenix Edition, a simplified DAW inspired by FL Studio’s pattern-based sequencer and piano roll. It details the `.qms` file format, note input, controls, features, and frequency chart.

## QMS File Format

Songs are stored as binary `.qms` files with the following structure:

INT16 trackCount              // Number of tracks (1-4)
REPEAT trackCount TIMES
  INT16 patternCount          // Number of patterns in track (1-16)
  REPEAT patternCount TIMES
    INT16 noteCount           // Number of notes in pattern (0-1024)
    INT16 timeSigNumerator    // Beats per measure (e.g., 4 for 4/4)
    INT16 timeSigDenominator  // Beat value (e.g., 4 for quarter note)
    SINGLE tempo              // Tempo in BPM (60-240)
    REPEAT noteCount TIMES
      Note record:
        STRING * 3 pitch      // e.g., "C#4", "A3 ", "R  " for rest
        SINGLE duration       // Duration in beats (e.g., 1 for quarter note)
        SINGLE volume         // Volume (0 to 1)

## Note Input

- **Selecting Notes**: Press `N` to choose the note or rest to place with left-clicks (e.g., `C#4`, `R`). Specify pitch, octave (1-7), duration (beats), and volume (0-1).
- **Piano Roll**: Click in the graphical piano roll (bottom of screen) to place notes at the selected octave. Right-click to edit existing notes.
- **Note List**: Left-click above the piano roll to add the selected note to the note list. Right-click a listed note to edit its pitch, octave, duration, or volume.
- **Duration**: Enter duration as a number of beats (e.g., 1 for quarter note, 0.5 for eighth note). Future versions may support symbols (W=whole, H=half, Q=quarter, E=eighth).

## Controls

- **S**: Save song to a `.qms` file.
- **L**: Load song from a `.qms` file.
- **P**: Play all tracks and patterns in sequence.
- **N**: Choose note or rest for placement (pitch, octave, duration, volume).
- **T**: Set time signature (beats per measure and beat value, e.g., 4/4).
- **M**: Set tempo (60-240 BPM).
- **C**: Clear all tracks and patterns in the song.
- **U**: Undo the last note addition, edit, clear, or time signature/tempo change.
- **W**: Export to WAV (placeholder, outputs a text file).
- **R**: Add a new pattern to the current track (up to 16 patterns).
- **K**: Select track (1-4, e.g., melody, bass).
- **A**: Select pattern within the current track (1 to pattern count).
- **Mouse Left-Click**: Place a note in the piano roll (based on Y-position) or note list (using selected note).
- **Mouse Right-Click**: Edit a note in the piano roll or note list.

## Features

- **Pattern-Based Sequencer**: Create up to 16 patterns per track for modular composition (e.g., verse, chorus). Add patterns with `R`, switch with `A`.
- **Multi-Track Support**: Up to 4 tracks for different instruments or parts (e.g., melody, bass). Select with `K`.
- **Graphical Piano Roll**: Visual note placement at the bottom of the screen (y=200-400). Notes display as blue bars, with length proportional to duration.
- **Volume Automation**: Set per-note volume (0 to 1) during placement or editing for dynamic control.
- **Basic Delay Effect**: Adjustable delay (0 to 1) adds an echo to notes during playback.
- **WAV Export Placeholder**: Outputs a text file as a stub for future audio export (requires external libraries).
- **Undo Functionality**: Revert the last action (note addition, edit, clear, etc.) with `U`.
- **Measure Display**: Notes are grouped by measures in the UI, based on the time signature.
- **Note Preview**: Hear notes immediately after placement or editing.
- **Save/Load**: Store songs in `.qms` files with error handling for file issues.

## Frequencies

Approximate frequencies for notes in octaves 3-5, used by the `PLAY` command:
- **Octave 3**:
  - C = 130.81 Hz
  - D = 146.83 Hz
  - E = 164.81 Hz
  - F = 174.61 Hz
  - G = 196.00 Hz
  - A = 220.00 Hz
  - B = 246.94 Hz
- **Octave 4**:
  - C = 261.63 Hz
  - D = 293.66 Hz
  - E = 329.63 Hz
  - F = 349.23 Hz
  - G = 392.00 Hz
  - A = 440.00 Hz
  - B = 493.88 Hz
- **Octave 5**:
  - C = 523.25 Hz
  - D = 587.33 Hz
  - E = 659.26 Hz
  - F = 698.46 Hz
  - G = 783.99 Hz
  - A = 880.00 Hz
  - B = 987.77 Hz

## Example PLAY Sequence

- `PLAY "T120 O4 CDEFGAB > C"`
- Plays a C major scale in octave 4 at 120 BPM.

## MIDI to QMS Converter

- A future feature will include a `MidiToQms.bas` stub to convert MIDI files to QMS format, mapping MIDI note events to `PLAY` notation (e.g., MIDI note 60 to `C4`).
- Not implemented in this version due to QB64-PE’s limited MIDI support.

## Notes

- This application is a starting point for a simplified DAW in QB64-PE, inspired by FL Studio.
- The piano roll is basic due to QB64-PE’s graphics limitations but supports visual note placement.
- WAV export is a placeholder; actual audio export requires external libraries or future QB64-PE enhancements.
- Future versions may add duration symbols (W=whole, H=half, Q=quarter, E=eighth) and MIDI import.
- See `QMusicStudio.bas` for the main program.


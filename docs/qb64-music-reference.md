# QBasic Music Studio Reference

This document accompanies the `QBasicMusicStudio` example. It summarises
note usage and the simple `.qms` file structure.

## QMS File Format

Each song is stored as a binary file using the following structure:

```
INT16 noteCount
REPEAT noteCount TIMES
  Note record:
    2 bytes pitch string (e.g. "C4")
    4 bytes single-precision duration
```

## Note Input

Click the left mouse button in the application window to add a
`C4` quarter note. Right-click on a listed note to choose a new
pitch, octave and duration. Press **S** to save, **L** to load, and
**P** to play.

## Frequencies

See `QBasicMusicStudio/ReferenceSheet.txt` for a basic octave chart.

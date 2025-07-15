# QBasic Music Studio Reference

This document accompanies the `QBasicMusicStudio` example. It summarises
note usage and the simple `.qms` file structure.

## QMS File Format

Each song is stored as a binary file using the following structure:

```
INT16 timeSigNumerator
INT16 timeSigDenominator
INT16 noteCount
REPEAT noteCount TIMES
  Note record:
    3 bytes pitch string (e.g. "C#4" or "R  ")
    4 bytes single-precision duration
```

## Note Input

Press **N** in the app to choose which note (or rest) will be placed by
left clicking. Right-click on an existing note to edit it. Use **T** to
change the time signature. Press **S** to save, **L** to load, and
**P** to play the song.

## Frequencies

See `QBasicMusicStudio/ReferenceSheet.txt` for a basic octave chart.

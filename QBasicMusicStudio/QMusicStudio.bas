' QBasic Music Studio - FL Studio Inspired
' Enhanced music tracker for QB64 Phoenix Edition

OPTION BASE 1

TYPE Note
    pitch AS STRING * 3   ' e.g. "C#4" or "R  "
    duration AS SINGLE    ' note length in beats
    volume AS SINGLE     ' volume (0 to 1) for automation
END TYPE

TYPE Pattern
    notes(1 TO 1024) AS Note
    count AS INTEGER
END TYPE

TYPE Track
    patterns(1 TO 16) AS Pattern
    patternCount AS INTEGER
END TYPE

TYPE Song
    tracks(1 TO 4) AS Track    ' Up to 4 tracks (e.g., melody, bass, drums, chords)
    trackCount AS INTEGER
    tempo AS SINGLE
    timeSigN AS INTEGER
    timeSigD AS INTEGER
END TYPE

DIM SHARED currentSong AS Song
DIM SHARED undoSong AS Song
DIM SHARED mouseX AS INTEGER, mouseY AS INTEGER
DIM SHARED selectedPitch AS STRING * 3
DIM SHARED selectedOctave AS INTEGER
DIM SHARED selectedDuration AS SINGLE
DIM SHARED selectedVolume AS SINGLE
DIM SHARED currentTrack AS INTEGER
DIM SHARED currentPattern AS INTEGER
DIM SHARED canUndo AS INTEGER
DIM SHARED delayAmount AS SINGLE   ' Delay effect strength (0 to 1)
CONST START_Y = 6
CONST PIANO_ROLL_Y = 200
CONST PIANO_ROLL_HEIGHT = 200

DECLARE SUB Init()
DECLARE SUB MainLoop()
DECLARE SUB Render()
DECLARE SUB HandleMouse()
DECLARE SUB EditNote(t AS INTEGER, p AS INTEGER, i AS INTEGER)
DECLARE SUB PromptNote(n$(), note$, octave AS INTEGER, dur AS SINGLE, vol AS SINGLE)
DECLARE SUB ChoosePlacement()
DECLARE SUB ChangeTimeSignature()
DECLARE SUB ChangeTempo()
DECLARE SUB SaveSong(filename$)
DECLARE SUB LoadSong(filename$)
DECLARE SUB PlaySong()
DECLARE SUB PlayNote(pitch$, duration AS SINGLE, volume AS SINGLE)
DECLARE SUB SaveUndo()
DECLARE SUB Undo()
DECLARE SUB ClearSong()
DECLARE SUB ExportWAV(filename$)
DECLARE SUB AddPattern()
DECLARE SUB SelectTrack()
DECLARE SUB SelectPattern()
DECLARE SUB DrawPianoRoll()

CALL Init
CALL MainLoop

SUB Init
    SCREEN _NEWIMAGE(800, 600, 32)
    _TITLE "QBasic Music Studio - FL Studio Inspired"
    currentSong.trackCount = 1
    currentSong.tracks(1).patternCount = 1
    currentSong.tracks(1).patterns(1).count = 0
    currentSong.tempo = 120
    currentSong.timeSigN = 4
    currentSong.timeSigD = 4
    selectedPitch = "C  "
    selectedOctave = 4
    selectedDuration = 1
    selectedVolume = 1
    currentTrack = 1
    currentPattern = 1
    canUndo = 0
    delayAmount = 0.3
END SUB

SUB MainLoop
    DO
        CALL HandleMouse
        CALL Render
        _LIMIT 60
    LOOP UNTIL INKEY$ = CHR$(27)
END SUB

SUB Render
    CLS
    PRINT "QBasic Music Studio - S:Save, L:Load, P:Play, N:Note, T:TimeSig, M:Tempo, C:Clear, U:Undo, W:Export WAV, R:New Pattern, K:Track, A:Pattern"
    PRINT "Track: "; currentTrack; "  Pattern: "; currentPattern; "  Notes: "; currentSong.tracks(currentTrack).patterns(currentPattern).count;
    PRINT "  TimeSig: "; currentSong.timeSigN; "/"; currentSong.timeSigD; "  Tempo: "; currentSong.tempo; " BPM  Delay: "; delayAmount
    IF canUndo THEN PRINT "  [Undo Available]";
    PRINT
    PRINT "Place: "; selectedPitch;
    IF LEFT$(selectedPitch, 1) <> "R" THEN PRINT selectedOctave;
    PRINT " - "; selectedDuration; " beat(s)  Volume: "; selectedVolume
    beats = 0
    measure = 1
    FOR i = 1 TO currentSong.tracks(currentTrack).patterns(currentPattern).count
        LOCATE START_Y + i - 1, 1
        IF beats = 0 THEN PRINT "Measure "; measure;
        PRINT USING "##"; i; ": "; currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pitch; " - ";
        PRINT currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration; " beats  Vol: "; currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).volume
        beats = beats + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration
        IF beats >= currentSong.timeSigN / currentSong.timeSigD THEN
            beats = 0
            measure = measure + 1
        END IF
    NEXT
    CALL DrawPianoRoll
END SUB

SUB DrawPianoRoll
    LINE (0, PIANO_ROLL_Y)-(800, PIANO_ROLL_Y + PIANO_ROLL_HEIGHT), _RGB(255, 255, 255), BF
    FOR i = 1 TO 12
        y = PIANO_ROLL_Y + (i - 1) * (PIANO_ROLL_HEIGHT / 12)
        LINE (0, y)-(800, y), _RGB(0, 0, 0)
        LOCATE (y / 16) + 1, 1
        PRINT i; ": "; MID$("C C#D D#E F F#G G#A A#B ", (i - 1) * 3 + 1, 3)
    NEXT
    x = 0
    FOR i = 1 TO currentSong.tracks(currentTrack).patterns(currentPattern).count
        note$ = currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pitch
        IF LEFT$(note$, 1) <> "R" THEN
            noteIdx = INSTR("C C#D D#E F F#G G#A A#B ", LEFT$(note$, 2)) / 3 + 1
            y = PIANO_ROLL_Y + (12 - noteIdx) * (PIANO_ROLL_HEIGHT / 12)
            LINE (x, y)-(x + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20, y), _RGB(0, 128, 255), BF
        END IF
        x = x + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20
    NEXT
END SUB

SUB HandleMouse
    DIM btn AS INTEGER
    _MOUSEINPUT btn, mouseX, mouseY
    IF _KEYDOWN(ASC("S")) THEN
        INPUT "Save file (.qms): ", f$
        CALL SaveSong(f$)
    ELSEIF _KEYDOWN(ASC("L")) THEN
        INPUT "Load file (.qms): ", f$
        CALL LoadSong(f$)
    ELSEIF _KEYDOWN(ASC("P")) THEN
        CALL PlaySong
    ELSEIF _KEYDOWN(ASC("N")) THEN
        CALL ChoosePlacement
    ELSEIF _KEYDOWN(ASC("T")) THEN
        CALL ChangeTimeSignature
    ELSEIF _KEYDOWN(ASC("M")) THEN
        CALL ChangeTempo
    ELSEIF _KEYDOWN(ASC("C")) THEN
        CALL ClearSong
    ELSEIF _KEYDOWN(ASC("U")) THEN
        CALL Undo
    ELSEIF _KEYDOWN(ASC("W")) THEN
        INPUT "Export WAV file: ", f$
        CALL ExportWAV(f$)
    ELSEIF _KEYDOWN(ASC("R")) THEN
        CALL AddPattern
    ELSEIF _KEYDOWN(ASC("K")) THEN
        CALL SelectTrack
    ELSEIF _KEYDOWN(ASC("A")) THEN
        CALL SelectPattern
    ELSEIF btn > 0 THEN
        IF mouseY >= PIANO_ROLL_Y AND mouseY <= PIANO_ROLL_Y + PIANO_ROLL_HEIGHT THEN
            noteIdx = 12 - INT((mouseY - PIANO_ROLL_Y) / (PIANO_ROLL_HEIGHT / 12))
            IF btn AND 1 THEN
                IF currentSong.tracks(currentTrack).patterns(currentPattern).count < 1024 THEN
                    CALL SaveUndo
                    currentSong.tracks(currentTrack).patterns(currentPattern).count = currentSong.tracks(currentTrack).patterns(currentPattern).count + 1
                    note$ = MID$("C C#D D#E F F#G G#A A#B ", (noteIdx - 1) * 3 + 1, 2) + LTRIM$(STR$(selectedOctave))
                    currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch = LEFT$(note$ + "   ", 3)
                    currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).duration = selectedDuration
                    currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).volume = selectedVolume
                    CALL PlayNote(currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch, selectedDuration, selectedVolume)
                END IF
            ELSEIF btn AND 2 THEN
                x = 0
                FOR i = 1 TO currentSong.tracks(currentTrack).patterns(currentPattern).count
                    xEnd = x + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20
                    IF mouseX >= x AND mouseX <= xEnd THEN
                        CALL SaveUndo
                        CALL EditNote(currentTrack, currentPattern, i)
                        EXIT FOR
                    END IF
                    x = x + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20
                NEXT
            END IF
        ELSEIF btn AND 1 THEN
            IF currentSong.tracks(currentTrack).patterns(currentPattern).count < 1024 THEN
                CALL SaveUndo
                currentSong.tracks(currentTrack).patterns(currentPattern).count = currentSong.tracks(currentTrack).patterns(currentPattern).count + 1
                IF LEFT$(selectedPitch, 1) = "R" THEN
                    currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch = "R  "
                ELSE
                    pitch$ = LEFT$(selectedPitch, 2) + LTRIM$(STR$(selectedOctave))
                    currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch = LEFT$(pitch$ + "   ", 3)
                END IF
                currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).duration = selectedDuration
                currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).volume = selectedVolume
                CALL PlayNote(currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch, selectedDuration, selectedVolume)
            END IF
        ELSEIF btn AND 2 THEN
            row = INT(mouseY / 16) + 1
            index = row - START_Y + 1
            IF index >= 1 AND index <= currentSong.tracks(currentTrack).patterns(currentPattern).count THEN
                CALL SaveUndo
                CALL EditNote(currentTrack, currentPattern, index)
            END IF
        END IF
    END IF
END SUB

SUB SaveSong(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".QMS" THEN filename$ = filename$ + ".qms"
    OPEN filename$ FOR BINARY AS #1
    IF LOF(1) = 0 THEN
        PUT #1, , currentSong
        CLOSE #1
    ELSE
        CLOSE #1
        PRINT "Error: File already exists or inaccessible."
        SLEEP 2
    END IF
END SUB

SUB LoadSong(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".QMS" THEN filename$ = filename$ + ".qms"
    IF _FILEEXISTS(filename$) THEN
        CALL SaveUndo
        OPEN filename$ FOR BINARY AS #1
        IF LOF(1) > 0 THEN
            GET #1, , currentSong
            CLOSE #1
        ELSE
            CLOSE #1
            PRINT "Error: File is empty or corrupted."
            SLEEP 2
        END IF
    ELSE
        PRINT "Error: File not found."
        SLEEP 2
    END IF
END SUB

SUB PlaySong
    FOR t = 1 TO currentSong.trackCount
        FOR p = 1 TO currentSong.tracks(t).patternCount
            FOR i = 1 TO currentSong.tracks(t).patterns(p).count
                CALL PlayNote(currentSong.tracks(t).patterns(p).notes(i).pitch, currentSong.tracks(t).patterns(p).notes(i).duration, currentSong.tracks(t).patterns(p).notes(i).volume)
            NEXT
        NEXT
    NEXT
END SUB

SUB PlayNote(pitch$, duration AS SINGLE, volume AS SINGLE)
    IF LEFT$(pitch$, 1) = "R" THEN
        _DELAY duration * (60 / currentSong.tempo)
    ELSE
        _SNDVOL _SNDOPEN(pitch$), volume
        _SNDPLAY pitch$
        _DELAY duration * (60 / currentSong.tempo)
        IF delayAmount > 0 THEN
            _DELAY (duration * 0.5) * (60 / currentSong.tempo)
            _SNDVOL _SNDOPEN(pitch$), volume * delayAmount
            _SNDPLAY pitch$
        END IF
        _SNDCLOSE _SNDOPEN(pitch$)
    END IF
END SUB

SUB EditNote(t AS INTEGER, p AS INTEGER, i AS INTEGER)
    DIM notes$(13)
    notes$(1) = "C": notes$(2) = "C#": notes$(3) = "D": notes$(4) = "D#"
    notes$(5) = "E": notes$(6) = "F": notes$(7) = "F#"
    notes$(8) = "G": notes$(9) = "G#": notes$(10) = "A": notes$(11) = "A#": notes$(12) = "B": notes$(13) = "R"
    DIM pitch$, oct%, dur!, vol!
    CLS
    PRINT "Edit Note"; i; " in Track"; t; " Pattern"; p
    CALL PromptNote(notes$(), pitch$, oct%, dur!, vol!)
    CALL SaveUndo
    IF LEFT$(pitch$, 1) = "R" THEN
        currentSong.tracks(t).patterns(p).notes(i).pitch = "R  "
    ELSE
        currentSong.tracks(t).patterns(p).notes(i).pitch = LEFT$(pitch$ + "   ", 3)
    END IF
    currentSong.tracks(t).patterns(p).notes(i).duration = dur!
    currentSong.tracks(t).patterns(p).notes(i).volume = vol!
    CALL PlayNote(currentSong.tracks(t).patterns(p).notes(i).pitch, dur!, vol!)
END SUB

SUB PromptNote(n$(), note$, octave AS INTEGER, dur AS SINGLE, vol AS SINGLE)
    FOR j = 1 TO 13
        PRINT USING "##"; j; ": "; n$(j)
    NEXT
    INPUT "Choose note number: ", idx%
    IF idx% < 1 OR idx% > 13 THEN idx% = 1
    IF n$(idx%) <> "R" THEN
        INPUT "Octave (1-7): ", octave%
        IF octave% < 1 OR octave% > 7 THEN octave% = 4
        note$ = n$(idx%) + LTRIM$(STR$(octave%))
    ELSE
        note$ = "R"
        octave% = 0
    END IF
    INPUT "Duration (beats): ", dur
    IF dur <= 0 THEN dur = 1
    INPUT "Volume (0 to 1): ", vol
    IF vol < 0 OR vol > 1 THEN vol = 1
END SUB

SUB ChoosePlacement
    DIM notes$(13)
    notes$(1) = "C": notes$(2) = "C#": notes$(3) = "D": notes$(4) = "D#"
    notes$(5) = "E": notes$(6) = "F": notes$(7) = "F#"
    notes$(8) = "G": notes$(9) = "G#": notes$(10) = "A": notes$(11) = "A#": notes$(12) = "B": notes$(13) = "R"
    CLS
    PRINT "Select note to place"
    CALL PromptNote(notes$(), selectedPitch, selectedOctave, selectedDuration, selectedVolume)
    IF LEFT$(selectedPitch, 1) = "R" THEN selectedOctave = 0
END SUB

SUB ChangeTimeSignature
    CALL SaveUndo
    INPUT "Beats per measure: ", currentSong.timeSigN
    INPUT "Beat value (e.g. 4 for quarter): ", currentSong.timeSigD
    IF currentSong.timeSigN <= 0 THEN currentSong.timeSigN = 4
    IF currentSong.timeSigD <= 0 THEN currentSong.timeSigD = 4
END SUB

SUB ChangeTempo
    CALL SaveUndo
    INPUT "Tempo (BPM, 60-240): ", tempo!
    IF tempo! < 60 OR tempo! > 240 THEN tempo! = 120
    currentSong.tempo = tempo!
END SUB

SUB SaveUndo
    undoSong = currentSong
    canUndo = -1
END SUB

SUB Undo
    IF canUndo THEN
        currentSong = undoSong
        canUndo = 0
    END IF
END SUB

SUB ClearSong
    CALL SaveUndo
    FOR t = 1 TO currentSong.trackCount
        FOR p = 1 TO currentSong.tracks(t).patternCount
            currentSong.tracks(t).patterns(p).count = 0
        NEXT
    NEXT
END SUB

SUB ExportWAV(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".WAV" THEN filename$ = filename$ + ".wav"
    OPEN filename$ FOR OUTPUT AS #1
    PRINT #1, "WAV export not fully implemented in QB64-PE. Placeholder for future audio export."
    CLOSE #1
    PRINT "WAV export placeholder created. Actual WAV export requires external libraries."
    SLEEP 2
END SUB

SUB AddPattern
    IF currentSong.tracks(currentTrack).patternCount < 16 THEN
        CALL SaveUndo
        currentSong.tracks(currentTrack).patternCount = currentSong.tracks(currentTrack).patternCount + 1
        currentSong.tracks(currentTrack).patterns(currentSong.tracks(currentTrack).patternCount).count = 0
        currentPattern = currentSong.tracks(currentTrack).patternCount
        PRINT "New pattern"; currentPattern; "added to Track"; currentTrack
        SLEEP 1
    ELSE
        PRINT "Maximum patterns (16) reached for Track"; currentTrack
        SLEEP 2
    END IF
END SUB

SUB SelectTrack
    INPUT "Select track (1-4): ", t%
    IF t% >= 1 AND t% <= 4 THEN
        IF currentSong.tracks(t%).patternCount = 0 THEN
            currentSong.tracks(t%).patternCount = 1
            currentSong.tracks(t%).patterns(1).count = 0
            IF t% > currentSong.trackCount THEN currentSong.trackCount = t%
        END IF
        currentTrack = t%
    END IF
END SUB

SUB SelectPattern
    INPUT "Select pattern (1-"; currentSong.tracks(currentTrack).patternCount; "): ", p%
    IF p% >= 1 AND p% <= currentSong.tracks(currentTrack).patternCount THEN
        currentPattern = p%
    END IF
END SUB
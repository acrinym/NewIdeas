' QBasic Music Studio
' Minimal music tracker example for QB64 Phoenix Edition

OPTION BASE 1

TYPE Note
    pitch AS STRING * 3   ' e.g. "C#4" or "R  " for rest
    duration AS SINGLE    ' note length in beats
END TYPE

TYPE Song
    notes(1 TO 1024) AS Note
    count AS INTEGER
END TYPE

DIM SHARED currentSong AS Song
DIM SHARED mouseX AS INTEGER, mouseY AS INTEGER
DIM SHARED selectedPitch AS STRING * 3
DIM SHARED selectedOctave AS INTEGER
DIM SHARED selectedDuration AS SINGLE
DIM SHARED timeSigN AS INTEGER, timeSigD AS INTEGER
CONST START_Y = 4

DECLARE SUB Init()
DECLARE SUB MainLoop()
DECLARE SUB Render()
DECLARE SUB HandleMouse()
DECLARE SUB EditNote(i AS INTEGER)
DECLARE SUB PromptNote(n$(), note$, octave AS INTEGER, dur AS SINGLE)
DECLARE SUB ChoosePlacement()
DECLARE SUB ChangeTimeSignature()
DECLARE SUB SaveSong(filename$)
DECLARE SUB LoadSong(filename$)
DECLARE SUB PlaySong()

CALL Init
CALL MainLoop

SUB Init
    SCREEN _NEWIMAGE(640, 480, 32)
    _TITLE "QBasic Music Studio"
    currentSong.count = 0
    selectedPitch = "C"
    selectedOctave = 4
    selectedDuration = 1
    timeSigN = 4: timeSigD = 4
END SUB

SUB MainLoop
    DO
        CALL HandleMouse
        CALL Render
        _LIMIT 60
    LOOP UNTIL INKEY$ = CHR$(27) ' Esc to quit
END SUB

SUB Render
    CLS
    PRINT "QBasic Music Studio - Press S to save, L to load, P to play"
    PRINT "Notes: "; currentSong.count; "  TimeSig: "; timeSigN; "/"; timeSigD
    PRINT "Place: "; selectedPitch;
    IF selectedPitch <> "R" THEN PRINT selectedOctave;
    PRINT " - "; selectedDuration; " beat(s)"
    FOR i = 1 TO currentSong.count
        LOCATE START_Y + i - 1, 1
        PRINT USING "##"; i; ": "; currentSong.notes(i).pitch; " - "; currentSong.notes(i).duration; " beats"
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
    ELSEIF btn > 0 THEN
        IF btn AND 1 THEN
            ' Left click places the currently selected note
            IF currentSong.count < 1024 THEN
                currentSong.count = currentSong.count + 1
                IF selectedPitch = "R" THEN
                    currentSong.notes(currentSong.count).pitch = "R  "
                ELSE
                    pitch$ = selectedPitch + LTRIM$(STR$(selectedOctave))
                    currentSong.notes(currentSong.count).pitch = LEFT$(pitch$ + "   ", 3)
                END IF
                currentSong.notes(currentSong.count).duration = selectedDuration
            END IF
        ELSEIF btn AND 2 THEN
            ' Right click edits the clicked note if any
            row = INT(mouseY / 16) + 1
            index = row - START_Y + 1
            IF index >= 1 AND index <= currentSong.count THEN
                CALL EditNote(index)
            END IF
        END IF
    END IF
END SUB

SUB SaveSong(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".QMS" THEN filename$ = filename$ + ".qms"
    OPEN filename$ FOR BINARY AS #1
    PUT #1, , timeSigN
    PUT #1, , timeSigD
    PUT #1, , currentSong.count
    FOR i = 1 TO currentSong.count
        PUT #1, , currentSong.notes(i)
    NEXT
    CLOSE #1
END SUB

SUB LoadSong(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".QMS" THEN filename$ = filename$ + ".qms"
    IF _FILEEXISTS(filename$) THEN
        OPEN filename$ FOR BINARY AS #1
        GET #1, , timeSigN
        GET #1, , timeSigD
        GET #1, , currentSong.count
        FOR i = 1 TO currentSong.count
            GET #1, , currentSong.notes(i)
        NEXT
        CLOSE #1
    END IF
END SUB

SUB PlaySong
    FOR i = 1 TO currentSong.count
        IF LEFT$(currentSong.notes(i).pitch, 1) = "R" THEN
            _DELAY currentSong.notes(i).duration * 0.5
        ELSE
            PLAY currentSong.notes(i).pitch
            _DELAY currentSong.notes(i).duration * 0.5
        END IF
    NEXT
END SUB

" Edit the pitch, octave and duration of a note via a simple menu
SUB EditNote(i AS INTEGER)
    DIM notes$(13)
    notes$(1) = "C": notes$(2) = "C#": notes$(3) = "D": notes$(4) = "D#"
    notes$(5) = "E": notes$(6) = "F": notes$(7) = "F#"
    notes$(8) = "G": notes$(9) = "G#": notes$(10) = "A": notes$(11) = "A#": notes$(12) = "B": notes$(13) = "R"

    DIM pitch$, oct%, dur!
    CLS
    PRINT "Edit Note"; i
    CALL PromptNote(notes$(), pitch$, oct%, dur!)
    currentSong.notes(i).pitch = LEFT$(pitch$ + "   ", 3)
    currentSong.notes(i).duration = dur!
END SUB

' Show a list of notes and prompt the user to choose
SUB PromptNote(n$(), note$, octave AS INTEGER, dur AS SINGLE)
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
END SUB

' Choose which note gets placed with the left mouse button
SUB ChoosePlacement
    DIM notes$(13)
    notes$(1) = "C": notes$(2) = "C#": notes$(3) = "D": notes$(4) = "D#"
    notes$(5) = "E": notes$(6) = "F": notes$(7) = "F#"
    notes$(8) = "G": notes$(9) = "G#": notes$(10) = "A": notes$(11) = "A#": notes$(12) = "B": notes$(13) = "R"
    CLS
    PRINT "Select note to place"
    CALL PromptNote(notes$(), selectedPitch, selectedOctave, selectedDuration)
    IF selectedPitch = "R" THEN selectedOctave = 0
END SUB

' Change the time signature
SUB ChangeTimeSignature
    INPUT "Beats per measure: ", timeSigN
    INPUT "Beat value (e.g. 4 for quarter): ", timeSigD
    IF timeSigN <= 0 THEN timeSigN = 4
    IF timeSigD <= 0 THEN timeSigD = 4
END SUB

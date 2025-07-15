' QBasic Music Studio
' Minimal music tracker example for QB64 Phoenix Edition

OPTION BASE 1

TYPE Note
    pitch AS STRING * 2   ' e.g. "C4", "A#3"
    duration AS SINGLE    ' note length in beats
END TYPE

TYPE Song
    notes(1 TO 1024) AS Note
    count AS INTEGER
END TYPE

DIM shared currentSong AS Song
DIM shared mouseX AS INTEGER, mouseY AS INTEGER

DECLARE SUB Init()
DECLARE SUB MainLoop()
DECLARE SUB Render()
DECLARE SUB HandleMouse()
DECLARE SUB SaveSong(filename$)
DECLARE SUB LoadSong(filename$)
DECLARE SUB PlaySong()

CALL Init
CALL MainLoop

SUB Init
    SCREEN _NEWIMAGE(640, 480, 32)
    _TITLE "QBasic Music Studio"
    currentSong.count = 0
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
    PRINT "Notes: "; currentSong.count
    FOR i = 1 TO currentSong.count
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
    ELSEIF btn > 0 THEN
        ' Simple example: left click adds a C4 quarter note
        IF currentSong.count < 1024 THEN
            currentSong.count = currentSong.count + 1
            currentSong.notes(currentSong.count).pitch = "C4"
            currentSong.notes(currentSong.count).duration = 1
        END IF
    END IF
END SUB

SUB SaveSong(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".QMS" THEN filename$ = filename$ + ".qms"
    OPEN filename$ FOR BINARY AS #1
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
        GET #1, , currentSong.count
        FOR i = 1 TO currentSong.count
            GET #1, , currentSong.notes(i)
        NEXT
        CLOSE #1
    END IF
END SUB

SUB PlaySong
    FOR i = 1 TO currentSong.count
        PLAY currentSong.notes(i).pitch
        _DELAY currentSong.notes(i).duration * 0.5
    NEXT
END SUB

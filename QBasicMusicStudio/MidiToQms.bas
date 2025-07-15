' MidiToQms.bas - MIDI to QMS Converter for QBasic Music Studio v2
' Converts MIDI note events to QMS format for QB64 Phoenix Edition

OPTION BASE 1

TYPE Note
    pitch AS STRING * 3   ' e.g., "C#4", "R  "
    duration AS SINGLE    ' duration in beats
    volume AS SINGLE     ' volume (0 to 1)
END TYPE

TYPE Pattern
    notes(1 TO 1024) AS Note
    count AS INTEGER
    timeSigN AS INTEGER
    timeSigD AS INTEGER
    tempo AS SINGLE
END TYPE

TYPE Track
    patterns(1 TO 16) AS Pattern
    patternCount AS INTEGER
END TYPE

TYPE Song
    tracks(1 TO 4) AS Track
    trackCount AS INTEGER
END TYPE

DIM SHARED song AS Song
DIM SHARED noteNames$(12)

' Initialize note names for MIDI mapping
noteNames$(1) = "C ": noteNames$(2) = "C#": noteNames$(3) = "D "
noteNames$(4) = "D#": noteNames$(5) = "E ": noteNames$(6) = "F "
noteNames$(7) = "F#": noteNames$(8) = "G ": noteNames$(9) = "G#"
noteNames$(10) = "A ": noteNames$(11) = "A#": noteNames$(12) = "B "

INPUT "MIDI file to convert: ", midi$
INPUT "Output QMS file: ", qms$

CALL ConvertMidiToQms(midi$, qms$)

SUB ConvertMidiToQms(midi$, qms$)
    IF _FILEEXISTS(midi$) THEN
        OPEN midi$ FOR BINARY AS #1
        IF LOF(1) > 0 THEN
            CALL ParseMidi(midi$)
            CALL SaveQms(qms$)
            CLOSE #1
            PRINT "Converted "; midi$; " to "; qms$
        ELSE
            CLOSE #1
            PRINT "Error: MIDI file is empty or corrupted."
        END IF
    ELSE
        PRINT "Error: MIDI file not found: "; midi$
    END IF
    SLEEP 2
END SUB

SUB ParseMidi(midi$)
    DIM header AS STRING * 14
    GET #1, 1, header
    IF LEFT$(header, 4) <> "MThd" THEN
        PRINT "Error: Invalid MIDI header."
        EXIT SUB
    END IF

    ' Read header: format (2 bytes), tracks (2 bytes), division (2 bytes)
    formatType = CVI(MID$(header, 9, 2))
    trackCount = CVI(MID$(header, 11, 2))
    division = CVI(MID$(header, 13, 2))
    IF formatType > 1 OR trackCount > 4 THEN
        PRINT "Error: Only MIDI format 0 or 1 with up to 4 tracks supported."
        EXIT SUB
    END IF
    song.trackCount = trackCount
    IF song.trackCount = 0 THEN song.trackCount = 1
    FOR t = 1 TO song.trackCount
        song.tracks(t).patternCount = 1
        song.tracks(t).patterns(1).timeSigN = 4
        song.tracks(t).patterns(1).timeSigD = 4
        song.tracks(t).patterns(1).tempo = 120
    NEXT

    ' Process each track
    FOR t = 1 TO song.trackCount
        DIM chunk AS STRING * 8
        GET #1, , chunk
        IF LEFT$(chunk, 4) <> "MTrk" THEN
            PRINT "Error: Invalid track chunk."
            EXIT SUB
        END IF
        trackLength = CVL(MID$(chunk, 5, 4))
        trackEnd = LOC(1) + trackLength

        ' Track state
        noteCount = 0
        DIM noteStart(127) AS LONG  ' Store start ticks for active notes
        DIM noteVelocity(127) AS INTEGER
        ticksPerBeat = division
        currentTempo = 500000  ' Default: 120 BPM (500000 us/quarter)
        runningStatus = 0

        WHILE LOC(1) < trackEnd AND noteCount < 1024
            deltaTicks = ReadVariableLength
            IF deltaTicks < 0 THEN EXIT WHILE

            status = ReadByte
            IF status = &HFF THEN
                ' Meta-event
                metaType = ReadByte
                metaLength = ReadVariableLength
                IF metaType = &H51 AND metaLength = 3 THEN
                    ' Tempo (microseconds per quarter note)
                    tempoBytes$ = SPACE$(3)
                    GET #1, , tempoBytes$
                    currentTempo = CVL(CHR$(0) + tempoBytes$)
                    song.tracks(t).patterns(1).tempo = 60000000 / currentTempo
                ELSEIF metaType = &H58 AND metaLength = 4 THEN
                    ' Time signature
                    timeSig$ = SPACE$(4)
                    GET #1, , timeSig$
                    song.tracks(t).patterns(1).timeSigN = ASC(LEFT$(timeSig$, 1))
                    song.tracks(t).patterns(1).timeSigD = 2 ^ ASC(MID$(timeSig$, 2, 1))
                ELSE
                    ' Skip other meta-events
                    SKIP$ = SPACE$(metaLength)
                    GET #1, , SKIP$
                END IF
            ELSEIF (status AND &HF0) = &H90 OR (status AND &HF0) = &H80 THEN
                ' Note-on or note-off
                IF status < &H80 THEN
                    ' Running status
                    noteNum = status
                    velocity = ReadByte
                    status = runningStatus
                ELSE
                    noteNum = ReadByte
                    velocity = ReadByte
                    runningStatus = status
                END IF
                IF (status AND &HF0) = &H90 AND velocity > 0 THEN
                    ' Note-on
                    noteStart(noteNum) = currentTicks
                    noteVelocity(noteNum) = velocity
                ELSE
                    ' Note-off (or note-on with velocity 0)
                    IF noteStart(noteNum) > 0 THEN
                        noteCount = noteCount + 1
                        durationTicks = currentTicks - noteStart(noteNum)
                        durationBeats = durationTicks / ticksPerBeat
                        IF durationBeats < 0.1 THEN durationBeats = 0.1
                        CALL MapMidiNote(noteNum, song.tracks(t).patterns(1).notes(noteCount).pitch)
                        song.tracks(t).patterns(1).notes(noteCount).duration = durationBeats
                        song.tracks(t).patterns(1).notes(noteCount).volume = noteVelocity(noteNum) / 127
                        noteStart(noteNum) = 0
                    END IF
                END IF
            ELSE
                ' Skip other events
                IF (status AND &HF0) = &HC0 OR (status AND &HF0) = &HD0 THEN
                    SKIP$ = SPACE$(1)
                    GET #1, , SKIP$
                ELSEIF (status AND &HF0) = &HA0 OR (status AND &HF0) = &HB0 OR (status AND &HF0) = &HE0 THEN
                    SKIP$ = SPACE$(2)
                    GET #1, , SKIP$
                END IF
                runningStatus = status
            END IF
            currentTicks = currentTicks + deltaTicks
        WEND
        song.tracks(t).patterns(1).count = noteCount
    NEXT
END SUB

SUB SaveQms(qms$)
    IF RIGHT$(UCASE$(qms$), 4) <> ".QMS" THEN qms$ = qms$ + ".qms"
    OPEN qms$ FOR BINARY AS #2
    IF LOF(2) = 0 THEN
        PUT #2, , song.trackCount
        FOR t = 1 TO song.trackCount
            PUT #2, , song.tracks(t).patternCount
            FOR p = 1 TO song.tracks(t).patternCount
                PUT #2, , song.tracks(t).patterns(p).timeSigN
                PUT #2, , song.tracks(t).patterns(p).timeSigD
                PUT #2, , song.tracks(t).patterns(p).tempo
                PUT #2, , song.tracks(t).patterns(p).count
                FOR i = 1 TO song.tracks(t).patterns(p).count
                    PUT #2, , song.tracks(t).patterns(p).notes(i)
                NEXT
            NEXT
        NEXT
        CLOSE #2
    ELSE
        CLOSE #2
        PRINT "Error: QMS file already exists or inaccessible."
        SLEEP 2
    END IF
END SUB

FUNCTION ReadVariableLength&
    result = 0
    DO
        byte = ReadByte
        IF byte < 0 THEN
            ReadVariableLength& = -1
            EXIT FUNCTION
        END IF
        result = result * 128 + (byte AND &H7F)
    LOOP WHILE byte AND &H80
    ReadVariableLength& = result
END FUNCTION

FUNCTION ReadByte%
    IF EOF(1) THEN
        ReadByte% = -1
        EXIT FUNCTION
    END IF
    byte$ = " "
    GET #1, , byte$
    ReadByte% = ASC(byte$)
END FUNCTION

SUB MapMidiNote(midiNote AS INTEGER, pitch$)
    IF midiNote < 36 OR midiNote > 95 THEN
        pitch$ = "R  "
        EXIT SUB
    END IF
    noteIdx = (midiNote MOD 12) + 1
    octave = (midiNote \ 12) - 1
    pitch$ = LEFT$(noteNames$(noteIdx) + LTRIM$(STR$(octave)) + "   ", 3)
END SUB

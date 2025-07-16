' QBasic Music Studio v5 - FL Studio Inspired with InForm-PE and BASS.DLL
' Final version for QB64 Phoenix Edition with SoundFont, OGG/WAV/MP3/MOD, and Wiki enhancements

OPTION BASE 1

'$INCLUDE:'InForm.bi'

' BASS.DLL declarations
DECLARE LIBRARY "bass"
    FUNCTION BASS_Init% (BYVAL device AS INTEGER, BYVAL freq AS LONG, BYVAL flags AS LONG, BYVAL win AS LONG)
    SUB BASS_Free
    FUNCTION BASS_SampleLoad& (BYVAL mem AS INTEGER, BYVAL f AS STRING, BYVAL offset AS LONG, BYVAL length AS LONG, BYVAL max AS LONG, BYVAL flags AS LONG)
    SUB BASS_SampleFree (BYVAL handle AS LONG)
    FUNCTION BASS_SamplePlay& (BYVAL handle AS LONG)
    FUNCTION BASS_StreamCreateFile& (BYVAL mem AS INTEGER, BYVAL f AS STRING, BYVAL offset AS LONG, BYVAL length AS LONG, BYVAL flags AS LONG)
    SUB BASS_StreamFree (BYVAL handle AS LONG)
    FUNCTION BASS_StreamPlay% (BYVAL handle AS LONG, BYVAL flush AS INTEGER, BYVAL flags AS LONG)
    FUNCTION BASS_MusicLoad& (BYVAL mem AS INTEGER, BYVAL f AS STRING, BYVAL offset AS LONG, BYVAL length AS LONG, BYVAL flags AS LONG)
    SUB BASS_MusicFree (BYVAL handle AS LONG)
    FUNCTION BASS_MusicPlay% (BYVAL handle AS LONG)
    FUNCTION BASS_ChannelSetAttributes% (BYVAL handle AS LONG, BYVAL freq AS LONG, BYVAL volume AS LONG, BYVAL pan AS LONG)
    FUNCTION BASS_ChannelSet3DPosition% (BYVAL handle AS LONG, BYVAL pos AS _OFFSET, BYVAL orient AS _OFFSET, BYVAL vel AS _OFFSET)
    SUB BASS_Apply3D
    FUNCTION BASS_SetEAXParameters% (BYVAL env AS LONG, BYVAL vol AS SINGLE, BYVAL decay AS SINGLE, BYVAL damp AS SINGLE)
    FUNCTION BASS_ChannelPause% (BYVAL handle AS LONG)
    FUNCTION BASS_ChannelResume% (BYVAL handle AS LONG)
    FUNCTION BASS_ChannelGetData& (BYVAL handle AS LONG, BYVAL buffer AS _OFFSET, BYVAL length AS LONG)
END DECLARE

TYPE Note
    pitch AS STRING * 3   ' e.g., "C#4", "R  "
    duration AS SINGLE    ' duration in beats
    volume AS SINGLE     ' 0 to 1
    pan AS SINGLE        ' -1 (left) to 1 (right)
END TYPE

TYPE Pattern
    notes(1 TO 1024) AS Note
    count AS INTEGER
    repeatCount AS INTEGER
END TYPE

TYPE Track
    patterns(1 TO 16) AS Pattern
    patternCount AS INTEGER
    instrument AS STRING * 50  ' e.g., "SINE", "WAV:piano.wav", "OGG:drum.ogg", "MP3:song.mp3", "MOD:track.mod", "SF:piano", "CHIP"
END TYPE

TYPE Song
    tracks(1 TO 4) AS Track
    trackCount AS INTEGER
    tempo AS SINGLE
    timeSigN AS INTEGER
    timeSigD AS INTEGER
    arrangement(1 TO 256) AS INTEGER
    arrangeCount AS INTEGER
    referenceTrack AS STRING * 50
END TYPE

DIM SHARED currentSong AS Song
DIM SHARED undoSong AS Song
DIM SHARED currentTrack AS INTEGER
DIM SHARED currentPattern AS INTEGER
DIM SHARED canUndo AS INTEGER
DIM SHARED delayAmount AS SINGLE
DIM SHARED reverbAmount AS SINGLE
DIM SHARED eqLowPass AS SINGLE
DIM SHARED pianoRollZoom AS SINGLE
DIM SHARED pianoRollOffset AS INTEGER
DIM SHARED playlistOffset AS INTEGER
DIM SHARED referenceTrackHandle AS LONG
DIM SHARED sampleHandles(1 TO 127, 1 TO 2) AS LONG ' Two velocity layers
DIM SHARED waveformImage AS LONG
CONST PIANO_ROLL_HEIGHT = 200
CONST PLAYLIST_HEIGHT = 100
CONST WAVEFORM_HEIGHT = 50

' InForm-PE controls
DIM SHARED FormID AS LONG
DIM SHARED PianoRollCanvas AS LONG
DIM SHARED PlaylistCanvas AS LONG
DIM SHARED WaveformCanvas AS LONG
DIM SHARED SaveButton AS LONG, LoadButton AS LONG, PlayButton AS LONG
DIM SHARED NoteButton AS LONG, TimeSigButton AS LONG, TempoButton AS LONG
DIM SHARED ClearButton AS LONG, UndoButton AS LONG, WAVButton AS LONG
DIM SHARED PatternButton AS LONG, TrackButton AS LONG, ArrangeButton AS LONG
DIM SHARED HelpButton AS LONG, DemoButton AS LONG, MidiButton AS LONG
DIM SHARED ZoomInButton AS LONG, ZoomOutButton AS LONG
DIM SHARED TempoField AS LONG, TimeSigNField AS LONG, TimeSigDField AS LONG
DIM SHARED DelayField AS LONG, ReverbField AS LONG, EQField AS LONG
DIM SHARED InstrumentField AS LONG, ReferenceTrackField AS LONG
DIM SHARED RefVolumeSlider AS LONG

DECLARE SUB Init()
DECLARE SUB RenderPianoRoll()
DECLARE SUB RenderPlaylist()
DECLARE SUB RenderWaveform()
DECLARE SUB HandleMouse(canvas AS LONG, x AS INTEGER, y AS INTEGER, btn AS INTEGER, shift AS INTEGER)
DECLARE SUB EditNote(t AS INTEGER, p AS INTEGER, i AS INTEGER)
DECLARE SUB PromptNote(n$(), note$, octave AS INTEGER, dur AS SINGLE, vol AS SINGLE, pan AS SINGLE)
DECLARE SUB ChoosePlacement()
DECLARE SUB ChangeTimeSignature()
DECLARE SUB ChangeTempo()
DECLARE SUB SaveSong(filename$)
DECLARE SUB LoadSong(filename$)
DECLARE SUB PlaySong()
DECLARE SUB PlayNote(pitch$, duration AS SINGLE, volume AS SINGLE, pan AS SINGLE, instrument$)
DECLARE SUB PlayReferenceTrack()
DECLARE SUB SaveUndo()
DECLARE SUB Undo()
DECLARE SUB ClearSong()
DECLARE SUB ExportWAV(filename$)
DECLARE SUB AddPattern()
DECLARE SUB SelectTrack()
DECLARE SUB SelectPattern()
DECLARE SUB ArrangePatterns()
DECLARE SUB ShowHelp()
DECLARE SUB LoadDemo()
DECLARE SUB ConvertMidiToQms(midi$, qms$)
DECLARE SUB PlayTrackedNote(noteNum AS INTEGER, duration AS SINGLE, volume AS SINGLE, pan AS SINGLE, instrument$)
DECLARE SUB InitSoundFont(folder$)
DECLARE SUB FreeSoundFont()

CALL Init

' InForm-PE event loop
DO
    _LIMIT 60
    CALL RenderPianoRoll
    CALL RenderPlaylist
    CALL RenderWaveform
LOOP UNTIL _KEYDOWN(27)

SUB Init
    IF BASS_Init(-1, 44100, 4, 0) = 0 THEN
        PRINT "Failed to initialize BASS.DLL"
        END
    END IF
    BASS_SetEAXParameters 0, reverbAmount, -1, -1
    waveformImage = _NEWIMAGE(800, WAVEFORM_HEIGHT, 32)
    
    FormID = _NEWFORM("QBasic Music Studio v5", 800, 650)
    PianoRollCanvas = _NEWPICTUREBOX(FormID, 0, 100, 800, PIANO_ROLL_HEIGHT)
    PlaylistCanvas = _NEWPICTUREBOX(FormID, 0, 350, 800, PLAYLIST_HEIGHT)
    WaveformCanvas = _NEWPICTUREBOX(FormID, 0, 500, 800, WAVEFORM_HEIGHT)
    SaveButton = _NEWBUTTON(FormID, "Save", 10, 10, 80, 30)
    LoadButton = _NEWBUTTON(FormID, "Load", 100, 10, 80, 30)
    PlayButton = _NEWBUTTON(FormID, "Play", 190, 10, 80, 30)
    NoteButton = _NEWBUTTON(FormID, "Note", 280, 10, 80, 30)
    TimeSigButton = _NEWBUTTON(FormID, "TimeSig", 370, 10, 80, 30)
    TempoButton = _NEWBUTTON(FormID, "Tempo", 460, 10, 80, 30)
    ClearButton = _NEWBUTTON(FormID, "Clear", 550, 10, 80, 30)
    UndoButton = _NEWBUTTON(FormID, "Undo", 640, 10, 80, 30)
    WAVButton = _NEWBUTTON(FormID, "Export WAV", 10, 50, 80, 30)
    PatternButton = _NEWBUTTON(FormID, "New Pattern", 100, 50, 80, 30)
    TrackButton = _NEWBUTTON(FormID, "Track", 190, 50, 80, 30)
    ArrangeButton = _NEWBUTTON(FormID, "Arrange", 280, 50, 80, 30)
    HelpButton = _NEWBUTTON(FormID, "Help", 370, 50, 80, 30)
    DemoButton = _NEWBUTTON(FormID, "Demo", 460, 50, 80, 30)
    MidiButton = _NEWBUTTON(FormID, "MIDI Import", 550, 50, 80, 30)
    ZoomInButton = _NEWBUTTON(FormID, "+", 640, 50, 40, 30)
    ZoomOutButton = _NEWBUTTON(FormID, "-", 690, 50, 40, 30)
    TempoField = _NEWTEXTBOX(FormID, "120", 10, 90, 80, 20)
    TimeSigNField = _NEWTEXTBOX(FormID, "4", 100, 90, 40, 20)
    TimeSigDField = _NEWTEXTBOX(FormID, "4", 150, 90, 40, 20)
    DelayField = _NEWTEXTBOX(FormID, "0.3", 200, 90, 80, 20)
    ReverbField = _NEWTEXTBOX(FormID, "0.2", 290, 90, 80, 20)
    EQField = _NEWTEXTBOX(FormID, "0.5", 380, 90, 80, 20)
    InstrumentField = _NEWTEXTBOX(FormID, "SINE", 470, 90, 100, 20)
    ReferenceTrackField = _NEWTEXTBOX(FormID, "", 580, 90, 100, 20)
    RefVolumeSlider = _NEWTRACKBAR(FormID, 0, 100, 50, 690, 90, 100, 20)
    
    currentSong.trackCount = 1
    currentSong.tracks(1).patternCount = 1
    currentSong.tracks(1).patterns(1).count = 0
    currentSong.tracks(1).patterns(1).repeatCount = 1
    currentSong.tracks(1).instrument = "SINE"
    currentSong.tempo = 120
    currentSong.timeSigN = 4
    currentSong.timeSigD = 4
    currentSong.arrangeCount = 1
    currentSong.arrangement(1) = 1
    currentSong.referenceTrack = ""
    currentTrack = 1
    currentPattern = 1
    delayAmount = 0.3
    reverbAmount = 0.2
    eqLowPass = 0.5
    pianoRollZoom = 1
    pianoRollOffset = 0
    playlistOffset = 0
END SUB

SUB InitSoundFont(folder$)
    FOR i = 36 TO 95
        note$ = MapMidiToNote$(i)
        IF _FILEEXISTS(folder$ + "/" + note$ + "_127.wav") THEN
            sampleHandles(i, 1) = BASS_SampleLoad(0, folder$ + "/" + note$ + "_127.wav" + CHR$(0), 0, 0, 3, 4)
        END IF
        IF _FILEEXISTS(folder$ + "/" + note$ + "_64.wav") THEN
            sampleHandles(i, 2) = BASS_SampleLoad(0, folder$ + "/" + note$ + "_64.wav" + CHR$(0), 0, 0, 3, 4)
        END IF
        IF sampleHandles(i, 1) = 0 AND sampleHandles(i, 2) = 0 THEN
            IF _FILEEXISTS(folder$ + "/default.wav") THEN
                sampleHandles(i, 1) = BASS_SampleLoad(0, folder$ + "/default.wav" + CHR$(0), 0, 0, 3, 4)
            END IF
        END IF
    NEXT
END SUB

SUB FreeSoundFont
    FOR i = 36 TO 95
        FOR v = 1 TO 2
            IF sampleHandles(i, v) <> 0 THEN BASS_SampleFree sampleHandles(i, v)
            sampleHandles(i, v) = 0
        NEXT
    NEXT
END SUB

SUB RenderPianoRoll
    _DEST PianoRollCanvas
    LINE (0, 0)-(800, PIANO_ROLL_HEIGHT), _RGB(255, 255, 255), BF
    FOR i = 1 TO 12
        y = (i - 1) * (PIANO_ROLL_HEIGHT / 12)
        LINE (0, y)-(800, y), _RGB(0, 0, 0)
        _PRINTSTRING (2, y + 2), STR$(i) + ": " + MID$("C C#D D#E F F#G G#A A#B ", (i - 1) * 3 + 1, 3)
    NEXT
    x = -pianoRollOffset * 20 * pianoRollZoom
    FOR i = 1 TO currentSong.tracks(currentTrack).patterns(currentPattern).count
        note$ = currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pitch
        IF LEFT$(note$, 1) <> "R" THEN
            noteIdx = INSTR("C C#D D#E F F#G G#A A#B ", LEFT$(note$, 2)) / 3 + 1
            y = (12 - noteIdx) * (PIANO_ROLL_HEIGHT / 12)
            xStart = x
            xEnd = x + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20 * pianoRollZoom
            IF xEnd > 0 AND xStart < 800 THEN
                LINE (xStart, y)-(xEnd, y + PIANO_ROLL_HEIGHT / 12), _RGB(0, 128, 255), BF
            END IF
        END IF
        x = x + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20 * pianoRollZoom
    NEXT
    _DEST 0
END SUB

SUB RenderPlaylist
    _DEST PlaylistCanvas
    LINE (0, 0)-(800, PLAYLIST_HEIGHT), _RGB(200, 200, 200), BF
    x = -playlistOffset * 50
    FOR a = 1 TO currentSong.arrangeCount
        p = currentSong.arrangement(a)
        FOR t = 1 TO currentSong.trackCount
            IF p <= currentSong.tracks(t).patternCount THEN
                y = (t - 1) * (PLAYLIST_HEIGHT / 4)
                LINE (x, y)-(x + 50 * currentSong.tracks(t).patterns(p).repeatCount, y + PLAYLIST_HEIGHT / 4), _RGB(100, 100, 255), BF
                _PRINTSTRING (x + 2, y + 2), "T" + STR$(t) + " P" + STR$(p)
            END IF
        NEXT
        x = x + 50 * currentSong.tracks(currentTrack).patterns(p).repeatCount
    NEXT
    _DEST 0
END SUB

SUB RenderWaveform
    _DEST waveformImage
    LINE (0, 0)-(800, WAVEFORM_HEIGHT), _RGB(255, 255, 255), BF
    IF referenceTrackHandle <> 0 THEN
        DIM buffer(0 TO 799) AS INTEGER
        samplesRead = BASS_ChannelGetData(referenceTrackHandle, _OFFSET(buffer(0)), 800 * 2)
        IF samplesRead > 0 THEN
            FOR x = 0 TO 799
                sample = buffer(x) / 32768
                y = WAVEFORM_HEIGHT / 2 - sample * (WAVEFORM_HEIGHT / 2)
                LINE (x, WAVEFORM_HEIGHT / 2)-(x, y), _RGB(0, 0, 255)
            NEXT
        END IF
    END IF
    _DEST WaveformCanvas
    _PUTIMAGE (0, 0), waveformImage
    _DEST 0
END SUB

SUB HandleMouse(canvas AS LONG, x AS INTEGER, y AS INTEGER, btn AS INTEGER, shift AS INTEGER)
    STATIC draggingNote AS INTEGER, draggingTrack AS INTEGER, draggingPattern AS INTEGER
    STATIC lastClickTime AS DOUBLE, lastClickX AS INTEGER, lastClickY AS INTEGER
    IF _MOUSEINPUT THEN
        IF _MOUSEWHEEL THEN
            pianoRollZoom = pianoRollZoom + _MOUSEWHEEL * 0.1
            IF pianoRollZoom < 0.5 THEN pianoRollZoom = 0.5
            IF pianoRollZoom > 2 THEN pianoRollZoom = 2
        END IF
    END IF
    IF canvas = PianoRollCanvas THEN
        noteIdx = 12 - INT(y / (PIANO_ROLL_HEIGHT / 12))
        xPos = -pianoRollOffset * 20 * pianoRollZoom
        FOR i = 1 TO currentSong.tracks(currentTrack).patterns(currentPattern).count
            xStart = xPos
            xEnd = xPos + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20 * pianoRollZoom
            IF LEFT$(currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pitch, 1) <> "R" THEN
                noteY = (12 - (INSTR("C C#D D#E F F#G G#A A#B ", LEFT$(currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pitch, 2)) / 3 + 1)) * (PIANO_ROLL_HEIGHT / 12)
                IF ABS(y - noteY) < 5 THEN
                    IF btn AND 1 AND x >= xEnd - 5 AND x <= xEnd + 5 THEN
                        draggingNote = i
                        draggingTrack = currentTrack
                        draggingPattern = currentPattern
                        EXIT FOR
                    ELSEIF btn AND 2 AND shift AND x >= xStart AND x <= xEnd THEN
                        CALL SaveUndo
                        FOR j = i TO currentSong.tracks(currentTrack).patterns(currentPattern).count - 1
                            currentSong.tracks(currentTrack).patterns(currentPattern).notes(j) = currentSong.tracks(currentTrack).patterns(currentPattern).notes(j + 1)
                        NEXT
                        currentSong.tracks(currentTrack).patterns(currentPattern).count = currentSong.tracks(currentTrack).patterns(currentPattern).count - 1
                        BEEP
                        EXIT FOR
                    ELSEIF btn AND 2 AND x >= xStart AND x <= xEnd THEN
                        IF lastClickX = x AND lastClickY = y AND TIMER - lastClickTime < 0.3 THEN
                            CALL SaveUndo
                            CALL EditNote(currentTrack, currentPattern, i)
                            EXIT FOR
                        END IF
                        lastClickTime = TIMER
                        lastClickX = x
                        lastClickY = y
                    END IF
                END IF
            END IF
            xPos = xPos + currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration * 20 * pianoRollZoom
        NEXT
        IF btn AND 1 AND draggingNote = 0 AND currentSong.tracks(currentTrack).patterns(currentPattern).count < 1024 THEN
            CALL SaveUndo
            currentSong.tracks(currentTrack).patterns(currentPattern).count = currentSong.tracks(currentTrack).patterns(currentPattern).count + 1
            note$ = MID$("C C#D D#E F F#G G#A A#B ", (noteIdx - 1) * 3 + 1, 2) + LTRIM$(STR$(4))
            currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch = LEFT$(note$ + "   ", 3)
            currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).duration = 1
            currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).volume = 1
            currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pan = 0
            CALL PlayNote(currentSong.tracks(currentTrack).patterns(currentPattern).notes(currentSong.tracks(currentTrack).patterns(currentPattern).count).pitch, 1, 1, 0, currentSong.tracks(currentTrack).instrument)
        END IF
        IF draggingNote > 0 AND btn AND 1 THEN
            xPos = -pianoRollOffset * 20 * pianoRollZoom
            FOR i = 1 TO currentSong.tracks(draggingTrack).patterns(draggingPattern).count
                IF i <> draggingNote THEN
                    xPos = xPos + currentSong.tracks(draggingTrack).patterns(draggingPattern).notes(i).duration * 20 * pianoRollZoom
                ELSE
                    newDuration = (x - xPos) / (20 * pianoRollZoom)
                    IF newDuration < 0.1 THEN newDuration = 0.1
                    currentSong.tracks(draggingTrack).patterns(draggingPattern).notes(draggingNote).duration = newDuration
                    EXIT FOR
                END IF
            NEXT
        ELSEIF draggingNote > 0 AND btn = 0 THEN
            CALL SaveUndo
            draggingNote = 0
            draggingTrack = 0
            draggingPattern = 0
        END IF
    ELSEIF canvas = PlaylistCanvas THEN
        trackIdx = INT(y / (PLAYLIST_HEIGHT / 4)) + 1
        xPos = -playlistOffset * 50
        FOR a = 1 TO currentSong.arrangeCount
            xEnd = xPos + 50 * currentSong.tracks(trackIdx).patterns(currentSong.arrangement(a)).repeatCount
            IF x >= xPos AND x <= xEnd AND trackIdx >= 1 AND trackIdx <= currentSong.trackCount THEN
                IF btn AND 2 THEN
                    CALL SaveUndo
                    FOR j = a TO currentSong.arrangeCount - 1
                        currentSong.arrangement(j) = currentSong.arrangement(j + 1)
                    NEXT
                    currentSong.arrangeCount = currentSong.arrangeCount - 1
                    BEEP
                    EXIT FOR
                END IF
            END IF
            xPos = xEnd
        NEXT
        IF btn AND 1 AND currentSong.arrangeCount < 256 THEN
            CALL SaveUndo
            INPUT "Pattern number (1-"; currentSong.tracks(trackIdx).patternCount; "): ", p%
            IF p% >= 1 AND p% <= currentSong.tracks(trackIdx).patternCount THEN
                currentSong.arrangeCount = currentSong.arrangeCount + 1
                currentSong.arrangement(currentSong.arrangeCount) = p%
                INPUT "Repeat count: ", r%
                IF r% >= 1 THEN currentSong.tracks(trackIdx).patterns(p%).repeatCount = r%
            END IF
        END IF
    END IF
END SUB

SUB PlaySong
    IF currentSong.referenceTrack <> "" THEN CALL PlayReferenceTrack
    FOR a = 1 TO currentSong.arrangeCount
        p = currentSong.arrangement(a)
        FOR r = 1 TO currentSong.tracks(currentTrack).patterns(p).repeatCount
            FOR t = 1 TO currentSong.trackCount
                IF p <= currentSong.tracks(t).patternCount THEN
                    FOR i = 1 TO currentSong.tracks(t).patterns(p).count
                        CALL PlayNote(currentSong.tracks(t).patterns(p).notes(i).pitch, currentSong.tracks(t).patterns(p).notes(i).duration, currentSong.tracks(t).patterns(p).notes(i).volume, currentSong.tracks(t).patterns(p).notes(i).pan, currentSong.tracks(t).instrument)
                    NEXT
                END IF
            NEXT
        NEXT
    NEXT
    IF referenceTrackHandle <> 0 THEN
        BASS_ChannelPause referenceTrackHandle
        BASS_StreamFree referenceTrackHandle
        referenceTrackHandle = 0
    END IF
END SUB

SUB PlayReferenceTrack
    IF currentSong.referenceTrack <> "" THEN
        IF referenceTrackHandle <> 0 THEN
            BASS_StreamFree referenceTrackHandle
            referenceTrackHandle = 0
        END IF
        IF _FILEEXISTS(currentSong.referenceTrack) THEN
            referenceTrackHandle = BASS_StreamCreateFile(0, currentSong.referenceTrack + CHR$(0), 0, 0, 4)
            IF referenceTrackHandle <> 0 THEN
                BASS_ChannelSetAttributes referenceTrackHandle, -1, _VALUE(RefVolumeSlider), 0
                BASS_StreamPlay referenceTrackHandle, 0, 4
            END IF
        END IF
    END IF
END SUB

SUB PlayNote(pitch$, duration AS SINGLE, volume AS SINGLE, pan AS SINGLE, instrument$)
    IF LEFT$(pitch$, 1) = "R" THEN
        _DELAY duration * (60 / currentSong.tempo)
    ELSE
        noteNum = MapPitchToMidi(pitch$)
        CALL PlayTrackedNote(noteNum, duration, volume, pan, instrument$)
    END IF
END SUB

SUB PlayTrackedNote(noteNum AS INTEGER, duration AS SINGLE, volume AS SINGLE, pan AS SINGLE, instrument$)
    IF noteNum < 36 OR noteNum > 95 THEN EXIT SUB
    IF LEFT$(instrument$, 3) = "SF:" THEN
        folder$ = MID$(instrument$, 4)
        IF sampleHandles(noteNum, 1) = 0 AND sampleHandles(noteNum, 2) = 0 THEN CALL InitSoundFont(folder$)
        handle = 0
        IF volume >= 0.5 AND sampleHandles(noteNum, 1) <> 0 THEN
            handle = BASS_SamplePlay(sampleHandles(noteNum, 1))
        ELSEIF sampleHandles(noteNum, 2) <> 0 THEN
            handle = BASS_SamplePlay(sampleHandles(noteNum, 2))
        END IF
        IF handle <> 0 THEN
            BASS_ChannelSetAttributes handle, -1, volume * 100, pan * 100
            _DELAY duration * (60 / currentSong.tempo)
            BASS_ChannelPause handle
        END IF
    ELSEIF LEFT$(instrument$, 4) = "WAV:" OR LEFT$(instrument$, 4) = "OGG:" OR LEFT$(instrument$, 4) = "MP3:" THEN
        IF _FILEEXISTS(MID$(instrument$, 5)) THEN
            handle = BASS_StreamCreateFile(0, MID$(instrument$, 5) + CHR$(0), 0, 0, 0)
            IF handle <> 0 THEN
                BASS_ChannelSetAttributes handle, -1, volume * 100, pan * 100
                BASS_StreamPlay handle, 0, 0
                _DELAY duration * (60 / currentSong.tempo)
                BASS_StreamFree handle
            END IF
        END IF
    ELSEIF LEFT$(instrument$, 4) = "MOD:" THEN
        IF _FILEEXISTS(MID$(instrument$, 5)) THEN
            handle = BASS_MusicLoad(0, MID$(instrument$, 5) + CHR$(0), 0, 0, 4)
            IF handle <> 0 THEN
                BASS_ChannelSetAttributes handle, -1, volume * 100, pan * 100
                BASS_MusicPlay handle
                _DELAY duration * (60 / currentSong.tempo)
                BASS_MusicFree handle
            END IF
        END IF
    ELSEIF UCASE$(instrument$) = "CHIP" THEN
        sampleRate = 44100
        samples = duration * (60 / currentSong.tempo) * sampleRate
        FOR i = 1 TO samples
            t = i / sampleRate
            freq = 440 * (2 ^ ((noteNum - 69) / 12))
            sample = SGN(SIN(2 * 3.14159 * freq * t)) * volume * (1 - eqLowPass * (1 - freq / 20000))
            sample = sample + 0.5 * SIN(2 * 3.14159 * (freq * 1.5) * t) * volume
            _SNDRAW sample * (1 - pan), sample * (1 + pan)
        NEXT
        _DELAY duration * (60 / currentSong.tempo)
        IF delayAmount > 0 THEN
            _DELAY (duration * 0.5) * (60 / currentSong.tempo)
            FOR i = 1 TO samples
                t = i / sampleRate
                sample = SGN(SIN(2 * 3.14159 * freq * t)) * volume * delayAmount * (1 - eqLowPass * (1 - freq / 20000))
                sample = sample + 0.5 * SIN(2 * 3.14159 * (freq * 1.5) * t) * volume
                _SNDRAW sample * (1 - pan), sample * (1 + pan)
            NEXT
        END IF
        DO WHILE _SNDRAWLEN > 0
            _DELAY 0.01
        LOOP
    ELSE
        sampleRate = 44100
        samples = duration * (60 / currentSong.tempo) * sampleRate
        FOR i = 1 TO samples
            t = i / sampleRate
            freq = 440 * (2 ^ ((noteNum - 69) / 12))
            SELECT CASE UCASE$(instrument$)
                CASE "SINE"
                    sample = SIN(2 * 3.14159 * freq * t) * volume * (1 - eqLowPass * (1 - freq / 20000))
                CASE "SQUARE"
                    sample = SGN(SIN(2 * 3.14159 * freq * t)) * volume * (1 - eqLowPass * (1 - freq / 20000))
                CASE ELSE
                    sample = SIN(2 * 3.14159 * freq * t) * volume * (1 - eqLowPass * (1 - freq / 20000))
            END SELECT
            _SNDRAW sample * (1 - pan), sample * (1 + pan)
        NEXT
        _DELAY duration * (60 / currentSong.tempo)
        IF delayAmount > 0 THEN
            _DELAY (duration * 0.5) * (60 / currentSong.tempo)
            FOR i = 1 TO samples
                t = i / sampleRate
                SELECT CASE UCASE$(instrument$)
                    CASE "SINE"
                        sample = SIN(2 * 3.14159 * freq * t) * volume * delayAmount * (1 - eqLowPass * (1 - freq / 20000))
                    CASE "SQUARE"
                        sample = SGN(SIN(2 * 3.14159 * freq * t)) * volume * delayAmount * (1 - eqLowPass * (1 - freq / 20000))
                    CASE ELSE
                        sample = SIN(2 * 3.14159 * freq * t) * volume * delayAmount * (1 - eqLowPass * (1 - freq / 20000))
                END SELECT
                _SNDRAW sample * (1 - pan), sample * (1 + pan)
            NEXT
        END IF
        DO WHILE _SNDRAWLEN > 0
            _DELAY 0.01
        LOOP
    END IF
END SUB

FUNCTION MapPitchToMidi%(pitch$)
    note$ = LEFT$(pitch$, 2)
    octave = VAL(RIGHT$(pitch$, 1))
    SELECT CASE UCASE$(note$)
        CASE "C ": noteNum = 0
        CASE "C#": noteNum = 1
        CASE "D ": noteNum = 2
        CASE "D#": noteNum = 3
        CASE "E ": noteNum = 4
        CASE "F ": noteNum = 5
        CASE "F#": noteNum = 6
        CASE "G ": noteNum = 7
        CASE "G#": noteNum = 8
        CASE "A ": noteNum = 9
        CASE "A#": noteNum = 10
        CASE "B ": noteNum = 11
        CASE ELSE: noteNum = 0
    END SELECT
    MapPitchToMidi% = noteNum + (octave + 1) * 12
END FUNCTION

FUNCTION MapMidiToNote$(midiNote AS INTEGER)
    IF midiNote < 36 OR midiNote > 95 THEN MapMidiToNote$ = "R  ": EXIT FUNCTION
    noteNames$(1) = "C ": noteNames$(2) = "C#": noteNames$(3) = "D "
    noteNames$(4) = "D#": noteNames$(5) = "E ": noteNames$(6) = "F "
    noteNames$(7) = "F#": noteNames$(8) = "G ": noteNames$(9) = "G#"
    noteNames$(10) = "A ": noteNames$(11) = "A#": noteNames$(12) = "B "
    noteIdx = (midiNote MOD 12) + 1
    octave = (midiNote \ 12) - 1
    MapMidiToNote$ = LEFT$(noteNames$(noteIdx) + LTRIM$(STR$(octave)) + "   ", 3)
END FUNCTION

SUB EditNote(t AS INTEGER, p AS INTEGER, i AS INTEGER)
    DIM notes$(13)
    notes$(1) = "C": notes$(2) = "C#": notes$(3) = "D": notes$(4) = "D#"
    notes$(5) = "E": notes$(6) = "F": notes$(7) = "F#"
    notes$(8) = "G": notes$(9) = "G#": notes$(10) = "A": notes$(11) = "A#": notes$(12) = "B": notes$(13) = "R"
    DIM pitch$, oct%, dur!, vol!, pan!
    CLS
    PRINT "Edit Note"; i; " in Track"; t; " Pattern"; p
    CALL PromptNote(notes$(), pitch$, oct%, dur!, vol!, pan!)
    CALL SaveUndo
    IF LEFT$(pitch$, 1) = "R" THEN
        currentSong.tracks(t).patterns(p).notes(i).pitch = "R  "
    ELSE
        currentSong.tracks(t).patterns(p).notes(i).pitch = LEFT$(pitch$ + "   ", 3)
    END IF
    currentSong.tracks(t).patterns(p).notes(i).duration = dur!
    currentSong.tracks(t).patterns(p).notes(i).volume = vol!
    currentSong.tracks(t).patterns(p).notes(i).pan = pan!
    CALL PlayNote(currentSong.tracks(t).patterns(p).notes(i).pitch, dur!, vol!, pan!, currentSong.tracks(t).instrument)
END SUB

SUB PromptNote(n$(), note$, octave AS INTEGER, dur AS SINGLE, vol AS SINGLE, pan AS SINGLE)
    FOR j = 1 TO 13
        PRINT USING "##"; j; ": "; n$(j)
    NEXT
    INPUT "Choose note number or duration (W=4, H=2, Q=1, E=0.5): ", input$
    SELECT CASE UCASE$(input$)
        CASE "W": dur = 4: note$ = "R": octave% = 0
        CASE "H": dur = 2: note$ = "R": octave% = 0
        CASE "Q": dur = 1: note$ = "R": octave% = 0
        CASE "E": dur = 0.5: note$ = "R": octave% = 0
        CASE ELSE
            idx% = VAL(input$)
            IF idx% < 1 OR idx% > 13 THEN idx% = 1
            IF n$(idx%) <> "R" THEN
                INPUT "Octave (1-7): ", octave%
                IF octave% < 1 OR octave% > 7 THEN octave% = 4
                note$ = n$(idx%) + LTRIM$(STR$(octave%))
            ELSE
                note$ = "R"
                octave% = 0
            END IF
            INPUT "Duration (beats or W=4, H=2, Q=1, E=0.5): ", durInput$
            SELECT CASE UCASE$(durInput$)
                CASE "W": dur = 4
                CASE "H": dur = 2
                CASE "Q": dur = 1
                CASE "E": dur = 0.5
                CASE ELSE: dur = VAL(durInput$)
            END SELECT
    END SELECT
    IF dur <= 0 THEN dur = 1
    INPUT "Volume (0 to 1): ", vol
    IF vol < 0 OR vol > 1 THEN vol = 1
    INPUT "Pan (-1 left, 0 center, 1 right): ", pan
    IF pan < -1 OR pan > 1 THEN pan = 0
END SUB

SUB ChoosePlacement
    DIM notes$(13)
    notes$(1) = "C": notes$(2) = "C#": notes$(3) = "D": notes$(4) = "D#"
    notes$(5) = "E": notes$(6) = "F": notes$(7) = "F#"
    notes$(8) = "G": notes$(9) = "G#": notes$(10) = "A": notes$(11) = "A#": notes$(12) = "B": notes$(13) = "R"
    CLS
    PRINT "Select note to place"
    CALL PromptNote(notes$(), selectedPitch, selectedOctave, selectedDuration, selectedVolume, selectedPan)
    IF LEFT$(selectedPitch, 1) = "R" THEN selectedOctave = 0
    INPUT "Instrument (SINE, SQUARE, CHIP, WAV:filename, OGG:filename, MP3:filename, MOD:filename, SF:folder): ", instr$
    IF instr$ <> "" THEN currentSong.tracks(currentTrack).instrument = LEFT$(instr$ + SPACE$(50), 50)
    INPUT "Reference track (WAV/OGG/MP3, blank for none): ", refTrack$
    IF refTrack$ <> "" THEN currentSong.referenceTrack = LEFT$(refTrack$ + SPACE$(50), 50)
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
            currentSong.tracks(t).patterns(p).repeatCount = 1
        NEXT
    NEXT
    currentSong.arrangeCount = 1
    currentSong.arrangement(1) = 1
    IF referenceTrackHandle <> 0 THEN
        BASS_StreamFree referenceTrackHandle
        referenceTrackHandle = 0
    END IF
END SUB

SUB ExportWAV(filename$)
    IF RIGHT$(UCASE$(filename$), 4) <> ".WAV" THEN filename$ = filename$ + ".wav"
    sampleRate = 44100
    OPEN filename$ FOR BINARY AS #1
    header$ = "RIFF" + STRING$(4, 0) + "WAVEfmt " + MKL$(16) + MKI$(1) + MKI$(2) + MKL$(sampleRate) + MKL$(sampleRate * 4) + MKI$(4) + MKI$(16) + "data" + STRING$(4, 0)
    PUT #1, , header$
    DIM buffer(0 TO 4095) AS INTEGER
    FOR a = 1 TO currentSong.arrangeCount
        p = currentSong.arrangement(a)
        FOR r = 1 TO currentSong.tracks(currentTrack).patterns(p).repeatCount
            FOR t = 1 TO currentSong.trackCount
                IF p <= currentSong.tracks(t).patternCount THEN
                    FOR i = 1 TO currentSong.tracks(t).patterns(p).count
                        noteNum = MapPitchToMidi(currentSong.tracks(t).patterns(p).notes(i).pitch)
                        IF noteNum >= 36 AND noteNum <= 95 THEN
                            IF LEFT$(currentSong.tracks(t).instrument, 3) = "SF:" THEN
                                folder$ = MID$(currentSong.tracks(t).instrument, 4)
                                IF sampleHandles(noteNum, 1) = 0 AND sampleHandles(noteNum, 2) = 0 THEN CALL InitSoundFont(folder$)
                                handle = 0
                                IF currentSong.tracks(t).patterns(p).notes(i).volume >= 0.5 AND sampleHandles(noteNum, 1) <> 0 THEN
                                    handle = BASS_SamplePlay(sampleHandles(noteNum, 1))
                                ELSEIF sampleHandles(noteNum, 2) <> 0 THEN
                                    handle = BASS_SamplePlay(sampleHandles(noteNum, 2))
                                END IF
                                IF handle <> 0 THEN
                                    BASS_ChannelSetAttributes handle, -1, currentSong.tracks(t).patterns(p).notes(i).volume * 100, currentSong.tracks(t).patterns(p).notes(i).pan * 100
                                    samples = currentSong.tracks(t).patterns(p).notes(i).duration * (60 / currentSong.tempo) * sampleRate
                                    WHILE samples > 0
                                        samplesRead = BASS_ChannelGetData(handle, _OFFSET(buffer(0)), MIN(4096, samples * 4))
                                        IF samplesRead <= 0 THEN EXIT WHILE
                                        FOR s = 0 TO samplesRead \ 2 - 1
                                            PUT #1, , buffer(s)
                                        NEXT
                                        samples = samples - (samplesRead \ 4)
                                    WEND
                                    BASS_ChannelPause handle
                                END IF
                            ELSEIF LEFT$(currentSong.tracks(t).instrument, 4) = "WAV:" OR LEFT$(currentSong.tracks(t).instrument, 4) = "OGG:" OR LEFT$(currentSong.tracks(t).instrument, 4) = "MP3:" THEN
                                IF _FILEEXISTS(MID$(currentSong.tracks(t).instrument, 5)) THEN
                                    handle = BASS_StreamCreateFile(0, MID$(currentSong.tracks(t).instrument, 5) + CHR$(0), 0, 0, 0)
                                    IF handle <> 0 THEN
                                        BASS_ChannelSetAttributes handle, -1, currentSong.tracks(t).patterns(p).notes(i).volume * 100, currentSong.tracks(t).patterns(p).notes(i).pan * 100
                                        samples = currentSong.tracks(t).patterns(p).notes(i).duration * (60 / currentSong.tempo) * sampleRate
                                        WHILE samples > 0
                                            samplesRead = BASS_ChannelGetData(handle, _OFFSET(buffer(0)), MIN(4096, samples * 4))
                                            IF samplesRead <= 0 THEN EXIT WHILE
                                            FOR s = 0 TO samplesRead \ 2 - 1
                                                PUT #1, , buffer(s)
                                            NEXT
                                            samples = samples - (samplesRead \ 4)
                                        WEND
                                        BASS_StreamFree handle
                                    END IF
                                END IF
                            ELSEIF LEFT$(currentSong.tracks(t).instrument, 4) = "MOD:" THEN
                                IF _FILEEXISTS(MID$(currentSong.tracks(t).instrument, 5)) THEN
                                    handle = BASS_MusicLoad(0, MID$(currentSong.tracks(t).instrument, 5) + CHR$(0), 0, 0, 4)
                                    IF handle <> 0 THEN
                                        BASS_ChannelSetAttributes handle, -1, currentSong.tracks(t).patterns(p).notes(i).volume * 100, currentSong.tracks(t).patterns(p).notes(i).pan * 100
                                        samples = currentSong.tracks(t).patterns(p).notes(i).duration * (60 / currentSong.tempo) * sampleRate
                                        WHILE samples > 0
                                            samplesRead = BASS_ChannelGetData(handle, _OFFSET(buffer(0)), MIN(4096, samples * 4))
                                            IF samplesRead <= 0 THEN EXIT WHILE
                                            FOR s = 0 TO samplesRead \ 2 - 1
                                                PUT #1, , buffer(s)
                                            NEXT
                                            samples = samples - (samplesRead \ 4)
                                        WEND
                                        BASS_MusicFree handle
                                    END IF
                                END IF
                            ELSE
                                freq = 440 * (2 ^ ((noteNum - 69) / 12))
                                samples = currentSong.tracks(t).patterns(p).notes(i).duration * (60 / currentSong.tempo) * sampleRate
                                FOR s = 1 TO samples
                                    t = s / sampleRate
                                    SELECT CASE UCASE$(currentSong.tracks(t).instrument)
                                        CASE "SINE"
                                            sample = SIN(2 * 3.14159 * freq * t) * currentSong.tracks(t).patterns(p).notes(i).volume * (1 - eqLowPass * (1 - freq / 20000))
                                        CASE "SQUARE"
                                            sample = SGN(SIN(2 * 3.14159 * freq * t)) * currentSong.tracks(t).patterns(p).notes(i).volume * (1 - eqLowPass * (1 - freq / 20000))
                                        CASE "CHIP"
                                            sample = SGN(SIN(2 * 3.14159 * freq * t)) * currentSong.tracks(t).patterns(p).notes(i).volume * (1 - eqLowPass * (1 - freq / 20000))
                                            sample = sample + 0.5 * SIN(2 * 3.14159 * (freq * 1.5) * t) * currentSong.tracks(t).patterns(p).notes(i).volume
                                        CASE ELSE
                                            sample = SIN(2 * 3.14159 * freq * t) * currentSong.tracks(t).patterns(p).notes(i).volume * (1 - eqLowPass * (1 - freq / 20000))
                                    END SELECT
                                    sampleL = sample * (1 - currentSong.tracks(t).patterns(p).notes(i).pan)
                                    sampleR = sample * (1 + currentSong.tracks(t).patterns(p).notes(i).pan)
                                    PUT #1, , CVI(sampleL * 32767)
                                    PUT #1, , CVI(sampleR * 32767)
                                NEXT
                            END IF
                        END IF
                    NEXT
                END IF
            NEXT
        NEXT
    NEXT
    fileSize = LOC(1)
    SEEK #1, 5
    PUT #1, , MKL$(fileSize - 8)
    SEEK #1, 41
    PUT #1, , MKL$(fileSize - 44)
    CLOSE #1
    PRINT "WAV file exported: "; filename$
    SLEEP 2
END SUB

SUB AddPattern
    IF currentSong.tracks(currentTrack).patternCount < 16 THEN
        CALL SaveUndo
        currentSong.tracks(currentTrack).patternCount = currentSong.tracks(currentTrack).patternCount + 1
        currentSong.tracks(currentTrack).patterns(currentSong.tracks(currentTrack).patternCount).count = 0
        currentSong.tracks(currentTrack).patterns(currentSong.tracks(currentTrack).patternCount).repeatCount = 1
        currentPattern = currentSong.tracks(currentTrack).patternCount
        PRINT "New pattern"; currentPattern; "added to Track"; currentTrack
        SLEEP 1
    ELSE
        PRINT "Maximum patterns (16) reached for Track"; currentTrack
        BEEP
        SLEEP 2
    END IF
END SUB

SUB SelectTrack
    INPUT "Select track (1-4): ", t%
    IF t% >= 1 AND t% <= 4 THEN
        IF currentSong.tracks(t%).patternCount = 0 THEN
            currentSong.tracks(t%).patternCount = 1
            currentSong.tracks(t%).patterns(1).count = 0
            currentSong.tracks(t%).patterns(1).repeatCount = 1
            currentSong.tracks(t%).instrument = "SINE"
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

SUB ArrangePatterns
    CLS
    PRINT "Arrange Patterns (current arrangement: "; currentSong.arrangeCount; " patterns)"
    FOR i = 1 TO currentSong.arrangeCount
        PRINT i; ": Pattern "; currentSong.arrangement(i); " (Track "; currentTrack; ")"
    NEXT
    INPUT "Add pattern number (0 to finish): ", p%
    IF p% = 0 THEN EXIT SUB
    IF p% >= 1 AND p% <= currentSong.tracks(currentTrack).patternCount THEN
        CALL SaveUndo
        currentSong.arrangeCount = currentSong.arrangeCount + 1
        currentSong.arrangement(currentSong.arrangeCount) = p%
        INPUT "Repeat count: ", r%
        IF r% >= 1 THEN currentSong.tracks(currentTrack).patterns(p%).repeatCount = r%
    END IF
END SUB

SUB ShowHelp
    CLS
    PRINT "QBasic Music Studio v5 - Reference"
    PRINT
    PRINT "Pitch Format:"
    PRINT "  A-G, optional # for sharp, octave 1-7 (e.g., C#4, R for rest)"
    PRINT
    PRINT "Frequencies (Hz):"
    PRINT "Octave 3: C=130.81, D=146.83, E=164.81, F=174.61, G=196.00, A=220.00, B=246.94"
    PRINT "Octave 4: C=261.63, D=293.66, E=329.63, F=349.23, G=392.00, A=440.00, B=493.88"
    PRINT "Octave 5: C=523.25, D=587.33, E=659.26, F=698.46, G=783.99, A=880.00, B=987.77"
    PRINT
    PRINT "Duration Symbols:"
    PRINT "  W=whole (4 beats), H=half (2 beats), Q=quarter (1 beat), E=eighth (0.5 beats)"
    PRINT
    PRINT "Instruments:"
    PRINT "  SINE, SQUARE, CHIP, WAV:filename, OGG:filename, MP3:filename, MOD:filename, SF:folder"
    PRINT
    PRINT "Example Sequence:"
    PRINT "  T120 O4 CDEFGAB > C"
    PRINT
    PRINT "Press any key to return..."
    SLEEP
END SUB

SUB LoadDemo
    CALL SaveUndo
    currentSong.tracks(currentTrack).patterns(currentPattern).count = 8
    currentSong.tempo = 120
    DIM demoNotes$(8)
    demoNotes$(1) = "C4": demoNotes$(2) = "D4": demoNotes$(3) = "E4": demoNotes$(4) = "F4"
    demoNotes$(5) = "G4": demoNotes$(6) = "A4": demoNotes$(7) = "B4": demoNotes$(8) = "C5"
    FOR i = 1 TO 8
        currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pitch = LEFT$(demoNotes$(i) + "   ", 3)
        currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).duration = 1
        currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).volume = 1
        currentSong.tracks(currentTrack).patterns(currentPattern).notes(i).pan = 0
    NEXT
    PRINT "Demo sequence loaded: T120 O4 CDEFGAB > C"
    SLEEP 2
END SUB

SUB ConvertMidiToQms(midi$, qms$)
    IF _FILEEXISTS(midi$) THEN
        OPEN midi$ FOR BINARY AS #1
        IF LOF(1) > 0 THEN
            DIM header AS STRING * 14
            GET #1, 1, header
            IF LEFT$(header, 4) <> "MThd" THEN
                CLOSE #1
                PRINT "Error: Invalid MIDI header."
                BEEP
                SLEEP 2
                EXIT SUB
            END IF
            formatType = CVI(MID$(header, 9, 2))
            trackCount = CVI(MID$(header, 11, 2))
            division = CVI(MID$(header, 13, 2))
            IF formatType > 1 OR trackCount > 4 THEN
                CLOSE #1
                PRINT "Error: Only MIDI format 0 or 1 with up to 4 tracks supported."
                BEEP
                SLEEP 2
                EXIT SUB
            END IF
            currentSong.trackCount = trackCount
            IF currentSong.trackCount = 0 THEN currentSong.trackCount = 1
            FOR t = 1 TO currentSong.trackCount
                currentSong.tracks(t).patternCount = 1
                currentSong.tracks(t).patterns(1).count = 0
                currentSong.tracks(t).patterns(1).repeatCount = 1
                currentSong.tracks(t).instrument = "SINE"
            NEXT
            FOR t = 1 TO currentSong.trackCount
                DIM chunk AS STRING * 8
                GET #1, , chunk
                IF LEFT$(chunk, 4) <> "MTrk" THEN
                    CLOSE #1
                    PRINT "Error: Invalid track chunk."
                    BEEP
                    SLEEP 2
                    EXIT SUB
                END IF
                trackLength = CVL(MID$(chunk, 5, 4))
                trackEnd = LOC(1) + trackLength
                noteCount = 0
                DIM noteStart(127) AS LONG
                DIM noteVelocity(127) AS INTEGER
                ticksPerBeat = division
                currentTempo = 500000
                runningStatus = 0
                currentTicks = 0
                WHILE LOC(1) < trackEnd AND noteCount < 1024
                    deltaTicks = ReadVariableLength
                    IF deltaTicks < 0 THEN EXIT WHILE
                    status = ReadByte
                    IF status = &HFF THEN
                        metaType = ReadByte
                        metaLength = ReadVariableLength
                        IF metaType = &H51 AND metaLength = 3 THEN
                            tempoBytes$ = SPACE$(3)
                            GET #1, , tempoBytes$
                            currentTempo = CVL(CHR$(0) + tempoBytes$)
                            currentSong.tempo = 60000000 / currentTempo
                        ELSEIF metaType = &H58 AND metaLength = 4 THEN
                            timeSig$ = SPACE$(4)
                            GET #1, , timeSig$
                            currentSong.timeSigN = ASC(LEFT$(timeSig$, 1))
                            currentSong.timeSigD = 2 ^ ASC(MID$(timeSig$, 2, 1))
                        ELSE
                            SKIP$ = SPACE$(metaLength)
                            GET #1, , SKIP$
                        END IF
                    ELSEIF (status AND &HF0) = &H90 OR (status AND &HF0) = &H80 THEN
                        IF status < &H80 THEN
                            noteNum = status
                            velocity = ReadByte
                            status = runningStatus
                        ELSE
                            noteNum = ReadByte
                            velocity = ReadByte
                            runningStatus = status
                        END IF
                        IF (status AND &HF0) = &H90 AND velocity > 0 THEN
                            noteStart(noteNum) = currentTicks
                            noteVelocity(noteNum) = velocity
                        ELSE
                            IF noteStart(noteNum) > 0 THEN
                                noteCount = noteCount + 1
                                durationTicks = currentTicks - noteStart(noteNum)
                                durationBeats = durationTicks / ticksPerBeat
                                IF durationBeats < 0.1 THEN durationBeats = 0.1
                                CALL MapMidiNote(noteNum, currentSong.tracks(t).patterns(1).notes(noteCount).pitch)
                                currentSong.tracks(t).patterns(1).notes(noteCount).duration = durationBeats
                                currentSong.tracks(t).patterns(1).notes(noteCount).volume = noteVelocity(noteNum) / 127
                                currentSong.tracks(t).patterns(1).notes(noteCount).pan = 0
                                noteStart(noteNum) = 0
                            END IF
                        END IF
                    ELSE
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
                currentSong.tracks(t).patterns(1).count = noteCount
            NEXT
            CALL SaveSong(qms$)
            CLOSE #1
            PRINT "Converted "; midi$; " to "; qms$
        ELSE
            CLOSE #1
            PRINT "Error: MIDI file is empty or corrupted."
            BEEP
        END IF
    ELSE
        PRINT "Error: MIDI file not found: "; midi$
        BEEP
    END IF
    SLEEP 2
END SUB

SUB SaveSong(qms$)
    IF RIGHT$(UCASE$(qms$), 4) <> ".QMS" THEN qms$ = qms$ + ".qms"
    OPEN qms$ FOR BINARY AS #2
    IF LOF(2) = 0 THEN
        PUT #2, , currentSong
        CLOSE #2
    ELSE
        CLOSE #2
        PRINT "Error: QMS file already exists or inaccessible."
        BEEP
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
    noteNames$(1) = "C ": noteNames$(2) = "C#": noteNames$(3) = "D "
    noteNames$(4) = "D#": noteNames$(5) = "E ": noteNames$(6) = "F "
    noteNames$(7) = "F#": noteNames$(8) = "G ":

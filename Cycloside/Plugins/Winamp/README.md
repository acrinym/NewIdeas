# Winamp Visualization Plugins for Cycloside

This directory contains Winamp visualization plugins that can be used with the Cycloside MP3 Player.

## How it works
1 MP3 Player plugin generates audio spectrum and waveform data
2. This data is sent to the Winamp Visual Host plugin via the PluginBus
3. The Winamp Visual Host loads Winamp visualization DLLs and passes the audio data to them
4. The visualization plugins render their effects in a separate window

## Adding Plugins

Place Winamp visualization DLLs in this directory. The plugins must export the `winampVisGetHeader` function and follow the Winamp visualization plugin specification.

## Building the Test Plugin

A sample test visualization plugin is included:1Make sure you have Visual Studio or Visual Studio Build Tools installed
2. Run `build_test_vis.bat` to compile the test plugin
3he resulting `TestVis.dll` will be automatically detected by the Winamp Visual Host

## Using Visualizations1art the MP3yer plugin
2. Load some MP3 files and start playing3 Click "Enable" in the Visualization section of the MP3 Player
4. If multiple plugins are found, a picker window will appear
5. Select a visualization plugin to start it
6. UseDisable" to stop the visualization or Toggle to switch it on/off

## Plugin Requirements

Winamp visualization plugins must:
- Export `winampVisGetHeader` function
- Implement the `winampVisModule` structure
- Provide `Init`, `Render`, `Quit`, and optionally `Config` functions
- Handle spectrum and waveform data in the expected format

## Troubleshooting

- If no plugins are found, check that DLLs are in this directory
- If plugins don't load, ensure they follow the Winamp specification
- Check the application log for error messages
- Make sure the MP3 Player is playing audio for visualizations to work 
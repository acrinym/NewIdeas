@echo off
echo Building Test Visualization Plugin...

REM Try to find cl.exe (Visual Studio compiler)
setCL_PATH=
for /ftokens=*" %%i in (where cl.exe2^>nul) do setCL_PATH=%%iif %CL_PATH%"==" (
    echo Error: cl.exe not found. Please install Visual Studio or Visual Studio Build Tools.
    echo You can also install the Windows SDK which includes the compiler.
    pause
    exit /b 1
)

echo Using compiler: %CL_PATH%

REM Compile the plugin
cl.exe /LD TestVis.cpp /Fe:TestVis.dll user32.lib gdi32.lib

if %ERRORLEVEL% EQU 0o Build successful! TestVis.dll created.
    echo You can now copy TestVis.dll to the Plugins\Winamp directory.
) else (
    echo Build failed!
)

pause 
@echo off
cls
echo.
echo  Select Framework to Run
echo =========================
echo.
echo  1) net8.0-windows (For running the full GUI application)
echo  2) net8.0         (For command-line tools or testing)
echo.
set /p choice="Enter your choice (1 or 2): "

if "%choice%"=="1" (
    echo.
    echo --- Running with net8.0-windows ---
    dotnet run --framework net8.0-windows
) else if "%choice%"=="2" (
    echo.
    echo --- Running with net8.0 ---
    dotnet run --framework net8.0
) else (
    echo Invalid choice.
)

echo.
pause
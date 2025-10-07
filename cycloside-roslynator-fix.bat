@echo off
setlocal
cd /d "%~dp0"

REM Save repository state
echo Saving current repository state...
git add -A
git commit -m "chore: save state before Roslynator pass"
git push

REM Roslynator operations
echo Running Roslynator analyze...
dotnet roslynator analyze CyclosideNextFeatures.sln --output cycloside-roslynator-analysis.sarif --verbosity diagnostic

echo Running Roslynator fix...
dotnet roslynator fix CyclosideNextFeatures.sln --apply --diagnostics IDE*,RCS*,CA* --verbosity diagnostic

REM Validate build
echo Validating build...
dotnet build CyclosideNextFeatures.sln
if errorlevel 1 (
  echo Build failed after Roslynator fixes.
  exit /b 1
)

REM Commit and push automated fixes
echo Committing automated fixes...
git add -A
git commit -m "chore: apply Roslynator automated fixes"
git push

echo Completed Roslynator analyze, fix, build, and push.
endlocal

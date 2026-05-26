@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-AutoBingSearch.ps1" -Setup
if errorlevel 1 (
  echo.
  echo AutoBingSearch install failed.
  pause
  exit /b 1
)
echo.
echo AutoBingSearch install finished.
pause

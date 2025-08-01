@echo off
echo Starting Keyboard Mouse Odometer in Debug Mode...
echo This will run with full logging and error reporting.
echo.
cd /d "%~dp0"
cd src\KeyboardMouseOdometer.UI
dotnet run
pause
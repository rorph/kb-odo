@echo off
echo Starting Keyboard Mouse Odometer in Test Mode...
echo This will show a simple dialog to confirm basic functionality.
echo.
cd /d "%~dp0"
cd src\KeyboardMouseOdometer.UI
dotnet run -- --test
pause
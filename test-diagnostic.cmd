@echo off
echo Starting Keyboard Mouse Odometer in Diagnostic Mode...
echo This will show detailed startup information and error logs.
echo.
cd /d "%~dp0"
cd src\KeyboardMouseOdometer.UI
dotnet run -- --diagnostic
pause
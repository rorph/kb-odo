@echo off
REM Build script for Keyboard Mouse Odometer (Windows)
REM This script builds the entire solution in Release mode

echo ======================================
echo Building Keyboard Mouse Odometer
echo ======================================

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean -c Release 2>NUL

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 goto :error

REM Build solution
echo Building solution in Release mode...
dotnet build -c Release
if %ERRORLEVEL% neq 0 goto :error

REM Run tests
echo Running tests...
dotnet test -c Release --no-build --verbosity normal
if %ERRORLEVEL% neq 0 goto :error

echo ======================================
echo Build completed successfully!
echo ======================================
echo.
echo Build output locations:
echo Core:  src\KeyboardMouseOdometer.Core\bin\Release\net8.0\
echo UI:    src\KeyboardMouseOdometer.UI\bin\Release\net8.0-windows\
echo Tests: src\KeyboardMouseOdometer.Tests\bin\Release\net8.0\
goto :eof

:error
echo Build failed!
exit /b 1
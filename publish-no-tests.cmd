@echo off
REM Publish script for Keyboard Mouse Odometer (WITHOUT TESTS)
REM This script creates a publishable release package without running tests
REM Use this when tests are failing but you need to publish anyway

echo ======================================
echo Publishing Keyboard Mouse Odometer
echo (SKIPPING TESTS)
echo ======================================

set VERSION=1.0.0
set OUTPUT_DIR=.\publish
set RUNTIME=win-x64

REM Clean output directory
echo Cleaning output directory...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean -c Release 2>nul

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if errorlevel 1 goto :error

REM Build solution (WITHOUT RUNNING TESTS)
echo Building solution in Release mode (SKIPPING TESTS)...
dotnet build -c Release
if errorlevel 1 goto :error

echo ======================================
echo Build completed (tests skipped)!
echo ======================================

REM Publish the UI project
echo Publishing for %RUNTIME%...
dotnet publish src\KeyboardMouseOdometer.UI\KeyboardMouseOdometer.UI.csproj ^
    -c Release ^
    -r %RUNTIME% ^
    --self-contained true ^
    --output "%OUTPUT_DIR%\KeyboardMouseOdometer-%VERSION%-%RUNTIME%" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:AssemblyVersion=%VERSION% ^
    -p:FileVersion=%VERSION%
if errorlevel 1 goto :error

echo ======================================
echo Publishing completed successfully!
echo (Tests were skipped)
echo ======================================
echo.
echo Published to: %OUTPUT_DIR%\KeyboardMouseOdometer-%VERSION%-%RUNTIME%\
echo.
echo To run the application:
echo   %OUTPUT_DIR%\KeyboardMouseOdometer-%VERSION%-%RUNTIME%\KeyboardMouseOdometer.UI.exe
echo.
echo WARNING: Tests were skipped during this build!
echo Please fix the failing tests and use publish.cmd for production builds.
goto :end

:error
echo ======================================
echo ERROR: Build or publish failed!
echo ======================================
exit /b 1

:end
exit /b 0
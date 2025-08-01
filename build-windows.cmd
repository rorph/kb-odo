@echo off
REM Enhanced Windows build script for Keyboard Mouse Odometer
REM Generates self-contained single-file .exe for Windows deployment

setlocal EnableDelayedExpansion

echo ==========================================
echo Keyboard Mouse Odometer - Windows Build
echo ==========================================

REM Configuration
set VERSION=1.0.0
set RUNTIME=win-x64
set CONFIGURATION=Release
set OUTPUT_DIR=publish\windows
set PROJECT_PATH=src\KeyboardMouseOdometer.UI\KeyboardMouseOdometer.UI.csproj

REM Parse command line arguments
:parse_args
if "%~1"=="" goto build_start
if "%~1"=="--version" (
    set VERSION=%~2
    shift
    shift
    goto parse_args
)
if "%~1"=="--runtime" (
    set RUNTIME=%~2
    shift
    shift
    goto parse_args
)
if "%~1"=="--help" (
    echo Usage: build-windows.cmd [--version ^<version^>] [--runtime ^<runtime^>]
    echo   --version: Version number (default: 1.0.0)
    echo   --runtime: Target runtime (default: win-x64, options: win-x64, win-x86, win-arm64)
    exit /b 0
)
shift
goto parse_args

:build_start
echo Configuration:
echo   Version: %VERSION%
echo   Runtime: %RUNTIME%
echo   Configuration: %CONFIGURATION%
echo   Output: %OUTPUT_DIR%
echo.

REM Clean previous builds
echo [1/6] Cleaning previous builds...
if exist %OUTPUT_DIR% rd /s /q %OUTPUT_DIR%
dotnet clean -c %CONFIGURATION% 2>NUL

REM Restore packages
echo [2/6] Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 goto error

REM Build solution
echo [3/6] Building solution...
dotnet build -c %CONFIGURATION%
if %ERRORLEVEL% neq 0 goto error

REM Run tests
echo [4/6] Running tests...
dotnet test -c %CONFIGURATION% --no-build --verbosity minimal
if %ERRORLEVEL% neq 0 goto error

REM Publish Windows executable
echo [5/6] Publishing Windows executable...
dotnet publish %PROJECT_PATH% ^
    -c %CONFIGURATION% ^
    -r %RUNTIME% ^
    --self-contained true ^
    --output %OUTPUT_DIR%\%RUNTIME% ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=false ^
    -p:PublishTrimmed=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:IncludeAllContentForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=embedded ^
    -p:DebugSymbols=false ^
    -p:AssemblyVersion=%VERSION% ^
    -p:FileVersion=%VERSION%

if %ERRORLEVEL% neq 0 goto error

REM Create distribution package
echo [6/6] Creating distribution package...
cd %OUTPUT_DIR%
powershell -Command "Compress-Archive -Path '%RUNTIME%' -DestinationPath 'KeyboardMouseOdometer-%VERSION%-%RUNTIME%.zip' -Force" 2>NUL
if %ERRORLEVEL% neq 0 (
    REM Fallback to 7zip or manual zip creation
    echo Warning: PowerShell compression failed, creating manual package...
    mkdir KeyboardMouseOdometer-%VERSION%-%RUNTIME%
    copy %RUNTIME%\*.* KeyboardMouseOdometer-%VERSION%-%RUNTIME%\ >NUL
)
cd ..\..

echo ==========================================
echo Build completed successfully!
echo ==========================================
echo.
echo Executable: %OUTPUT_DIR%\%RUNTIME%\KeyboardMouseOdometer.UI.exe
echo Package:    %OUTPUT_DIR%\KeyboardMouseOdometer-%VERSION%-%RUNTIME%.zip
echo Size:       
for %%F in ("%OUTPUT_DIR%\%RUNTIME%\KeyboardMouseOdometer.UI.exe") do echo   %%~zF bytes
echo.
echo To run the application:
echo   %OUTPUT_DIR%\%RUNTIME%\KeyboardMouseOdometer.UI.exe
echo.
echo To install on target machine:
echo   1. Extract the ZIP file
echo   2. Run KeyboardMouseOdometer.UI.exe
echo   3. No .NET runtime installation required
goto end

:error
echo.
echo ==========================================
echo Build failed!
echo ==========================================
echo Check the error messages above for details.
exit /b 1

:end
endlocal
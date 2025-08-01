@echo off
REM Simple .exe generator for Keyboard Mouse Odometer
REM Generates KeyboardMouseOdometer.UI.exe directly in the bin\Release directory for development

setlocal EnableDelayedExpansion

echo ========================================
echo Keyboard Mouse Odometer - Quick EXE Build
echo ========================================

REM Configuration
set CONFIGURATION=Release
set RUNTIME=win-x64
set PROJECT_PATH=src\KeyboardMouseOdometer.UI\KeyboardMouseOdometer.UI.csproj
set BIN_DIR=src\KeyboardMouseOdometer.UI\bin\%CONFIGURATION%\net8.0-windows\%RUNTIME%

REM Create bin directory if it doesn't exist
if not exist "%BIN_DIR%" mkdir "%BIN_DIR%"

echo Building .exe in: %BIN_DIR%
echo.

REM Quick build with minimal output
echo [1/3] Restoring packages...
dotnet restore --verbosity quiet
if %ERRORLEVEL% neq 0 goto error

echo [2/3] Building for Windows...
dotnet build "%PROJECT_PATH%" ^
    -c "%CONFIGURATION%" ^
    -r "%RUNTIME%" ^
    --self-contained true ^
    --verbosity quiet ^
    -p:PublishSingleFile=false
if %ERRORLEVEL% neq 0 goto error

echo [3/3] Publishing single .exe file...
dotnet publish "%PROJECT_PATH%" ^
    -c "%CONFIGURATION%" ^
    -r "%RUNTIME%" ^
    --self-contained true ^
    --output "%BIN_DIR%" ^
    --verbosity quiet ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=false ^
    -p:PublishTrimmed=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:IncludeAllContentForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=embedded ^
    -p:DebugSymbols=false
if %ERRORLEVEL% neq 0 goto error

echo.
echo ========================================
echo Build completed successfully!
echo ========================================

REM Check if .exe was created
if exist "%BIN_DIR%\KeyboardMouseOdometer.UI.exe" (
    echo Executable: %BIN_DIR%\KeyboardMouseOdometer.UI.exe
    for %%F in ("%BIN_DIR%\KeyboardMouseOdometer.UI.exe") do echo Size:       %%~zF bytes
    
    REM Show full path
    echo Full path:  %CD%\%BIN_DIR%\KeyboardMouseOdometer.UI.exe
) else (
    echo ERROR: KeyboardMouseOdometer.UI.exe was not created!
    goto error
)

echo.
echo To run the application:
echo   %BIN_DIR%\KeyboardMouseOdometer.UI.exe
echo.
echo Or run directly (no .NET runtime required)
goto end

:error
echo.
echo ========================================
echo Build failed!
echo ========================================
echo Check the error messages above for details.
exit /b 1

:end
endlocal
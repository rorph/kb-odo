#!/bin/bash

# Simple .exe generator for Keyboard Mouse Odometer
# Generates KeyboardMouseOdometer.UI.exe directly in the bin/Release directory for development

set -e  # Exit on error

echo "========================================"
echo "Keyboard Mouse Odometer - Quick EXE Build"
echo "========================================"

# Configuration
CONFIGURATION="Release"
RUNTIME="win-x64"
PROJECT_PATH="src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj"
BIN_DIR="src/KeyboardMouseOdometer.UI/bin/$CONFIGURATION/net8.0-windows/$RUNTIME"

# Create bin directory if it doesn't exist
mkdir -p "$BIN_DIR"

echo "Building .exe in: $BIN_DIR"
echo ""

# Quick build with minimal output
echo "[1/3] Restoring packages..."
dotnet restore --verbosity quiet

echo "[2/3] Building for Windows..."
dotnet build "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained true \
    --verbosity quiet \
    -p:PublishSingleFile=false

echo "[3/3] Publishing single .exe file..."
dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained true \
    --output "$BIN_DIR" \
    --verbosity quiet \
    -p:PublishSingleFile=true \
    -p:PublishReadyToRun=false \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=embedded \
    -p:DebugSymbols=false

echo ""
echo "========================================"
echo "Build completed successfully!"
echo "========================================"

# Check if .exe was created
if [ -f "$BIN_DIR/KeyboardMouseOdometer.UI.exe" ]; then
    SIZE=$(stat -c%s "$BIN_DIR/KeyboardMouseOdometer.UI.exe" 2>/dev/null || echo "unknown")
    echo "Executable: $BIN_DIR/KeyboardMouseOdometer.UI.exe"
    echo "Size:       $SIZE bytes"
    
    # Make the script show the full absolute path
    FULL_PATH=$(realpath "$BIN_DIR/KeyboardMouseOdometer.UI.exe")
    echo "Full path:  $FULL_PATH"
else
    echo "ERROR: KeyboardMouseOdometer.UI.exe was not created!"
    exit 1
fi

echo ""
echo "To run the application:"
echo "  ./$BIN_DIR/KeyboardMouseOdometer.UI.exe"
echo ""
echo "Or copy to Windows machine and run directly (no .NET runtime required)"
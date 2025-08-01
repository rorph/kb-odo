#!/bin/bash

# Build script for Debug version with enhanced logging

echo "Building Keyboard Mouse Odometer DEBUG Executable..."
echo "============================================"

# Build in Debug mode
echo "[1/3] Building in Debug mode..."
dotnet build -c Debug src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj

# Publish as single file exe with Debug configuration
echo "[2/3] Creating Windows DEBUG executable..."
dotnet publish src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj \
    -c Debug \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o ./output-debug

echo "[3/3] Creating console output version..."
# Also create a version that shows console output
dotnet publish src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj \
    -c Debug \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:OutputType=Exe \
    -o ./output-debug-console

echo ""
echo "============================================"
echo "Debug builds completed!"
echo ""
echo "Debug executable (no console): ./output-debug/KeyboardMouseOdometer.UI.exe"
echo "Debug executable (with console): ./output-debug-console/KeyboardMouseOdometer.UI.exe"
echo ""
echo "The debug version includes:"
echo "- Enhanced logging for keyboard/mouse events"
echo "- Configuration save/load debugging"
echo "- Detailed error messages"
echo ""
echo "Run with console version to see real-time debug output!"
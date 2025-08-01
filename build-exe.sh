#!/bin/bash

# Simple script to build Windows executable with XAML fixes

echo "Building Keyboard Mouse Odometer Windows Executable..."
echo "============================================"

# Clean previous builds
echo "[1/4] Cleaning..."
dotnet clean -c Release src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj >/dev/null 2>&1

# Restore packages
echo "[2/4] Restoring packages..."
dotnet restore src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj

# Build
echo "[3/4] Building..."
dotnet build -c Release src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj

# Publish as single file exe
echo "[4/4] Creating Windows executable..."
dotnet publish src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o ./output

echo ""
echo "============================================"
echo "Build completed!"
echo "Executable: ./output/KeyboardMouseOdometer.UI.exe"
echo "Size: $(ls -lh ./output/KeyboardMouseOdometer.UI.exe | awk '{print $5}')"
echo ""
echo "The executable is self-contained and can run on any Windows 10/11 x64 machine."
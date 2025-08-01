#!/bin/bash

# Cross-platform Windows build script for Keyboard Mouse Odometer
# Can be run on Linux to generate Windows .exe files

set -e  # Exit on error

echo "=========================================="
echo "Keyboard Mouse Odometer - Windows Build"
echo "=========================================="

# Configuration
VERSION="1.0.0"
RUNTIME="win-x64"
CONFIGURATION="Release"
OUTPUT_DIR="publish/windows"
PROJECT_PATH="src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --help)
            echo "Usage: ./build-windows.sh [--version <version>] [--runtime <runtime>]"
            echo "  --version: Version number (default: 1.0.0)"
            echo "  --runtime: Target runtime (default: win-x64)"
            echo "             Options: win-x64, win-x86, win-arm64"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "Configuration:"
echo "  Version: $VERSION"
echo "  Runtime: $RUNTIME"
echo "  Configuration: $CONFIGURATION"
echo "  Output: $OUTPUT_DIR"
echo ""

# Clean previous builds
echo "[1/6] Cleaning previous builds..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
dotnet clean -c "$CONFIGURATION" >/dev/null 2>&1 || true

# Restore packages
echo "[2/6] Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "Failed to restore packages"
    exit 1
fi

# Build solution
echo "[3/6] Building solution..."
dotnet build -c "$CONFIGURATION"
if [ $? -ne 0 ]; then
    echo "Build failed"
    exit 1
fi

# Run tests
echo "[4/6] Running tests..."
dotnet test -c "$CONFIGURATION" --no-build --verbosity minimal
if [ $? -ne 0 ]; then
    echo "Tests failed"
    exit 1
fi

# Publish Windows executable
echo "[5/6] Publishing Windows executable..."
dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained true \
    --output "$OUTPUT_DIR/$RUNTIME" \
    -p:PublishSingleFile=true \
    -p:PublishReadyToRun=false \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=embedded \
    -p:DebugSymbols=false \
    -p:AssemblyVersion="$VERSION" \
    -p:FileVersion="$VERSION"

if [ $? -ne 0 ]; then
    echo "Publish failed"
    exit 1
fi

# Create distribution package
echo "[6/6] Creating distribution package..."
cd "$OUTPUT_DIR"
if command -v zip >/dev/null 2>&1; then
    zip -r "KeyboardMouseOdometer-$VERSION-$RUNTIME.zip" "$RUNTIME/"
else
    tar -czf "KeyboardMouseOdometer-$VERSION-$RUNTIME.tar.gz" "$RUNTIME/"
    echo "Note: Created .tar.gz archive (zip not available)"
fi
cd ../..

echo "=========================================="
echo "Build completed successfully!"
echo "=========================================="
echo ""
echo "Executable: $OUTPUT_DIR/$RUNTIME/KeyboardMouseOdometer.UI.exe"
if [ -f "$OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME.zip" ]; then
    echo "Package:    $OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME.zip"
else
    echo "Package:    $OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME.tar.gz"
fi

if [ -f "$OUTPUT_DIR/$RUNTIME/KeyboardMouseOdometer.UI.exe" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/$RUNTIME/KeyboardMouseOdometer.UI.exe" 2>/dev/null || stat -c%s "$OUTPUT_DIR/$RUNTIME/KeyboardMouseOdometer.UI.exe" 2>/dev/null || echo "unknown")
    echo "Size:       $SIZE bytes"
fi

echo ""
echo "To run the application:"
echo "  $OUTPUT_DIR/$RUNTIME/KeyboardMouseOdometer.UI.exe"
echo ""
echo "To install on target machine:"
echo "  1. Extract the archive"
echo "  2. Run KeyboardMouseOdometer.UI.exe"
echo "  3. No .NET runtime installation required"
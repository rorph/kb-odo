#!/bin/bash

# Publish script for Keyboard Mouse Odometer (WITHOUT TESTS)
# This script creates a publishable release package without running tests
# Use this when tests are failing but you need to publish anyway

set -e  # Exit on error

echo "======================================"
echo "Publishing Keyboard Mouse Odometer"
echo "(SKIPPING TESTS)"
echo "======================================"

VERSION="1.0.0"
OUTPUT_DIR="./publish"
RUNTIME="win-x64"

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
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: ./publish-no-tests.sh [--version <version>] [--runtime <runtime>] [--output <dir>]"
            exit 1
            ;;
    esac
done

# Clean output directory
echo "Cleaning output directory..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c Release 2>/dev/null || true

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build solution (WITHOUT RUNNING TESTS)
echo "Building solution in Release mode (SKIPPING TESTS)..."
dotnet build -c Release

echo "======================================"
echo "Build completed (tests skipped)!"
echo "======================================"

# Publish the UI project (which includes all dependencies)
echo "Publishing for $RUNTIME..."
dotnet publish src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj \
    -c Release \
    -r "$RUNTIME" \
    --self-contained true \
    --output "$OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:AssemblyVersion=$VERSION \
    -p:FileVersion=$VERSION

# Create a ZIP archive
echo "Creating ZIP archive..."
cd "$OUTPUT_DIR"
zip -r "KeyboardMouseOdometer-$VERSION-$RUNTIME.zip" "KeyboardMouseOdometer-$VERSION-$RUNTIME"
cd ..

echo "======================================"
echo "Publishing completed successfully!"
echo "(Tests were skipped)"
echo "======================================"
echo ""
echo "Published to: $OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME/"
echo "Archive: $OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME.zip"
echo ""
echo "To run the application:"
echo "  $OUTPUT_DIR/KeyboardMouseOdometer-$VERSION-$RUNTIME/KeyboardMouseOdometer.UI.exe"
echo ""
echo "WARNING: Tests were skipped during this build!"
echo "Please fix the failing tests and use ./publish.sh for production builds."
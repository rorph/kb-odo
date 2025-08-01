#!/bin/bash

# Build script for Keyboard Mouse Odometer
# This script builds the entire solution in Release mode

set -e  # Exit on error

echo "======================================"
echo "Building Keyboard Mouse Odometer"
echo "======================================"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c Release 2>/dev/null || true

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build solution
echo "Building solution in Release mode..."
dotnet build -c Release

# Run tests
echo "Running tests..."
dotnet test -c Release --no-build --verbosity normal

echo "======================================"
echo "Build completed successfully!"
echo "======================================"

# Show output location
echo ""
echo "Build output locations:"
echo "Core:  src/KeyboardMouseOdometer.Core/bin/Release/net8.0/"
echo "UI:    src/KeyboardMouseOdometer.UI/bin/Release/net8.0-windows/"
echo "Tests: src/KeyboardMouseOdometer.Tests/bin/Release/net8.0/"
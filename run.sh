#!/bin/bash

# Run script for Keyboard Mouse Odometer
# This script runs the application in development mode

set -e  # Exit on error

echo "======================================"
echo "Running Keyboard Mouse Odometer"
echo "======================================"

# Check if we're on Windows
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Running on Windows..."
else
    echo "Warning: This application is designed for Windows."
    echo "Some features may not work correctly on other platforms."
    echo ""
fi

# Build first
echo "Building application..."
dotnet build -c Debug src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj

# Run the application
echo "Starting application..."
dotnet run --project src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj -c Debug
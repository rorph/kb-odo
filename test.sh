#!/bin/bash

# Test script for Keyboard Mouse Odometer
# This script runs all tests with various options

set -e  # Exit on error

echo "======================================"
echo "Running Keyboard Mouse Odometer Tests"
echo "======================================"

# Parse command line arguments
COVERAGE=false
VERBOSE=false
FILTER=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --coverage)
            COVERAGE=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: ./test.sh [--coverage] [--verbose] [--filter <test-filter>]"
            exit 1
            ;;
    esac
done

# Build in Debug mode for better test diagnostics
echo "Building solution in Debug mode..."
dotnet build -c Debug

# Prepare test command
TEST_CMD="dotnet test -c Debug --no-build"

if [ "$VERBOSE" = true ]; then
    TEST_CMD="$TEST_CMD --verbosity detailed"
else
    TEST_CMD="$TEST_CMD --verbosity normal"
fi

if [ -n "$FILTER" ]; then
    TEST_CMD="$TEST_CMD --filter \"$FILTER\""
fi

if [ "$COVERAGE" = true ]; then
    echo "Running tests with code coverage..."
    TEST_CMD="$TEST_CMD --collect:\"XPlat Code Coverage\" --results-directory ./TestResults"
else
    echo "Running tests..."
fi

# Run tests
eval $TEST_CMD

if [ "$COVERAGE" = true ]; then
    echo ""
    echo "Code coverage results saved to ./TestResults/"
    echo "To view coverage report, install and run:"
    echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
    echo "  reportgenerator -reports:./TestResults/*/coverage.cobertura.xml -targetdir:./TestResults/CoverageReport -reporttypes:Html"
fi

echo ""
echo "======================================"
echo "All tests completed!"
echo "======================================" 
#!/bin/bash

# Check script to verify build readiness
# This script checks for common build issues without requiring dotnet

set -e

echo "======================================"
echo "Checking Build Readiness"
echo "======================================"

ERROR_COUNT=0
WARNING_COUNT=0

# Function to report errors
report_error() {
    echo "❌ ERROR: $1"
    ((ERROR_COUNT++))
}

# Function to report warnings
report_warning() {
    echo "⚠️  WARNING: $1"
    ((WARNING_COUNT++))
}

# Function to report success
report_success() {
    echo "✅ OK: $1"
}

echo ""
echo "Checking for WPF dependencies in Core project..."
echo "================================================"

# Check for System.Windows references in Core project
if grep -r "using System.Windows" src/KeyboardMouseOdometer.Core/ --include="*.cs" 2>/dev/null | grep -v "^Binary file"; then
    report_error "Found System.Windows references in Core project"
else
    report_success "No System.Windows references in Core project"
fi

# Check for WPF-specific types in Core project
if grep -r "System.Windows.Input.Key" src/KeyboardMouseOdometer.Core/ --include="*.cs" 2>/dev/null; then
    report_error "Found System.Windows.Input.Key references in Core project"
else
    report_success "No System.Windows.Input.Key references in Core project"
fi

echo ""
echo "Checking for required files..."
echo "=============================="

# Check for new required files
REQUIRED_FILES=(
    "src/KeyboardMouseOdometer.Core/Models/CoreKeyCode.cs"
    "src/KeyboardMouseOdometer.Core/Interfaces/IKeyCodeMapper.cs"
    "src/KeyboardMouseOdometer.Core/Utils/KeyCodeMapper.cs"
    "src/KeyboardMouseOdometer.Core/Models/KeyboardKey.cs"
    "src/KeyboardMouseOdometer.Core/Models/KeyboardLayout.cs"
    "src/KeyboardMouseOdometer.UI/Services/WpfKeyCodeMapper.cs"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        report_success "Found: $file"
    else
        report_error "Missing: $file"
    fi
done

echo ""
echo "Checking for dependency injection setup..."
echo "========================================="

# Check if IKeyCodeMapper is registered in App.xaml.cs
if grep -q "IKeyCodeMapper" src/KeyboardMouseOdometer.UI/App.xaml.cs; then
    report_success "IKeyCodeMapper is registered in dependency injection"
else
    report_error "IKeyCodeMapper is NOT registered in App.xaml.cs"
fi

echo ""
echo "Checking for test files..."
echo "========================="

TEST_FILES=(
    "src/KeyboardMouseOdometer.Tests/Utils/KeyCodeMapperTests.cs"
    "src/KeyboardMouseOdometer.Tests/Services/DatabaseMigrationTests.cs"
)

for file in "${TEST_FILES[@]}"; do
    if [ -f "$file" ]; then
        report_success "Found test: $file"
    else
        report_warning "Missing test: $file"
    fi
done

echo ""
echo "Checking for compilation issues..."
echo "================================="

# Check for common C# syntax errors
echo "Checking for unclosed braces..."
for dir in src/KeyboardMouseOdometer.Core src/KeyboardMouseOdometer.UI; do
    for file in $(find "$dir" -name "*.cs" -type f 2>/dev/null); do
        OPEN_BRACES=$(grep -o "{" "$file" 2>/dev/null | wc -l)
        CLOSE_BRACES=$(grep -o "}" "$file" 2>/dev/null | wc -l)
        if [ "$OPEN_BRACES" -ne "$CLOSE_BRACES" ]; then
            report_error "Mismatched braces in $file (Open: $OPEN_BRACES, Close: $CLOSE_BRACES)"
        fi
    done
done

echo ""
echo "Checking project references..."
echo "============================="

# Check Core project doesn't reference WPF
if grep -q "net8.0-windows" src/KeyboardMouseOdometer.Core/KeyboardMouseOdometer.Core.csproj; then
    report_error "Core project targets Windows-specific framework"
else
    report_success "Core project targets cross-platform framework"
fi

# Check UI project references Core
if grep -q "KeyboardMouseOdometer.Core" src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj; then
    report_success "UI project references Core project"
else
    report_error "UI project does NOT reference Core project"
fi

echo ""
echo "Checking database migration..."
echo "============================="

# Check for database versioning
if grep -q "schema_version" src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs; then
    report_success "Database versioning is implemented"
else
    report_error "Database versioning is NOT implemented"
fi

# Check for key_stats table creation
if grep -q "CREATE TABLE.*key_stats" src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs; then
    report_success "key_stats table creation is implemented"
else
    report_error "key_stats table creation is NOT found"
fi

echo ""
echo "======================================"
echo "Build Readiness Check Complete"
echo "======================================"
echo ""

if [ $ERROR_COUNT -eq 0 ]; then
    echo "✅ SUCCESS: No errors found! Project should build successfully."
    echo ""
    if [ $WARNING_COUNT -gt 0 ]; then
        echo "⚠️  Found $WARNING_COUNT warning(s) - these won't prevent building but should be addressed."
    fi
    echo ""
    echo "To build the project, run:"
    echo "  ./build.sh"
    echo ""
    echo "To publish a release, run:"
    echo "  ./publish.sh"
    exit 0
else
    echo "❌ FAILED: Found $ERROR_COUNT error(s) that will prevent building."
    echo ""
    echo "Please fix the errors above before attempting to build."
    exit 1
fi
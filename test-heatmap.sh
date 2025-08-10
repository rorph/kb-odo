#!/bin/bash

echo "========================================"
echo "Testing Keyboard Heatmap Feature"
echo "========================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

# Function to check if file exists
check_file() {
    if [ -f "$1" ]; then
        echo -e "${GREEN}✓${NC} File exists: $1"
        ((TESTS_PASSED++))
    else
        echo -e "${RED}✗${NC} File missing: $1"
        ((TESTS_FAILED++))
    fi
}

# Function to check if pattern exists in file
check_pattern() {
    if grep -q "$2" "$1" 2>/dev/null; then
        echo -e "${GREEN}✓${NC} Pattern found in $1: $2"
        ((TESTS_PASSED++))
    else
        echo -e "${RED}✗${NC} Pattern not found in $1: $2"
        ((TESTS_FAILED++))
    fi
}

echo ""
echo "1. Checking Core Models..."
echo "------------------------"
check_file "src/KeyboardMouseOdometer.Core/Models/KeyboardKey.cs"
check_file "src/KeyboardMouseOdometer.Core/Models/KeyboardLayout.cs"
check_file "src/KeyboardMouseOdometer.Core/Models/CoreKeyCode.cs"
check_file "src/KeyboardMouseOdometer.Core/Models/HeatmapColor.cs"

echo ""
echo "2. Checking Key Code Mapping..."
echo "------------------------"
check_file "src/KeyboardMouseOdometer.Core/Interfaces/IKeyCodeMapper.cs"
check_file "src/KeyboardMouseOdometer.Core/Utils/KeyCodeMapper.cs"
check_file "src/KeyboardMouseOdometer.UI/Services/WpfKeyCodeMapper.cs"

echo ""
echo "3. Checking Database Integration..."
echo "------------------------"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs" "key_stats"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs" "GetTodayKeyStatsAsync"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs" "GetWeeklyKeyStatsAsync"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs" "GetMonthlyKeyStatsAsync"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs" "GetLifetimeKeyStatsAsync"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs" "schema_version"

echo ""
echo "4. Checking UI Components..."
echo "------------------------"
check_file "src/KeyboardMouseOdometer.UI/Controls/KeyboardHeatmapControl.xaml"
check_file "src/KeyboardMouseOdometer.UI/Controls/KeyboardHeatmapControl.xaml.cs"
check_file "src/KeyboardMouseOdometer.UI/Views/HeatmapView.xaml"
check_file "src/KeyboardMouseOdometer.UI/Views/HeatmapView.xaml.cs"
check_file "src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs"

echo ""
echo "5. Checking MainWindow Integration..."
echo "------------------------"
check_pattern "src/KeyboardMouseOdometer.UI/Views/MainWindow.xaml" "Heatmap"
check_pattern "src/KeyboardMouseOdometer.UI/ViewModels/MainWindowViewModel.cs" "HeatmapViewModel"
check_pattern "src/KeyboardMouseOdometer.UI/App.xaml.cs" "HeatmapViewModel"

echo ""
echo "6. Checking Converters..."
echo "------------------------"
check_file "src/KeyboardMouseOdometer.UI/Converters/EnumBooleanConverter.cs"
check_file "src/KeyboardMouseOdometer.UI/Converters/GreaterThanZeroConverter.cs"

echo ""
echo "7. Checking Styles and Resources..."
echo "------------------------"
check_pattern "src/KeyboardMouseOdometer.UI/App.xaml" "TimeRangeRadioButton"
check_pattern "src/KeyboardMouseOdometer.UI/App.xaml" "KeyButtonStyle"
check_pattern "src/KeyboardMouseOdometer.UI/App.xaml" "ModernButton"
check_pattern "src/KeyboardMouseOdometer.UI/App.xaml" "EnumBooleanConverter"

echo ""
echo "8. Checking Statistics Service..."
echo "------------------------"
check_pattern "src/KeyboardMouseOdometer.Core/Services/StatisticsService.cs" "CalculateHeatmapData"
check_pattern "src/KeyboardMouseOdometer.Core/Services/StatisticsService.cs" "CalculateTypingSpeed"

echo ""
echo "9. Checking Input Monitoring..."
echo "------------------------"
check_pattern "src/KeyboardMouseOdometer.Core/Services/InputMonitoringService.cs" "KeyIdentifier"
check_pattern "src/KeyboardMouseOdometer.Core/Services/DataLoggerService.cs" "_currentHourKeyStats"

echo ""
echo "10. Checking Unit Tests..."
echo "------------------------"
check_file "src/KeyboardMouseOdometer.Tests/Services/HeatmapCalculationTests.cs"
check_file "src/KeyboardMouseOdometer.Tests/Integration/KeyCaptureIntegrationTests.cs"
check_file "src/KeyboardMouseOdometer.Tests/Utils/KeyCodeMapperTests.cs"

echo ""
echo "11. Checking for WPF Dependencies in Core..."
echo "------------------------"
# This should NOT find System.Windows in Core project
if grep -r "System.Windows" src/KeyboardMouseOdometer.Core/*.cs 2>/dev/null | grep -v "// " | grep -v "/// "; then
    echo -e "${RED}✗${NC} Found WPF dependencies in Core project!"
    ((TESTS_FAILED++))
else
    echo -e "${GREEN}✓${NC} No WPF dependencies in Core project"
    ((TESTS_PASSED++))
fi

echo ""
echo "12. Checking CHANGELOG..."
echo "------------------------"
check_file "CHANGELOG.md"
check_pattern "CHANGELOG.md" "Keyboard Heatmap"

echo ""
echo "========================================"
echo "Test Results"
echo "========================================"
echo -e "Tests Passed: ${GREEN}$TESTS_PASSED${NC}"
echo -e "Tests Failed: ${RED}$TESTS_FAILED${NC}"

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "\n${GREEN}✓ All tests passed! Heatmap feature appears to be fully implemented.${NC}"
    exit 0
else
    echo -e "\n${YELLOW}⚠ Some tests failed. Please review the implementation.${NC}"
    exit 1
fi
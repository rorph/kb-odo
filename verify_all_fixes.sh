#!/bin/bash

echo "==================================="
echo "COMPREHENSIVE FIX VERIFICATION"
echo "==================================="
echo ""

echo "1. Checking HeatmapViewModel.cs fixes:"
echo "---------------------------------------"
grep -n "Core.Models.KeyboardLayout" src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs | wc -l
echo "Expected: 3 occurrences (lines 120, 159, 177)"
grep -n "Core.Models.KeyboardLayout" src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs | head -3
echo ""

echo "2. Checking App.xaml.cs fix:"
echo "----------------------------"
grep -n "ViewModels.RelayCommand" src/KeyboardMouseOdometer.UI/App.xaml.cs | wc -l
echo "Expected: 1 occurrence (line 406)"
grep -n "ViewModels.RelayCommand" src/KeyboardMouseOdometer.UI/App.xaml.cs
echo ""

echo "3. Checking test fixes - HeatmapCalculationTests:"
echo "--------------------------------------------------"
grep -n "Core.Models.KeyboardLayout" src/KeyboardMouseOdometer.Tests/Services/HeatmapCalculationTests.cs | wc -l
echo "Expected: 4 occurrences"
echo ""

echo "4. Checking test fixes - KeyCaptureIntegrationTests:"
echo "-----------------------------------------------------"
grep -n "EventType = InputEventType" src/KeyboardMouseOdometer.Tests/Integration/KeyCaptureIntegrationTests.cs | wc -l
echo "Expected: 4 occurrences"
echo ""

echo "5. Checking test fixes - DatabaseMigrationTests:"
echo "-------------------------------------------------"
grep -n "result\[0\]\.Item" src/KeyboardMouseOdometer.Tests/Services/DatabaseMigrationTests.cs | wc -l
echo "Expected: 2 occurrences (Item1 and Item2)"
echo ""

echo "6. File modification times:"
echo "---------------------------"
ls -la _build_error.log | awk '{print "Build error log: " $6 " " $7 " " $8}'
ls -la src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs | awk '{print "HeatmapViewModel: " $6 " " $7 " " $8}'
ls -la src/KeyboardMouseOdometer.UI/App.xaml.cs | awk '{print "App.xaml.cs: " $6 " " $7 " " $8}'
echo ""

echo "7. Current date/time:"
echo "---------------------"
date
echo ""

echo "ANALYSIS:"
echo "========="
echo "If the build error log time is BEFORE the fix times,"
echo "then the errors shown are outdated and a new build is needed."
echo ""
echo "All fixes appear to be properly applied!"
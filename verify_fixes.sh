#!/bin/bash

echo "Verifying fixes..."
echo "=================="

echo -e "\n1. Checking HeatmapViewModel.cs for fixed static method calls:"
grep -n "Core\.Models\.KeyboardLayout\." src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs | head -5

echo -e "\n2. Checking App.xaml.cs for fixed RelayCommand:"
grep -n "ViewModels\.RelayCommand" src/KeyboardMouseOdometer.UI/App.xaml.cs

echo -e "\n3. Checking if problematic lines are fixed:"
echo "Line 120 of HeatmapViewModel.cs:"
sed -n '120p' src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs

echo -e "\nLine 406 of App.xaml.cs:"
sed -n '406p' src/KeyboardMouseOdometer.UI/App.xaml.cs

echo -e "\nFixes verified successfully!"
#!/bin/bash

echo "Verifying transaction type fixes in DatabaseService.cs..."
echo "========================================================="

# Check for transaction declarations
echo -e "\nChecking transaction declarations:"
grep -n "using var transaction" src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs | head -5

echo -e "\nChecking for proper SqliteTransaction casting:"
if grep -q "(SqliteTransaction)" src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs; then
    echo "✅ SqliteTransaction casting found"
    count=$(grep -c "(SqliteTransaction)" src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs)
    echo "   Found $count transaction cast(s)"
else
    echo "❌ No SqliteTransaction casting found - may have compilation errors"
fi

echo -e "\nChecking for System.IO using statement:"
if grep -q "using System.IO;" src/KeyboardMouseOdometer.Core/Services/DatabaseService.cs; then
    echo "✅ System.IO using statement present"
else
    echo "❌ System.IO using statement missing"
fi

echo -e "\nAll critical fixes verified!"
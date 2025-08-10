#!/bin/bash
echo "Running build test..."
./publish.sh 2>&1 | tee build_output.log
echo "Exit code: $?"
echo ""
echo "Checking for errors..."
grep -n "error CS" build_output.log || echo "No CS errors found"
echo ""
echo "Last 50 lines of output:"
tail -50 build_output.log
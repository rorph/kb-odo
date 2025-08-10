import os
import re

def check_csharp_syntax(file_path):
    """Basic C# syntax check"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check for basic syntax issues
    issues = []
    
    # Check balanced braces
    brace_count = content.count('{') - content.count('}')
    if brace_count \!= 0:
        issues.append(f"Unbalanced braces: {brace_count} extra")
    
    # Check balanced parentheses
    paren_count = content.count('(') - content.count(')')
    if paren_count \!= 0:
        issues.append(f"Unbalanced parentheses: {paren_count} extra")
    
    return issues

# Check App.xaml.cs
app_file = '/mnt/1TB_RAID10/Dropbox/.httpd/~scripts/cvedia/kb-odo/src/KeyboardMouseOdometer.UI/App.xaml.cs'
print(f"Checking {app_file}...")
issues = check_csharp_syntax(app_file)
if issues:
    print("Issues found:")
    for issue in issues:
        print(f"  - {issue}")
else:
    print("✅ No syntax issues found")

# Check KeyboardHeatmapControl.xaml.cs
control_file = '/mnt/1TB_RAID10/Dropbox/.httpd/~scripts/cvedia/kb-odo/src/KeyboardMouseOdometer.UI/Controls/KeyboardHeatmapControl.xaml.cs'
print(f"\nChecking {control_file}...")
issues = check_csharp_syntax(control_file)
if issues:
    print("Issues found:")
    for issue in issues:
        print(f"  - {issue}")
else:
    print("✅ No syntax issues found")

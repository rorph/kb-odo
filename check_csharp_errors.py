#!/usr/bin/env python3
import os
import re
import glob

def check_csharp_file(filepath):
    """Check a C# file for common compilation errors"""
    errors = []
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
        lines = content.split('\n')
    
    # Check for basic syntax issues
    open_braces = content.count('{')
    close_braces = content.count('}')
    if open_braces != close_braces:
        errors.append(f"Brace mismatch: {open_braces} open, {close_braces} close")
    
    # Check for async/await issues
    async_methods = re.findall(r'async\s+(?:Task|ValueTask|void)\s+(\w+)', content)
    for method in async_methods:
        # Check if async method has await
        method_pattern = rf'async[^{{]*{method}[^{{]*{{([^}}]*)}}' 
        match = re.search(method_pattern, content, re.DOTALL)
        if match and 'await' not in match.group(1):
            errors.append(f"Async method '{method}' may not have await")
    
    # Check for transaction usage
    if 'BeginTransactionAsync' in content:
        # Check if transaction is properly used
        transaction_blocks = re.findall(r'using\s+var\s+transaction\s*=.*?BeginTransactionAsync.*?(?:CommitAsync|RollbackAsync)', content, re.DOTALL)
        if not transaction_blocks and 'transaction' in content:
            errors.append("Transaction may not be properly committed or rolled back")
    
    # Check for missing using statements
    if 'SqliteConnection' in content and 'using Microsoft.Data.Sqlite;' not in content:
        errors.append("Missing: using Microsoft.Data.Sqlite;")
    
    if 'Directory.CreateDirectory' in content and 'using System.IO;' not in content and 'System.IO.Directory' not in content:
        errors.append("Missing: using System.IO;")
    
    if 'Path.' in content and 'using System.IO;' not in content and 'System.IO.Path' not in content:
        errors.append("Missing: using System.IO;")
    
    # Check for GetAwaiter().GetResult() issues
    if '.GetAwaiter().GetResult()' in content:
        # This is actually valid but can cause deadlocks
        errors.append("Warning: GetAwaiter().GetResult() can cause deadlocks in UI context")
    
    return errors

def main():
    print("Checking C# files for compilation errors...")
    print("=" * 60)
    
    # Find all C# files
    cs_files = glob.glob('src/**/*.cs', recursive=True)
    
    issues_found = False
    for filepath in cs_files:
        if 'obj' in filepath or 'bin' in filepath:
            continue
            
        errors = check_csharp_file(filepath)
        if errors:
            issues_found = True
            print(f"\n{filepath}:")
            for error in errors:
                print(f"  - {error}")
    
    if not issues_found:
        print("\nNo obvious compilation errors found in C# files")
    else:
        print("\n" + "=" * 60)
        print("Issues found that may cause compilation errors")

if __name__ == "__main__":
    main()
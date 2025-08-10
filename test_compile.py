#!/usr/bin/env python3
import subprocess
import sys
import os

def test_compile():
    """Test if the project compiles using msbuild or xbuild"""
    
    # Try to find a build tool
    build_tools = [
        ('msbuild', ['msbuild', '/version']),
        ('xbuild', ['xbuild', '/version']),
        ('dotnet', ['dotnet', '--version'])
    ]
    
    build_tool = None
    for name, cmd in build_tools:
        try:
            result = subprocess.run(cmd, capture_output=True, text=True)
            if result.returncode == 0:
                build_tool = name
                print(f"Found build tool: {name}")
                break
        except FileNotFoundError:
            continue
    
    if not build_tool:
        print("No .NET build tools found (msbuild, xbuild, or dotnet)")
        print("\nTo compile on Linux, you need:")
        print("1. Install .NET SDK: https://dotnet.microsoft.com/download")
        print("2. Or use Mono: sudo apt-get install mono-complete")
        return False
    
    # Find solution or project files
    solution_files = []
    for root, dirs, files in os.walk('src'):
        for file in files:
            if file.endswith('.sln'):
                solution_files.append(os.path.join(root, file))
            elif file.endswith('.csproj'):
                solution_files.append(os.path.join(root, file))
    
    if not solution_files:
        print("No .sln or .csproj files found")
        return False
    
    print(f"\nFound project files:")
    for f in solution_files:
        print(f"  - {f}")
    
    # Try to compile each project
    print("\nAttempting compilation...")
    success = True
    
    for project_file in solution_files:
        if project_file.endswith('.csproj'):
            print(f"\nChecking {project_file}:")
            
            if build_tool == 'dotnet':
                cmd = ['dotnet', 'build', project_file, '--no-restore', '--verbosity', 'minimal']
            elif build_tool == 'msbuild':
                cmd = ['msbuild', project_file, '/p:Configuration=Debug', '/verbosity:minimal']
            else:  # xbuild
                cmd = ['xbuild', project_file, '/p:Configuration=Debug', '/verbosity:minimal']
            
            # Just do a syntax check, not full build
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if 'error' in result.stdout.lower() or 'error' in result.stderr.lower():
                print(f"  ❌ Errors found:")
                # Extract error messages
                for line in result.stdout.split('\n'):
                    if 'error' in line.lower():
                        print(f"    {line.strip()}")
                for line in result.stderr.split('\n'):
                    if 'error' in line.lower():
                        print(f"    {line.strip()}")
                success = False
            else:
                print(f"  ✅ No obvious compilation errors")
    
    return success

if __name__ == "__main__":
    print("Testing C# Compilation")
    print("=" * 60)
    
    if test_compile():
        print("\n✅ Project should compile successfully")
        sys.exit(0)
    else:
        print("\n❌ Compilation issues detected")
        sys.exit(1)
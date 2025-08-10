#!/usr/bin/env python3
"""
Simple script to try building the project and capture nullability warnings
"""
import subprocess
import sys
import os

def find_dotnet():
    """Try to find dotnet executable"""
    possible_paths = [
        '/usr/bin/dotnet',
        '/usr/local/bin/dotnet',
        '/opt/dotnet/dotnet',
        '/snap/bin/dotnet',
        'dotnet'  # Try PATH
    ]
    
    for path in possible_paths:
        try:
            result = subprocess.run([path, '--version'], 
                                  capture_output=True, text=True, timeout=10)
            if result.returncode == 0:
                print(f"Found dotnet at: {path}")
                return path
        except:
            continue
    
    return None

def build_project(dotnet_path):
    """Build the project and capture output"""
    try:
        # Build in release mode
        cmd = [dotnet_path, 'build', '-c', 'Release', '--verbosity', 'normal']
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)
        
        # Combine stdout and stderr
        output = result.stdout + result.stderr
        
        # Filter for nullability warnings
        lines = output.split('\n')
        null_warnings = []
        
        for line in lines:
            if 'warning' in line.lower() and 'null' in line.lower():
                null_warnings.append(line.strip())
        
        print("=== NULLABILITY WARNINGS ===")
        if null_warnings:
            for warning in null_warnings:
                print(warning)
        else:
            print("No nullability warnings found!")
            
        print(f"\n=== BUILD RESULT ===")
        print(f"Return code: {result.returncode}")
        if result.returncode != 0:
            print("Build failed. Full output:")
            print(output)
        
        return len(null_warnings)
        
    except subprocess.TimeoutExpired:
        print("Build timed out!")
        return -1
    except Exception as e:
        print(f"Error during build: {e}")
        return -1

def main():
    print("Checking for nullability warnings in C# project...")
    
    # Change to project directory
    project_dir = "/mnt/1TB_RAID10/Dropbox/.httpd/~scripts/cvedia/kb-odo"
    if os.path.exists(project_dir):
        os.chdir(project_dir)
        print(f"Changed to directory: {project_dir}")
    
    # Find dotnet
    dotnet_path = find_dotnet()
    if not dotnet_path:
        print("Could not find dotnet executable!")
        sys.exit(1)
    
    # Build and check
    warning_count = build_project(dotnet_path)
    
    if warning_count > 0:
        print(f"\nFound {warning_count} nullability warnings")
    elif warning_count == 0:
        print("\nNo nullability warnings found - all fixes successful!")
    else:
        print("\nBuild failed or timed out")

if __name__ == "__main__":
    main()
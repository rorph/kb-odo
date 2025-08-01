# Keyboard Mouse Odometer - Windows Deployment Guide

## Overview

This document provides comprehensive instructions for building, signing, and deploying the Keyboard Mouse Odometer Windows desktop application.

## Build Output Summary

✅ **Successfully generating Windows .exe files**
- **64-bit**: `publish/windows/win-x64/KeyboardMouseOdometer.UI.exe` (74.2 MB)
- **32-bit**: `publish/windows/win-x86/KeyboardMouseOdometer.UI.exe` (68.6 MB)

## Build Methods

### Method 1: Cross-Platform Build (Linux/macOS to Windows)

```bash
# Build 64-bit Windows executable
./build-windows.sh --version 1.0.0 --runtime win-x64

# Build 32-bit Windows executable  
./build-windows.sh --version 1.0.0 --runtime win-x86

# Build ARM64 Windows executable
./build-windows.sh --version 1.0.0 --runtime win-arm64
```

### Method 2: Native Windows Build

```cmd
# Build 64-bit Windows executable
build-windows.cmd --version 1.0.0 --runtime win-x64

# Build 32-bit Windows executable
build-windows.cmd --version 1.0.0 --runtime win-x86
```

### Method 3: Visual Studio Publish Profile

```bash
dotnet publish src/KeyboardMouseOdometer.UI/KeyboardMouseOdometer.UI.csproj -p:PublishProfile=WindowsRelease
```

## Deployment Characteristics

### Self-Contained Single File Executable
- **No .NET Runtime Required**: Includes .NET 8 runtime
- **Single File**: Everything packaged into one .exe
- **Native Libraries**: Embedded SQLite and other dependencies
- **Compressed**: Built-in compression reduces file size
- **Ready-to-Run**: Disabled for cross-platform compatibility

### System Requirements
- **Operating System**: Windows 10 version 1607+ or Windows Server 2016+
- **Architecture**: x64, x86, or ARM64 (depending on build)
- **Memory**: 512 MB RAM minimum, 1 GB recommended
- **Disk Space**: 100 MB for extraction and operation

## Code Signing and Security

### Why Code Signing is Important
1. **Windows Defender SmartScreen**: Unsigned executables show security warnings
2. **User Trust**: Signed applications appear more professional and trustworthy
3. **Enterprise Deployment**: Many organizations require code-signed applications
4. **Automatic Updates**: Signed applications can use Windows Update mechanisms

### Obtaining a Code Signing Certificate

#### Option 1: Commercial Certificate Authority
- **DigiCert** - $299-599/year
- **Sectigo (formerly Comodo)** - $85-200/year  
- **GlobalSign** - $199-299/year

#### Option 2: Extended Validation (EV) Certificate
- **Higher Trust Level**: Bypasses SmartScreen warnings immediately
- **Hardware Token Required**: Certificate stored on USB hardware token
- **Cost**: $300-800/year
- **Validation**: Extensive business verification process

### Code Signing Process

#### Step 1: Install Certificate
```cmd
# Import certificate to Windows certificate store
certmgr.exe -add certificate.p12 -s -r localMachine my
```

#### Step 2: Sign the Executable
```cmd
# Using SignTool (Windows SDK)
signtool sign /fd SHA256 /t http://timestamp.digicert.com /n "Your Company Name" KeyboardMouseOdometer.UI.exe

# Using Azure Key Vault (cloud signing)
AzureSignTool sign -kvu https://yourvault.vault.azure.net -kvi clientid -kvs clientsecret -kvc certificatename -fd SHA256 -tr http://timestamp.digicert.com KeyboardMouseOdometer.UI.exe
```

#### Step 3: Verify Signature
```cmd
signtool verify /pa /v KeyboardMouseOdometer.UI.exe
```

### Automated Signing in Build Process

Add to `build-windows.cmd`:
```cmd
REM Sign the executable if certificate is available
if exist "%CERT_PATH%" (
    echo Signing executable...
    signtool sign /fd SHA256 /t http://timestamp.digicert.com /f "%CERT_PATH%" /p "%CERT_PASSWORD%" "%OUTPUT_DIR%\%RUNTIME%\KeyboardMouseOdometer.UI.exe"
    if %ERRORLEVEL% neq 0 (
        echo Warning: Code signing failed
    ) else (
        echo Executable signed successfully
    )
)
```

## Distribution Methods

### Method 1: Direct Download
- **Simple**: Users download and run the .exe directly
- **Security Warning**: Unsigned executables show SmartScreen warnings
- **Best For**: Internal/testing use

### Method 2: Windows Package Manager (winget)
```yaml
# winget manifest example
PackageIdentifier: YourCompany.KeyboardMouseOdometer
PackageVersion: 1.0.0
PackageName: Keyboard Mouse Odometer
Publisher: Your Company
License: MIT
ShortDescription: Track keyboard and mouse usage statistics
Installers:
- Architecture: x64
  InstallerType: exe
  InstallerUrl: https://releases.yoursite.com/v1.0.0/KeyboardMouseOdometer-1.0.0-win-x64.exe
  InstallerSha256: [SHA256 hash]
```

### Method 3: Microsoft Store (MSIX Package)
```xml
<!-- Package.appxmanifest -->
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
  <Identity Name="YourCompany.KeyboardMouseOdometer" 
            Publisher="CN=Your Company" 
            Version="1.0.0.0" />
  <Applications>
    <Application Id="KeyboardMouseOdometer" 
                 Executable="KeyboardMouseOdometer.UI.exe"
                 EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="Keyboard Mouse Odometer" />
    </Application>
  </Applications>
</Package>
```

### Method 4: Chocolatey Package
```powershell
# chocolateyinstall.ps1
$packageName = 'keyboard-mouse-odometer'
$url64 = 'https://releases.yoursite.com/v1.0.0/KeyboardMouseOdometer-1.0.0-win-x64.exe'
$checksum64 = '[SHA256 hash]'

Install-ChocolateyPackage $packageName 'exe' '/S' $url64 -checksum64 $checksum64
```

## Enterprise Deployment

### Group Policy Deployment
1. Copy signed executable to network share
2. Create GPO for software installation
3. Deploy via Computer Configuration > Software Settings

### Microsoft Intune
1. Upload .exe as line-of-business app
2. Configure installation command: `KeyboardMouseOdometer.UI.exe /S`
3. Set detection rules and requirements

### SCCM/ConfigMgr
1. Create application in SCCM console
2. Define deployment types for different architectures
3. Distribute to target collections

## Security Considerations

### Application Permissions
- **Low-Level Input Hooks**: Requires administrator privileges for global hooks
- **File System Access**: Needs write access to user profile for data storage
- **Network Access**: Optional for telemetry or updates

### Windows Defender Configuration
```powershell
# Exclude application from real-time scanning (if needed)
Add-MpPreference -ExclusionPath "C:\Program Files\KeyboardMouseOdometer"
Add-MpPreference -ExclusionProcess "KeyboardMouseOdometer.UI.exe"
```

### Firewall Configuration
```cmd
# Allow through Windows Firewall (if network features are added)
netsh advfirewall firewall add rule name="Keyboard Mouse Odometer" dir=in action=allow program="KeyboardMouseOdometer.UI.exe"
```

## Performance Optimization

### Build Optimizations Applied
- ✅ **Self-Contained**: No runtime dependency
- ✅ **Single File**: Reduced deployment complexity
- ✅ **Compression**: Smaller download size
- ❌ **ReadyToRun**: Disabled for cross-platform builds
- ❌ **Trimming**: Disabled to avoid WPF compatibility issues

### Runtime Performance
- **Startup Time**: ~2-3 seconds (cold start with extraction)
- **Memory Usage**: ~50-80 MB typical
- **CPU Usage**: Minimal (<1% during normal operation)

## Troubleshooting

### Common Issues

#### "Windows protected your PC" SmartScreen Warning
- **Cause**: Unsigned executable
- **Solution**: Code sign the application or use EV certificate

#### Application Won't Start
- **Cause**: Missing Visual C++ Redistributables
- **Solution**: Self-contained build includes required libraries

#### Permission Denied Errors
- **Cause**: Application needs elevated privileges for system hooks
- **Solution**: Run as administrator or use UAC manifest

### Debug Information
```cmd
# Enable detailed logging
KeyboardMouseOdometer.UI.exe --debug --log-level verbose

# Check Windows Event Logs
eventvwr.msc > Application and Services Logs > Microsoft > Windows > AppModel-Runtime
```

## Version Management

### Semantic Versioning
- **Major.Minor.Patch** (e.g., 1.0.0)
- Update version in build scripts and project files
- Maintain changelog for release notes

### Auto-Update Mechanism
Consider implementing:
- **ClickOnce Deployment** for automatic updates
- **Squirrel.Windows** for delta updates  
- **Custom Update Service** with background checks

## Build Environment Setup

### Required Tools
- **.NET 8 SDK** - Latest version
- **Visual Studio 2022** or **VS Code** with C# extension
- **Windows SDK** - For signtool (code signing)
- **Git** - Source control

### CI/CD Pipeline Example (GitHub Actions)
```yaml
name: Build Windows Release
on:
  push:
    tags: ['v*']

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - run: ./build-windows.cmd --version ${{ github.ref_name }}
    - uses: actions/upload-artifact@v3
      with:
        name: windows-executables
        path: publish/windows/
```

## Contact and Support

For deployment issues or questions:
- **Documentation**: This file and README.md
- **Issues**: GitHub Issues tracker
- **Security**: Report vulnerabilities privately

---

*Last Updated: July 31, 2025*
*Build System: .NET 8.0, Cross-platform compatible*
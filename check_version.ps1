$assembly = [System.Reflection.Assembly]::LoadFrom("./src/KeyboardMouseOdometer.UI/bin/Release/net8.0-windows/KeyboardMouseOdometer.UI.dll")
$version = $assembly.GetName().Version
Write-Host "Assembly Version: $($version.Major).$($version.Minor).$($version.Build).$($version.Revision)"
Write-Host "Display Version: v$($version.Major).$($version.Minor).$($version.Build)"
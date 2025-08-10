# Keyboard Mouse Odometer - Logging Guide

## Overview
The application now includes comprehensive logging to help diagnose crashes and issues. All critical operations are logged with detailed information.

## Log Locations

### Windows
All logs and crash dumps are stored in:
```
%LOCALAPPDATA%\KeyboardMouseOdometer\
```

This typically resolves to:
```
C:\Users\[YourUsername]\AppData\Local\KeyboardMouseOdometer\
```

### Directory Structure
```
KeyboardMouseOdometer\
├── logs\                    # Application logs
│   ├── app_20240101.log    # Daily rotating log files
│   ├── app_20240102.log
│   └── ...
├── crashes\                 # Crash dumps
│   ├── crash_20240101_143025.txt
│   └── ...
├── config.json             # Configuration file
└── odometer.db             # SQLite database
```

## Log Files

### Application Logs (`logs/app_YYYYMMDD.log`)
- **Rotation**: Daily
- **Retention**: 7 days (older logs are automatically deleted)
- **Format**: Timestamp, Log Level, Source, Message, Exception details
- **Content**: 
  - Application startup/shutdown
  - Database operations
  - Configuration changes
  - Input tracking events
  - Errors and warnings
  - Heatmap color scheme changes

### Crash Dumps (`crashes/crash_YYYYMMDD_HHMMSS.txt`)
- **Created**: When application crashes
- **Content**:
  - Crash timestamp
  - Application version
  - Operating system details
  - .NET version
  - Full exception stack trace
  - Inner exceptions

## Log Levels

- **DEBUG**: Detailed diagnostic information
- **INFO**: General informational messages
- **WARNING**: Warning messages for potential issues
- **ERROR**: Error messages for failures
- **FATAL**: Critical failures that may cause application termination

## Viewing Logs

### Method 1: Direct File Access
1. Press `Win + R`
2. Type `%LOCALAPPDATA%\KeyboardMouseOdometer\logs`
3. Press Enter
4. Open the latest log file with any text editor

### Method 2: PowerShell
```powershell
# View latest log
Get-Content "$env:LOCALAPPDATA\KeyboardMouseOdometer\logs\app_$(Get-Date -Format 'yyyyMMdd').log" -Tail 50

# Monitor log in real-time
Get-Content "$env:LOCALAPPDATA\KeyboardMouseOdometer\logs\app_$(Get-Date -Format 'yyyyMMdd').log" -Wait

# Search for errors
Select-String -Path "$env:LOCALAPPDATA\KeyboardMouseOdometer\logs\*.log" -Pattern "ERROR|FATAL"
```

### Method 3: Command Prompt
```cmd
# Navigate to logs directory
cd %LOCALAPPDATA%\KeyboardMouseOdometer\logs

# View latest entries
type app_20240101.log | more

# Find errors
findstr /C:"ERROR" /C:"FATAL" *.log
```

## Common Issues and What to Look For

### Application Won't Start
Look for:
- `"Failed to initialize database"`
- `"Database initialization failed"`
- `"Failed to load configuration"`

### Data Not Saving
Look for:
- `"Failed to save daily stats"`
- `"Failed to save pending data"`
- `"Transaction rolled back"`
- `"Day rollover detected"`

### Heatmap Issues
Look for:
- `"Heatmap color scheme changed"`
- `"Failed to load heatmap data"`
- Any errors mentioning `HeatmapViewModel`

### UI Crashes
Look for:
- `"Dispatcher unhandled exception"`
- `"Unobserved task exception"`
- Stack traces mentioning UI components

## Enabling Diagnostic Mode

For even more detailed logging, run the application with diagnostic flag:
```cmd
KeyboardMouseOdometer.UI.exe --diagnostic
```

This will:
- Show additional startup diagnostics
- Display detailed error messages
- Create more verbose log entries

## Reporting Issues

When reporting issues, please include:
1. The relevant log file(s) from the date of the issue
2. Any crash dump files if the application crashed
3. Your `config.json` file (remove any sensitive information)
4. Steps to reproduce the issue

## Log File Examples

### Successful Startup
```
2024-01-01 10:00:00.123 [INF] KeyboardMouseOdometer.UI.App Application startup initiated
2024-01-01 10:00:00.234 [INF] KeyboardMouseOdometer.Core.Services.DatabaseService Initializing database with connection: Data Source=C:\Users\User\AppData\Local\KeyboardMouseOdometer\odometer.db
2024-01-01 10:00:00.345 [INF] KeyboardMouseOdometer.Core.Services.DatabaseService Database journal mode set to: wal
2024-01-01 10:00:00.456 [INF] KeyboardMouseOdometer.Core.Services.DatabaseService Database initialized successfully with WAL mode
```

### Error Example
```
2024-01-01 10:00:00.123 [ERR] KeyboardMouseOdometer.Core.Services.DataLoggerService Failed to save pending data to database. Stats may be lost!
System.Data.SQLite.SQLiteException: database is locked
   at Microsoft.Data.Sqlite.SqliteCommand.ExecuteNonQuery()
   at KeyboardMouseOdometer.Core.Services.DatabaseService.SaveDailyStatsAsync()
```

### Day Rollover
```
2024-01-01 23:59:59.999 [WRN] KeyboardMouseOdometer.Core.Services.DataLoggerService Day rollover detected: 2024-01-01 -> 2024-01-02. Saving old day's data.
2024-01-02 00:00:00.001 [INF] KeyboardMouseOdometer.Core.Services.DataLoggerService Performing synchronous save for day rollover to prevent data loss
2024-01-02 00:00:00.123 [INF] KeyboardMouseOdometer.Core.Services.DataLoggerService Day rollover save completed successfully
2024-01-02 00:00:00.124 [INF] KeyboardMouseOdometer.Core.Services.DataLoggerService New day stats initialized for 2024-01-02
```

## Troubleshooting Tips

1. **Clear logs if they get too large**: Delete old log files from the logs directory
2. **Database locked errors**: Close all instances of the application and try again
3. **Configuration not saving**: Check file permissions on the AppData directory
4. **Missing logs**: Ensure the application has write permissions to AppData

## Contact Support

If you continue experiencing issues after checking the logs:
1. Create an issue on GitHub with the log files attached
2. Include system information (Windows version, .NET version)
3. Describe the issue and steps to reproduce
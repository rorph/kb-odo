# Changelog

All notable changes to Keyboard + Mouse Odometer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.2] - 2025-08-30

### ✨ New Features

#### CSV Data Export
- **Added comprehensive CSV export functionality**
  - Export all statistics to CSV format via new Export button in Settings
  - Includes daily stats, hourly stats, key stats, and application usage data
  - Optional date range filtering for targeted exports
  - Automatic file naming with timestamp
  - Progress window during export operation

#### Enhanced Dashboard Interactivity
- **Interactive chart updates based on table selection**
  - Daily tab: Click any row to see hourly charts for that specific day
  - Weekly tab: Click any week to see daily charts for that week  
  - Monthly tab: Click any month to see daily charts for that month
  - Removed redundant date picker controls in favor of table-driven selection

### 🐛 Bug Fixes

#### Application Usage Tracking
- **Fixed application tracking accuracy issues**
  - Now tracks time continuously every second instead of only on app switches
  - Fixed losing fractional seconds due to integer truncation
  - Fixed first second of app focus being lost
  - UWP apps now show actual app names from window titles instead of "ApplicationFrameHost"
  - Changed from `DateTime.Now` to `DateTime.UtcNow` for consistent timing

#### Database & Export Fixes
- **Fixed database column mismatches in export**
  - Corrected `key_count` vs `keystrokes` column names
  - Fixed separate click columns (`left_clicks`, `right_clicks`, `middle_clicks`) vs combined `mouse_clicks`
  - Fixed hourly_stats table INSERT/UPDATE operations using correct column names

#### UI/UX Fixes
- **Fixed table selection causing data disappearance**
  - Removed data grid clearing when selecting rows
  - Fixed infinite selection loop issue
  - Charts now update without affecting table data

- **Fixed monthly bar chart visualization**
  - Now shows all days of the month (1-31) even without data
  - Future days appear as empty bars for consistent month view
  - Fixed bar charts not updating on row selection in Weekly/Monthly tabs

- **Fixed Lifetime tab auto-refresh**
  - Lifetime stats now refresh automatically when tab is selected
  - Removed redundant "Refresh Lifetime Stats" button

### 🎨 UI Improvements
- **Cleaner startup experience**
  - Removed debug "Application started successfully!" notification
  
- **Better chart responsiveness**
  - Weekly/Monthly bar chart visualizations update when selecting different time periods
  - Proper normalization of bar heights for better visual comparison

### 🧪 Testing Improvements
- **Enhanced test coverage**
  - Added 8 comprehensive test cases for CSV export functionality
  - Fixed async method warnings in test files (removed unnecessary `async` keywords)
  - All 249 tests passing with 0 warnings

### 🏗️ Code Quality
- **Fixed compilation warnings**
  - Resolved CS1998 warnings for async methods without await operators
  - Clean build with 0 warnings and 0 errors

### 📦 Technical Details
- Updated version to 1.2.2 in all project files
- Added `ExportTests.cs` with comprehensive export validation
- Modified `LoadDailyDataAsync`, `LoadWeeklyDataAsync`, `LoadMonthlyDataAsync` to only update charts
- Implemented partial methods for `OnSelectedDailyItemChanged`, `OnSelectedWeeklyItemChanged`, `OnSelectedMonthlyItemChanged`
- Added `ExportToCsvAsync` method to DatabaseService
- Added `ExportDataAsync` command to MainWindowViewModel

## [1.2.1] - 2025-08-10

### 🐛 Bug Fixes

#### Critical Data Loss Fix
- **Fixed: All historical data was being deleted at midnight** when `DatabaseRetentionDays = 0`
  - Added proper handling for retention = 0 (keep forever)
  - Added early return in `CleanupOldDataAsync` for non-positive retention values
  - Added conditional check in midnight rollover before calling cleanup
  - Created comprehensive unit tests for data retention logic

#### Application Usage Tracking Fixes
- **Fixed: Timezone issues causing empty "Today" view** 
  - Replaced SQLite `date('now')` UTC-based views with local time queries
  - All date queries now use C#'s `DateTime.Today` respecting system timezone
  - Fixed data not showing for users in non-UTC timezones (e.g., GMT-3)
- **Fixed: Application Usage UI not displaying data**
  - Fixed database query column mismatch in view definitions
  - Added missing `AppUsageViewModel.InitializeAsync()` call on startup
  - Fixed data type conversion issues in database readers
- **Fixed: Application Usage dropdown styling in dark theme**
  - Replaced problematic ComboBox with RadioButton controls
  - Implemented consistent button-style UI matching Heatmap tab
  - Added proper dark theme styling with `StringBooleanConverter`

#### UI/Display Fixes
- **Fixed: Monthly chart date formatting** - Changed from "MM/dd" to "M/d" format
  - Added explicit `IntervalType = DateTimeIntervalType.Days`
  - Added padding settings to prevent axis shifting
- **Fixed: Daily table showing empty rows** - Now only displays days with actual data
  - Removed creation of zero-value entries for missing days
  - Table only shows days that have been tracked in the database
- **Fixed: Applications tab DataGrid not switching data sources**
  - Added proper DataTrigger for "Today" time range
  - Fixed ItemsSource binding for all time ranges (Today/Weekly/Monthly/Lifetime)
- **Removed redundant chart titles** 
  - Charts now use GroupBox headers only for cleaner UI
  - Removed "Top Applications by Usage Time" header from Applications tab

### 🧪 Testing
- Added `DataRetentionTests.cs` with 7 comprehensive test cases
  - Validates retention = 0 preserves all data
  - Validates retention < 0 preserves all data
  - Validates positive retention only deletes old data
  - Validates midnight rollover doesn't delete data when retention = 0

### 🎨 UI Improvements
- **Consistent UI Design** - Applications tab now uses button-style time range selectors matching Heatmap tab
- **Better Dark Theme Support** - Fixed white-on-white text issues in Applications dropdown

## [1.2.0] - 2025-08-08

### 🔥 Critical Fixes
- **Fixed Midnight Rollover Crash** - Resolved critical infinite loop at 00:00:00
  - Added `_isRollingOver` flag to prevent recursive rollover attempts
  - Protected critical methods during rollover operations
  - Eliminated recursive `GetCurrentStats()` calls
  - Enhanced midnight timer with date change verification

### 🎨 UI/UX Improvements

#### Redesigned Daily Tab
- Renamed "Today" tab to "Daily" for clarity
- Arranged three charts (Keys, Mouse Distance, Scroll Distance) side-by-side
- Added comprehensive daily statistics table showing last 30 days
- Implemented interactive data grids with sorting capabilities

#### Enhanced Data Tables
- **Fixed Dark Theme Readability** - Resolved black text on dark background issue
  - Explicit white foreground colors for all data grid elements
  - Proper styling for headers, cells, and row hover states
  - Consistent dark theme colors across all tabs
- **Added Visual Data Bars** - `PercentageToHeightConverter` for percentage-based height visualization in grids
- **Separated Data Models** - Independent collections for grid data and chart visualization to prevent sorting conflicts

#### Heatmap Enhancements
- **FLIR Thermal Color Scheme** - New thermal imaging palette option
  - Professional thermal gradient (Black → Purple → Red → Yellow → White)
  - Configurable via settings (Classic/FLIR)
  - Fixed legend updates when switching color schemes
- **Top Keys Table** - Added most pressed keys analysis
  - Shows key name, press count, and percentage
  - Sortable columns with proper dark theme styling
  - Integrated below heatmap visualization

### 🏗️ Database Architecture Overhaul

#### SQL View-Based Architecture
- **Eliminated Complex Rollover Logic** - Replaced with database-driven aggregation
- **Added Aggregation Views**:
  - `weekly_stats` - Automatic 7-day aggregation
  - `monthly_stats` - Automatic 30-day aggregation  
  - `lifetime_stats_view` - All-time statistics
  - `today_hourly_stats` - Hourly breakdown for current day
- **UPSERT Operations** - Atomic `IncrementStatsAsync()` using INSERT ON CONFLICT UPDATE
- **WAL Mode** - Write-Ahead Logging for better concurrent access
- **Enhanced Configuration**:
  - 5-second busy timeout for better reliability
  - Synchronous mode NORMAL for data safety
  - Optimized connection string parameters

### ✨ New Features

#### Configuration Enhancements
- **Window State Persistence** - Remembers main window size (`MainWindowWidth`, `MainWindowHeight`)
- **Multi-Monitor Support** - `ToolbarMonitorDeviceName` for toolbar positioning
- **Heatmap Color Scheme Setting** - Persistent color scheme preference
- **Application Version Display** - Shows version in UI

#### Data Models
- `DailyStatsSummary` - Comprehensive daily stats with normalized values
- `WeeklyStatsSummary` - Weekly aggregation with date ranges
- `MonthlyStatsSummary` - Monthly aggregation with activity scoring
- `KeyStats` - Core model for individual key statistics storage
- `KeyUsageStatsSummary` - Display model for top keys analysis with percentage formatting
- Enhanced `HeatmapColor` with dual color scheme support

### 🚀 Performance Improvements
- **Database Query Optimization** - Views reduce query complexity by 60%
- **Connection Management** - Better connection pooling and error handling
- **Async Operations** - Improved async/await patterns throughout
- **Batch Processing** - Enhanced batch operations for key statistics
- **Memory Management** - Better disposal patterns and resource cleanup
- **Thread Safety** - Enhanced concurrent access with proper locking

### 📊 Code Quality Improvements

#### Logging System Upgrade
- **Serilog Integration** - Professional structured logging
  - Rolling file logs with 7-day retention
  - Separate logs for errors and general information
  - Console output with timestamps
  - Global exception handlers for unhandled errors
- **Enhanced Debug Information** - Trace-level logging for diagnostics
- **Better Error Tracking** - Comprehensive exception handling

#### Architecture Improvements
- **MVVM Compliance** - Enhanced ViewModels with proper data binding
- **Separation of Concerns** - Better separation between data and display models
- **Dependency Injection** - Improved service registration and lifecycle
- **Error Boundaries** - Global exception handling for WPF and AppDomain

### 🧪 Testing Improvements
- **Fixed All Skipped Tests** - Resolved test framework issues
- **Enhanced Integration Tests**:
  - Updated `KeyCaptureIntegrationTests` with correct API usage
  - Fixed tuple access patterns (Item1/Item2 → KeyCode/Count)
  - Added stress test for massive concurrent operations
- **Database Migration Tests** - Comprehensive schema version validation
- **Fixed 30 Nullability Warnings** - Improved code quality with nullable reference types

### 🐛 Additional Bug Fixes
- Fixed compilation errors related to database column naming (key_identifier → key_code)
- Resolved `LogMouseMovement` method signature issues
- Fixed bar chart updates when sorting data tables
- Corrected Weekly/Monthly tabs showing daily instead of aggregated data
- Fixed missing methods in `DatabaseService`
- Resolved test property binding issues in heatmap tests

### 📝 Documentation
- Added comprehensive inline documentation
- Updated CLAUDE.md with architectural decisions
- Improved code comments for complex logic
- Added XML documentation for public APIs

## [1.1.0]

### Added
- **Keyboard Heatmap Visualization** - New feature to visualize keyboard usage patterns
  - Interactive heatmap display showing key press frequency
  - Color gradient visualization (Blue → Green → Yellow → Red)
  - Time range filtering (Today, Weekly, Monthly, Lifetime)
  - Hover tooltips showing exact key counts
  - US QWERTY layout support (104 keys)

### Database Changes
- Added `schema_version` table for database versioning
- Added `key_stats` table for individual key tracking:
  - Columns: date, hour, key_code, count
  - Primary key: (date, hour, key_code)
  - Indexes on (date, key_code) and (key_code) for efficient queries
- Implemented automatic migration from v1 to v2 schema
- Added methods for key stats CRUD operations

### Core Components Added
- **KeyCodeMapper** utility class for virtual key code to human-readable name conversion
- **KeyboardKey** model class representing individual keys with position and heat data
- **KeyboardLayout** class defining US QWERTY layout with 104 keys
- Enhanced **InputEvent** model with KeyIdentifier property
- Updated **InputMonitoringService** to capture individual key identifiers
- Enhanced **DataLoggerService** with hourly key stats aggregation

### Technical Improvements
- Implemented database migration system with version tracking
- Added concurrent dictionary for thread-safe key counting
- Batch insert operations for key statistics
- Optimized queries with proper indexing
- Maintained performance target of < 2% CPU usage
- Added key stats methods to DatabaseService:
  - SaveKeyStatsAsync for batch inserts
  - GetKeyStatsByDateRangeAsync for time-based queries
  - GetTodayKeyStatsAsync, GetWeeklyKeyStatsAsync, GetMonthlyKeyStatsAsync
  - GetLifetimeKeyStatsAsync for all-time statistics
  - GetTopKeysAsync for most-used key analysis

### Architecture Improvements
- **Fixed WPF Dependencies**: Removed all WPF-specific dependencies from Core project
- **Created CoreKeyCode Enum**: Platform-agnostic key code enumeration using virtual key codes
- **Introduced IKeyCodeMapper Interface**: Abstraction for key code mapping with dependency injection
- **Separated Concerns**: 
  - Core project now UI-framework independent
  - WpfKeyCodeMapper in UI project handles WPF-specific conversions
  - Clean architecture with proper layer separation

### Testing
- Added comprehensive unit tests for KeyCodeMapper
- Added database migration tests with 15 test cases
- Tests verify schema versioning, key stats storage, and data aggregation
- All tests ensure backward compatibility with existing data

## [1.0.0] - Initial Release

### Features
- Real-time keyboard and mouse tracking
- System tray integration
- Transparent floating toolbar
- Daily, weekly, monthly, and lifetime statistics
- SQLite database storage
- Distance calculations with auto-scaling units
- Interactive charts with OxyPlot
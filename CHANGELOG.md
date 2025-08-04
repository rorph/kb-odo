# Changelog

All notable changes to Keyboard + Mouse Odometer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - In Progress

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
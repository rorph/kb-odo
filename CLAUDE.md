# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Keyboard + Mouse Odometer for Windows** - a fully functional C# desktop application that tracks comprehensive keyboard and mouse usage statistics. The application runs in the system tray and provides detailed real-time metrics about computer usage.

### Implemented Features

**Input Tracking:**
- Real-time keypress tracking with individual key identification
- **NEW: Individual key statistics for heatmap visualization**
- Mouse movement distance calculation with smart unit scaling
- Mouse click tracking (left/right/middle button separately)
- Scroll wheel distance measurement
- System-wide global input hooks using Windows API

**Data Storage & Analytics:**
- SQLite database with comprehensive schema
- Daily statistics aggregation
- Hourly statistics for intra-day patterns
- Lifetime cumulative statistics
- Automatic data retention management

**User Interface:**
- **Transparent Resizable Toolbar**: Semi-transparent floating toolbar showing real-time stats
- **Comprehensive Dashboard**: Tabbed interface with Today/Weekly/Monthly/Lifetime/Settings views
- **NEW: Keyboard Heatmap Visualization**: Interactive heatmap showing key usage patterns with time ranges
- **Interactive Charts**: OxyPlot-powered charts for usage patterns
- **System Tray Integration**: Minimize to tray with context menu
- **Settings Panel**: Configurable tracking options and display preferences

**Distance Units & Scaling:**
- **Auto-scaling Unit System**: Metric (mm/cm/m/km/Mm), Imperial (in/ft/mi), Pixels (px/Kpx/Mpx/Gpx)
- **Smart Unit Selection**: Automatically chooses appropriate unit based on magnitude
- **DPI-aware Calculations**: Accurate physical distance conversion from pixel movement
- **Scroll Distance Tracking**: Realistic scroll wheel distance measurement

## Technology Stack

- **Language**: C# (.NET 8)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite3 with Microsoft.Data.Sqlite
- **Charts**: OxyPlot.Wpf for interactive data visualization
- **Global Hooks**: MouseKeyHook NuGet package
- **System Tray**: Hardcodet.NotifyIcon.Wpf
- **Target OS**: Windows 10/11 (x64)
- **Build System**: MSBuild / dotnet CLI

## Design Philosophy

- Always ultra think about changes, and use specialized agents to execute tasks in parallel and consult

[... rest of the existing file content remains unchanged ...]
- application must compile with no warnings and no build errors, we should have any skipped tests
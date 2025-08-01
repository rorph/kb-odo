# Keyboard + Mouse Odometer

A Windows desktop application that tracks your keyboard and mouse usage statistics, providing insights into your daily computer usage patterns.

![Build Status](https://github.com/rorph/kb-odo/actions/workflows/build-and-test.yml/badge.svg)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)

## Features

- **Comprehensive Input Tracking**
  - Real-time keypress tracking with individual key identification
  - Mouse movement distance with smart auto-scaling units (mm/cm/m/km/Mm, in/ft/mi, px/Kpx/Mpx/Gpx)
  - Mouse click tracking (left/right/middle button separately)
  - Scroll wheel distance measurement
  - System-wide global input hooks
  
- **Advanced User Interface**
  - **Transparent Resizable Toolbar**: Semi-transparent floating toolbar with real-time stats
  - **Comprehensive Dashboard**: Tabbed interface with Today/Weekly/Monthly/Lifetime/Settings views
  - **Interactive Charts**: OxyPlot-powered charts showing usage patterns throughout the day
  - **System Tray Integration**: Full system tray operation with context menu
  - **Customizable Settings**: Configurable tracking options and display preferences
  
- **Robust Data Management**
  - SQLite database with comprehensive schema (daily_stats and hourly_stats tables)
  - Hourly statistics for detailed intra-day patterns
  - Lifetime cumulative statistics with tracking period
  - Automatic data retention management
  - DPI-aware distance calculations for accurate measurements

## Screenshots

### System Tray Menu
```
[Icon] Keyboard+Mouse Odometer
-------------------------------
Open Dashboard
Pause Tracking
Exit
```

### Transparent Resizable Toolbar
```
KM Odometer | Key: W | Keys: 1,254 | Mouse: 1.32 km | Scroll: 45.2 m
```

## Requirements

- Windows 10/11 (x64, x86, or ARM64)
- .NET 8.0 Runtime (included in self-contained builds)
- Administrator privileges (for global input hooks)

## Installation

### From Release (Recommended)

1. Download the latest release from the [Releases](https://github.com/rorph/kb-odo/releases) page
2. Extract the ZIP file to your preferred location
3. Run `KeyboardMouseOdometer.UI.exe`
4. The application will start in your system tray

### From Source

1. Clone the repository:
   ```bash
   git clone https://github.com/rorph/kb-odo.git
   cd keyboard-mouse-odometer
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project src/KeyboardMouseOdometer.UI
   ```

## Building

### Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11 development environment

### Build Scripts

#### Windows (CMD)
```cmd
build.cmd              # Build in Release mode
test.cmd               # Run all tests
test.cmd --coverage    # Run tests with code coverage
```

#### Cross-platform (Bash)
```bash
./build.sh             # Build in Release mode
./test.sh              # Run all tests
./test.sh --coverage   # Run tests with code coverage
./publish.sh           # Create publishable package
```

## Development

### Project Structure

```
src/
├── KeyboardMouseOdometer.Core/      # Core business logic
│   ├── Models/                      # Data models
│   ├── Services/                    # Business services
│   └── Utils/                       # Utility classes
├── KeyboardMouseOdometer.UI/        # WPF application
│   ├── Views/                       # XAML views
│   ├── ViewModels/                  # MVVM view models
│   └── Services/                    # UI-specific services
└── KeyboardMouseOdometer.Tests/     # Unit tests
```

### Key Technologies

- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with Microsoft.Data.Sqlite
- **Input Hooks**: MouseKeyHook library
- **Charts**: OxyPlot.Wpf
- **MVVM**: CommunityToolkit.Mvvm
- **Testing**: xUnit, FluentAssertions, Moq

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific tests
dotnet test --filter "FullyQualifiedName~DatabaseService"
```

## Configuration

The application stores its configuration in `%APPDATA%\KeyboardMouseOdometer\config.json`:

```json
{
  "TrackKeyboard": true,
  "TrackMouse": true,
  "TrackClicks": true,
  "TrackScrolling": false,
  "StartWithWindows": true,
  "DataRetentionDays": 90,
  "ShowToolbar": true,
  "ToolbarPosition": "Bottom",
  "DatabasePath": "%APPDATA%\\KeyboardMouseOdometer\\odometer.db"
}
```

## Database Schema

The application uses SQLite with the following comprehensive schema:

```sql
-- Daily aggregated statistics
CREATE TABLE daily_stats (
    date TEXT PRIMARY KEY,           -- YYYY-MM-DD
    key_count INTEGER DEFAULT 0,
    mouse_distance REAL DEFAULT 0,  -- in meters
    left_clicks INTEGER DEFAULT 0,
    right_clicks INTEGER DEFAULT 0,
    middle_clicks INTEGER DEFAULT 0,
    scroll_distance REAL DEFAULT 0  -- in meters
);

-- Hourly statistics for detailed charts
CREATE TABLE hourly_stats (
    date TEXT,                       -- YYYY-MM-DD
    hour INTEGER,                    -- 0-23
    key_count INTEGER DEFAULT 0,
    mouse_distance REAL DEFAULT 0,  -- in meters
    left_clicks INTEGER DEFAULT 0,
    right_clicks INTEGER DEFAULT 0,
    middle_clicks INTEGER DEFAULT 0,
    scroll_distance REAL DEFAULT 0, -- in meters
    PRIMARY KEY (date, hour)
);
```

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [MouseKeyHook](https://github.com/gmamaladze/globalmousekeyhook) for global input hooks
- [OxyPlot](https://github.com/oxyplot/oxyplot) for charting
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) for system tray support

## Roadmap

### Implemented Features ✅
- [x] Real-time keyboard and mouse tracking
- [x] Transparent resizable toolbar
- [x] Comprehensive dashboard with Today/Weekly/Monthly/Lifetime views
- [x] Interactive charts for usage patterns
- [x] Auto-scaling distance units (Metric/Imperial/Pixels)
- [x] Scroll wheel tracking
- [x] Hourly statistics for detailed analysis
- [x] SQLite database with full schema
- [x] System tray integration
- [x] Settings panel with configurable options
- [x] Comprehensive test suite (136 tests)
- [x] Cross-platform build system

### Future Enhancements
- [ ] Export to CSV/Excel formats
- [ ] Custom chart date ranges
- [ ] Keyboard heatmap visualization
- [ ] Usage pattern analysis and insights
- [ ] Multi-monitor DPI handling improvements
- [ ] Cloud sync support (optional)
- [ ] Web dashboard
- [ ] Application-specific tracking
- [ ] Gamification features
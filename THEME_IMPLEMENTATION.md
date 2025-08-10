# Windows Theme Detection Implementation

## Overview
Implemented automatic dark/light theme detection that follows Windows system settings. The dashboard GUI now automatically adapts to the user's Windows theme preference.

## Implementation Details

### 1. Theme Detection
- **Method**: Registry-based detection
- **Registry Key**: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`
- **Value**: `AppsUseLightTheme` (0 = Dark mode, 1 = Light mode)
- **Real-time Monitoring**: Listens to `SystemEvents.UserPreferenceChanged` for theme changes

### 2. Files Created

#### ThemeManager Service (`/src/KeyboardMouseOdometer.UI/Services/ThemeManager.cs`)
- Detects Windows theme from registry
- Applies theme to application
- Monitors for theme changes
- Manages theme switching

#### Theme Resource Dictionaries
- **LightTheme.xaml** (`/src/KeyboardMouseOdometer.UI/Themes/LightTheme.xaml`)
  - Light background colors (#FFFFFF)
  - Dark text (#000000)
  - Blue accent (#0078D4)
  - Light gray content areas (#F5F5F5)
  
- **DarkTheme.xaml** (`/src/KeyboardMouseOdometer.UI/Themes/DarkTheme.xaml`)
  - Dark background (#1E1E1E)
  - Light text (#F0F0F0)
  - Blue accent (#007ACC)
  - Dark gray content areas (#252526)

### 3. Modified Files

#### App.xaml.cs
- Added ThemeManager to DI container
- Initialize ThemeManager on startup
- Dispose ThemeManager on shutdown

#### MainWindow.xaml
- Replaced hard-coded colors with DynamicResource bindings
- `Background="LightBlue"` → `Background="{DynamicResource HeaderBackgroundBrush}"`
- `Background="LightGray"` → `Background="{DynamicResource ContentBackgroundBrush}"`
- `Background="White"` → `Background="{DynamicResource ChartBackgroundBrush}"`

#### ToolbarWindow.xaml
- Updated toolbar to use theme colors
- `Background="#CC1E1E1E"` → `Background="{DynamicResource ToolbarBackgroundBrush}"`
- `Foreground="White"` → `Foreground="{DynamicResource ToolbarForegroundBrush}"`

## Color Resources

### Common Resources Used
- **WindowBackgroundBrush**: Main window background
- **WindowForegroundBrush**: Main window text
- **HeaderBackgroundBrush**: Header section background
- **HeaderForegroundBrush**: Header text color
- **ContentBackgroundBrush**: Content area background
- **ChartBackgroundBrush**: Chart background
- **ToolbarBackgroundBrush**: Floating toolbar background
- **StatValueBrush**: Statistics value color
- **StatLabelBrush**: Statistics label color
- **AccentColorBrush**: Primary accent color

## Features

### Automatic Theme Detection
- Detects Windows theme on application startup
- Applies appropriate theme immediately

### Real-time Theme Switching
- Monitors Windows theme changes
- Updates application theme without restart
- Smooth transition between themes

### Consistent Experience
- All UI elements follow theme
- Charts adapt to theme colors
- Toolbar respects theme settings

## Technical Notes

### Dynamic Resources
All color resources use `DynamicResource` binding instead of `StaticResource` to enable runtime theme switching.

### Performance
Theme switching is performed on the UI thread to ensure smooth transitions and prevent threading issues.

### Fallback
If theme detection fails, defaults to Light theme for compatibility.

## Testing

To test theme switching:
1. Open Windows Settings → Personalization → Colors
2. Toggle between "Light" and "Dark" under "Choose your mode"
3. The application should immediately update its appearance

## Benefits
- **Better User Experience**: Matches user's system preference
- **Reduced Eye Strain**: Dark mode for low-light environments
- **Professional Appearance**: Consistent with modern Windows apps
- **No Manual Configuration**: Automatic detection and switching
# Heatmap Refresh Fix - Immediate Data Update

## Problem
When clicking refresh in the heatmap tab, the data wasn't updating immediately because recent keystrokes were still in the DataLoggerService's memory buffer and hadn't been flushed to the database yet.

## Root Cause
- DataLoggerService buffers input events and saves them periodically (every 30 seconds by default)
- HeatmapViewModel was reading directly from the database without flushing pending data first
- This meant recent keystrokes (last 0-30 seconds) weren't visible until the next automatic flush

## Solution
Modified both HeatmapViewModel and MainWindowViewModel to flush pending data before refreshing:

### Files Modified

#### 1. `/src/KeyboardMouseOdometer.UI/ViewModels/HeatmapViewModel.cs`
- Added `DataLoggerService` dependency
- Created new `RefreshHeatmapAsync()` method that:
  1. Calls `_dataLoggerService.FlushAsync()` to save pending data
  2. Then calls `LoadHeatmapDataAsync()` to read updated data
- Updated both RefreshCommand and timer tick to use the new method

#### 2. `/src/KeyboardMouseOdometer.UI/ViewModels/MainWindowViewModel.cs`
- Updated `RefreshLifetimeStatsAsync()` to also flush pending data first
- Ensures consistency across all refresh operations

## Technical Details

### Before Fix
```csharp
// HeatmapViewModel - Direct database read
RefreshCommand = new RelayCommand(async () => await LoadHeatmapDataAsync());
```

### After Fix
```csharp
// HeatmapViewModel - Flush then read
RefreshCommand = new RelayCommand(async () => await RefreshHeatmapAsync());

private async Task RefreshHeatmapAsync()
{
    // Flush pending data to database first
    await _dataLoggerService.FlushAsync();
    // Then load the updated data
    await LoadHeatmapDataAsync();
}
```

## Impact
- Refresh button now shows real-time data immediately
- Both manual refresh and auto-refresh (every 5 seconds) include latest keystrokes
- Consistent behavior across all tabs (Heatmap and Lifetime)
- No data loss or delay in visualization

## Configuration Note
The flush interval is configured in `Configuration.cs`:
- `DataFlushIntervalSeconds`: Default 30 seconds
- `DatabaseSaveIntervalMs`: Default 30000ms (30 seconds)

With this fix, the refresh button bypasses the wait and forces an immediate save.
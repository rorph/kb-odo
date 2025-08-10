# Midnight Rollover Crash Fix

## Problem Identified
The application was crashing at midnight with an infinite loop, as shown by the logs:
```
2025-08-08 00:00:00.000 [INF] Midnight rollover - creating new daily stats
2025-08-08 00:00:00.005 [INF] Date changed from 2025-08-07 to 2025-08-08, rolling over stats
[repeats ~2000 times then crash]
```

## Root Cause Analysis

The infinite loop was caused by a recursive call chain:

1. At midnight, `EnsureCurrentDateStats()` detects date change
2. It calls `SavePendingDataAsync().GetAwaiter().GetResult()` to save the old day's data
3. `SavePendingDataAsync()` calls `GetCurrentStats()` to get data to save
4. `GetCurrentStats()` calls `EnsureCurrentDateStats()` again
5. Since the date hasn't been updated yet (still in step 1), it detects the date change again
6. **INFINITE LOOP!**

## The Fix

### 1. Added Rollover Protection Flag
- Added `_isRollingOver` flag to prevent recursive rollover attempts
- Check this flag at the beginning of `EnsureCurrentDateStats()` to exit early if already rolling over

### 2. Eliminated Recursive Calls
- In `EnsureCurrentDateStats()`, now create a copy of stats to save WITHOUT calling `GetCurrentStats()`
- Save data directly to database without triggering any methods that might recurse

### 3. Protected Critical Methods
- `GetCurrentStats()` now checks `_isRollingOver` flag before calling `EnsureCurrentDateStats()`
- `SavePendingDataAsync()` skips execution if rollover is in progress

### 4. Improved Midnight Timer Logic
- Added checks to prevent duplicate rollover attempts
- Verify date actually needs changing before attempting rollover
- Added more detailed logging at each step

### 5. Enhanced Logging
- Added informational logs at the start and end of rollover
- Added debug logs for skipped operations during rollover
- Added logs for successful data cleanup after rollover

## Key Changes Made

### DataLoggerService.cs

1. **Added rollover flag:**
```csharp
private bool _isRollingOver = false; // Prevent recursive rollover
```

2. **Protected EnsureCurrentDateStats:**
```csharp
if (_isRollingOver)
{
    _logger.LogDebug("Rollover already in progress, skipping");
    return;
}
```

3. **Direct data save without recursion:**
```csharp
// Create copy of stats without calling GetCurrentStats
var statsToSave = new DailyStats
{
    Date = _todayStats.Date,
    KeyCount = _todayStats.KeyCount,
    // ... copy all fields
};
// Save directly
_databaseService.SaveDailyStatsAsync(statsToSave).GetAwaiter().GetResult();
```

4. **Protected helper methods:**
```csharp
// In GetCurrentStats()
if (!_isRollingOver)
{
    EnsureCurrentDateStats();
}

// In SavePendingDataAsync()
if (_isRollingOver)
{
    _logger.LogDebug("Skipping save during rollover");
    return;
}
```

## Testing Recommendations

1. **Test normal midnight rollover:**
   - Change system time to 23:59:50
   - Monitor logs as it crosses midnight
   - Verify single rollover message, no repeats

2. **Test with active usage during rollover:**
   - Generate key/mouse events during midnight
   - Verify data is properly saved for both days

3. **Monitor logs for these key messages:**
   - "Date changed from X to Y, starting rollover"
   - "Rollover complete, new day stats initialized"
   - No repeated "Date changed" messages

4. **Verify data integrity:**
   - Check database has final stats for previous day
   - Check new day starts with zero counts
   - Verify hourly stats are preserved

## Expected Log Output at Midnight

```
[00:00:00] Midnight timer triggered - checking for rollover
[00:00:00] Midnight rollover confirmed - transitioning from 2025-08-07 to 2025-08-08
[00:00:00] Date changed from 2025-08-07 to 2025-08-08, starting rollover
[00:00:00] Saving final stats for 2025-08-07: 15234 keys, 1823.45m
[00:00:00] Saving final hourly stats for hour 23
[00:00:00] Saving 87 key stats for final hour
[00:00:00] Rollover complete, new day stats initialized for 2025-08-08
[00:00:01] Old data cleanup completed
```

## Benefits

1. **No more infinite loops** - Recursive calls eliminated
2. **Data integrity** - All data saved before creating new day
3. **Better observability** - Clear logging of rollover process
4. **Robust error handling** - Rollover completes even if some operations fail
5. **Performance** - Cleanup runs asynchronously after critical operations
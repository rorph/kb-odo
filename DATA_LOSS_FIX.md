# Critical Data Loss Bug Fix - v1.2.0

## Issue Summary
**Critical Bug**: All historical data was being deleted every night at midnight when `DatabaseRetentionDays = 0` (which should mean "keep forever").

## Root Cause Analysis

### The Bug
1. **Configuration Intent**: `DatabaseRetentionDays = 0` means "keep data forever" (never delete)
2. **Actual Behavior**: At midnight, the cleanup function was called with `retentionDays = 0`
3. **Bug Location 1**: `DatabaseService.CleanupOldDataAsync()` calculated cutoff as `DateTime.Today.AddDays(-0)` = TODAY
4. **Bug Location 2**: `DataLoggerService` midnight rollover called cleanup without checking if retention > 0
5. **Result**: All data before today was deleted every night

### Evidence from Debug Logs
```
2025-08-09 00:00:00.015 [INF] Cleaned up 3 daily stats, 56 hourly stats, and 0 events older than 0 days
```
This shows the system deleted 3 days of daily stats and 56 hours of hourly stats when retention was set to 0.

### Database Analysis
- `daily_stats` table: Only had today's data (2025-08-09)
- `key_stats` table: Had data from 2025-08-04 to 2025-08-09
- This inconsistency proved that daily_stats were being incorrectly deleted

## The Fix

### 1. DatabaseService.cs (Line ~890)
**Added early return for non-positive retention:**
```csharp
public async Task CleanupOldDataAsync(int retentionDays)
{
    // Skip cleanup if retention is 0 or negative (keep forever)
    if (retentionDays <= 0)
    {
        _logger.LogDebug("Skipping data cleanup - retention days is {RetentionDays} (keep forever)", retentionDays);
        return;
    }
    // ... rest of cleanup logic
}
```

### 2. DataLoggerService.cs (Line ~165)
**Added conditional check before cleanup:**
```csharp
// Clean up old data asynchronously (non-critical) - only if retention is enabled
if (_configuration.DatabaseRetentionDays > 0)
{
    await _databaseService.CleanupOldDataAsync(_configuration.DatabaseRetentionDays);
    _logger.LogInformation("Old data cleanup completed");
}
else
{
    _logger.LogDebug("Skipping data cleanup - retention disabled (DatabaseRetentionDays = {RetentionDays})", 
        _configuration.DatabaseRetentionDays);
}
```

## Testing
Created comprehensive unit tests in `DataRetentionTests.cs` to verify:
- Retention = 0 preserves all data
- Retention < 0 preserves all data  
- Retention > 0 only deletes old data
- Midnight rollover with retention = 0 doesn't delete data
- Proper logging of skip messages

## Impact
- **Before Fix**: Users lost all historical data every night when using the default setting
- **After Fix**: Historical data is properly preserved when retention is disabled (0 or negative)

## Prevention
1. Added unit tests to prevent regression
2. Added debug logging to track cleanup behavior
3. Consistent checks for `retentionDays > 0` before any cleanup operations

## User Action Required
Users who have lost data cannot recover it, but after updating to v1.2.0:
- Set `DatabaseRetentionDays = 0` in config to keep data forever
- Set `DatabaseRetentionDays = 90` (or any positive number) to auto-delete old data
- Historical data will now accumulate properly without being deleted
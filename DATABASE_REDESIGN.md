# Database-Driven Architecture Redesign

## Current Problems
1. **Complex rollover logic** - DataLoggerService has 200+ lines just for midnight/hourly rollovers
2. **Fragile timing** - App must be running at midnight or data is lost
3. **Infinite loop bugs** - Recursive calls during rollover causing crashes
4. **CPU waste** - Constant checking and updating of in-memory stats
5. **Data integrity risks** - Stats can be inconsistent if app crashes

## Proposed Solution: Let SQLite Do The Work!

### Core Principle
- **Write once, aggregate on read** - No rollovers needed!
- The app only needs to INSERT/UPDATE the current moment's data
- SQLite views handle all aggregation automatically
- Stats are ALWAYS correct, even if app crashes

### New Architecture

#### 1. Simplified Data Flow
```
User Input → Save to daily_stats/hourly_stats → Done!
UI Request → Query View → Get Aggregated Data
```

#### 2. SQL Views for Aggregation

```sql
-- Weekly stats view
CREATE VIEW IF NOT EXISTS weekly_stats AS
SELECT 
    date,
    key_count,
    mouse_distance,
    left_clicks,
    right_clicks,
    middle_clicks,
    scroll_distance,
    (left_clicks + right_clicks + middle_clicks) as total_clicks
FROM daily_stats
WHERE date >= date('now', '-7 days')
ORDER BY date;

-- Monthly stats view  
CREATE VIEW IF NOT EXISTS monthly_stats AS
SELECT 
    date,
    key_count,
    mouse_distance,
    left_clicks,
    right_clicks,
    middle_clicks,
    scroll_distance,
    (left_clicks + right_clicks + middle_clicks) as total_clicks
FROM daily_stats
WHERE date >= date('now', 'start of month')
ORDER BY date;

-- Lifetime stats view
CREATE VIEW IF NOT EXISTS lifetime_stats AS
SELECT 
    SUM(key_count) as total_keys,
    SUM(mouse_distance) as total_mouse_distance,
    SUM(left_clicks) as total_left_clicks,
    SUM(right_clicks) as total_right_clicks,
    SUM(middle_clicks) as total_middle_clicks,
    SUM(scroll_distance) as total_scroll_distance,
    MIN(date) as first_date,
    MAX(date) as last_date,
    COUNT(DISTINCT date) as total_days
FROM daily_stats;

-- Today's hourly breakdown
CREATE VIEW IF NOT EXISTS today_hourly_stats AS
SELECT 
    hour,
    key_count,
    mouse_distance,
    click_count,
    scroll_distance
FROM hourly_stats
WHERE date = date('now')
ORDER BY hour;
```

#### 3. Simplified DataLoggerService

Instead of complex rollover logic:

```csharp
public void LogKeyPress(string keyCode)
{
    var today = DateTime.Today.ToString("yyyy-MM-dd");
    var hour = DateTime.Now.Hour;
    
    // Just increment the database directly
    await _databaseService.IncrementStatsAsync(today, hour, 
        keyCount: 1);
    
    // That's it! No rollover needed!
}
```

#### 4. Auto-create new day/hour records with UPSERT

```sql
INSERT INTO daily_stats (date, key_count) 
VALUES (@date, 1)
ON CONFLICT(date) DO UPDATE SET 
    key_count = key_count + 1;

INSERT INTO hourly_stats (date, hour, key_count)
VALUES (@date, @hour, 1)  
ON CONFLICT(date, hour) DO UPDATE SET
    key_count = key_count + 1;
```

### Benefits

1. **No rollover logic needed** - Database handles everything
2. **Always accurate** - Views aggregate in real-time
3. **Crash-proof** - No in-memory state to lose
4. **Less CPU** - No constant date checking
5. **Simpler code** - Remove 300+ lines of rollover logic
6. **Works offline** - Stats continue working even if app restarts

### Migration Path

1. Add the views to DatabaseService.CreateTablesAsync()
2. Create IncrementStatsAsync methods using UPSERT
3. Simplify DataLoggerService to just call database methods
4. Remove all rollover timers and logic
5. Update queries to use views instead of manual aggregation

### Performance Considerations

- SQLite views are very fast for these simple aggregations
- Indexes on `date` column ensure quick lookups
- WAL mode allows concurrent reads during writes
- Much less CPU than current timer-based approach

## Implementation Priority

1. **Phase 1**: Add views, keep existing logic (safe rollout)
2. **Phase 2**: Switch read operations to use views
3. **Phase 3**: Simplify write operations to use UPSERT
4. **Phase 4**: Remove rollover logic entirely

This is a much cleaner, more reliable architecture!
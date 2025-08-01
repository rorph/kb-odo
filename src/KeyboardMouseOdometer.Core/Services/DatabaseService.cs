using KeyboardMouseOdometer.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace KeyboardMouseOdometer.Core.Services;

/// <summary>
/// SQLite database service implementing the PROJECT_SPEC database schema
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseService(ILogger<DatabaseService> logger, string databasePath = "odometer.db")
    {
        _logger = logger;
        _connectionString = $"Data Source={databasePath}";
    }

    /// <summary>
    /// Initialize database and create tables if they don't exist
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();

            await CreateTablesAsync();
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    private async Task CreateTablesAsync()
    {
        // Create daily_stats table as per PROJECT_SPEC schema
        var createDailyStatsTable = @"
            CREATE TABLE IF NOT EXISTS daily_stats (
                date TEXT PRIMARY KEY,
                key_count INTEGER DEFAULT 0,
                mouse_distance REAL DEFAULT 0,
                left_clicks INTEGER DEFAULT 0,
                right_clicks INTEGER DEFAULT 0,
                middle_clicks INTEGER DEFAULT 0,
                scroll_distance REAL DEFAULT 0
            )";

        // Create hourly_stats table for intra-day charts
        var createHourlyStatsTable = @"
            CREATE TABLE IF NOT EXISTS hourly_stats (
                date TEXT,
                hour INTEGER,
                key_count INTEGER DEFAULT 0,
                mouse_distance REAL DEFAULT 0,
                left_clicks INTEGER DEFAULT 0,
                right_clicks INTEGER DEFAULT 0,
                middle_clicks INTEGER DEFAULT 0,
                scroll_distance REAL DEFAULT 0,
                PRIMARY KEY (date, hour)
            )";

        // Create optional raw events table for debugging/analytics
        var createRawEventsTable = @"
            CREATE TABLE IF NOT EXISTS key_mouse_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                event_type TEXT,
                key TEXT,
                mouse_dx REAL,
                mouse_dy REAL,
                mouse_button TEXT,
                wheel_delta INTEGER
            )";

        using var command = _connection!.CreateCommand();
        
        command.CommandText = createDailyStatsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createHourlyStatsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createRawEventsTable;
        await command.ExecuteNonQueryAsync();

        // Handle all database migrations
        await RunDatabaseMigrationsAsync();
    }

    /// <summary>
    /// Run all database migrations to ensure schema consistency
    /// </summary>
    private async Task RunDatabaseMigrationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting database schema migrations");

            // Migration 1: Add scroll_distance to daily_stats (legacy migration)
            await MigrateDailyStatsScrollDistanceAsync();

            // Migration 2: Add scroll_distance to hourly_stats
            await MigrateHourlyStatsScrollDistanceAsync();

            // Migration 3: Add missing columns to key_mouse_events
            await MigrateKeyMouseEventsColumnsAsync();

            // Validate schema consistency after migrations
            await ValidateSchemaConsistencyAsync();

            _logger.LogInformation("All database migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database migrations");
            throw;
        }
    }

    /// <summary>
    /// Migration: Add scroll_distance column to daily_stats table if it doesn't exist
    /// </summary>
    private async Task MigrateDailyStatsScrollDistanceAsync()
    {
        try
        {
            if (!await ColumnExistsAsync("daily_stats", "scroll_distance"))
            {
                if (await ColumnExistsAsync("daily_stats", "scroll_count"))
                {
                    // Migrate from scroll_count to scroll_distance
                    await AddColumnAsync("daily_stats", "scroll_distance", "REAL DEFAULT 0");
                    await ExecuteSqlAsync("UPDATE daily_stats SET scroll_distance = scroll_count * 0.045");
                    _logger.LogInformation("Migrated scroll_count to scroll_distance in daily_stats");
                }
                else
                {
                    // Fresh installation, just add scroll_distance column
                    await AddColumnAsync("daily_stats", "scroll_distance", "REAL DEFAULT 0");
                    _logger.LogInformation("Added scroll_distance column to daily_stats table");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate daily_stats scroll_distance column");
            throw;
        }
    }

    /// <summary>
    /// Migration: Add scroll_distance column to hourly_stats table if it doesn't exist
    /// </summary>
    private async Task MigrateHourlyStatsScrollDistanceAsync()
    {
        try
        {
            if (!await ColumnExistsAsync("hourly_stats", "scroll_distance"))
            {
                if (await ColumnExistsAsync("hourly_stats", "scroll_count"))
                {
                    // Migrate from scroll_count to scroll_distance
                    await AddColumnAsync("hourly_stats", "scroll_distance", "REAL DEFAULT 0");
                    await ExecuteSqlAsync("UPDATE hourly_stats SET scroll_distance = scroll_count * 0.045");
                    _logger.LogInformation("Migrated scroll_count to scroll_distance in hourly_stats");
                }
                else
                {
                    // Fresh installation, just add scroll_distance column
                    await AddColumnAsync("hourly_stats", "scroll_distance", "REAL DEFAULT 0");
                    _logger.LogInformation("Added scroll_distance column to hourly_stats table");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate hourly_stats scroll_distance column");
            throw;
        }
    }

    /// <summary>
    /// Migration: Add missing columns to key_mouse_events table
    /// </summary>
    private async Task MigrateKeyMouseEventsColumnsAsync()
    {
        try
        {
            bool migrationNeeded = false;

            if (!await ColumnExistsAsync("key_mouse_events", "mouse_button"))
            {
                await AddColumnAsync("key_mouse_events", "mouse_button", "TEXT");
                _logger.LogInformation("Added mouse_button column to key_mouse_events table");
                migrationNeeded = true;
            }

            if (!await ColumnExistsAsync("key_mouse_events", "wheel_delta"))
            {
                await AddColumnAsync("key_mouse_events", "wheel_delta", "INTEGER");
                _logger.LogInformation("Added wheel_delta column to key_mouse_events table");
                migrationNeeded = true;
            }

            if (migrationNeeded)
            {
                _logger.LogInformation("key_mouse_events table schema updated successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate key_mouse_events table columns");
            throw;
        }
    }

    /// <summary>
    /// Check if a column exists in a table
    /// </summary>
    private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM pragma_table_info(@tableName) 
            WHERE name = @columnName";

        using var command = _connection!.CreateCommand();
        command.CommandText = sql.Replace("@tableName", $"'{tableName}'");
        command.Parameters.AddWithValue("@columnName", columnName);
        
        var result = await command.ExecuteScalarAsync();
        return (long)(result ?? 0L) > 0;
    }

    /// <summary>
    /// Add a column to a table if it doesn't exist
    /// </summary>
    private async Task AddColumnAsync(string tableName, string columnName, string columnDefinition)
    {
        var sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
        await ExecuteSqlAsync(sql);
    }

    /// <summary>
    /// Execute a SQL command
    /// </summary>
    private async Task ExecuteSqlAsync(string sql)
    {
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Validate that all tables have consistent schemas after migration
    /// </summary>
    private async Task ValidateSchemaConsistencyAsync()
    {
        try
        {
            _logger.LogInformation("Validating database schema consistency");

            var expectedDailyStatsColumns = new[] { "date", "key_count", "mouse_distance", "left_clicks", "right_clicks", "middle_clicks", "scroll_distance" };
            var expectedHourlyStatsColumns = new[] { "date", "hour", "key_count", "mouse_distance", "left_clicks", "right_clicks", "middle_clicks", "scroll_distance" };
            var expectedKeyMouseEventsColumns = new[] { "id", "timestamp", "event_type", "key", "mouse_dx", "mouse_dy", "mouse_button", "wheel_delta" };

            await ValidateTableColumnsAsync("daily_stats", expectedDailyStatsColumns);
            await ValidateTableColumnsAsync("hourly_stats", expectedHourlyStatsColumns);
            await ValidateTableColumnsAsync("key_mouse_events", expectedKeyMouseEventsColumns);

            _logger.LogInformation("Database schema validation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Schema validation completed with warnings - this may affect summary generation");
        }
    }

    /// <summary>
    /// Validate that a table has all expected columns
    /// </summary>
    private async Task ValidateTableColumnsAsync(string tableName, string[] expectedColumns)
    {
        foreach (var column in expectedColumns)
        {
            if (!await ColumnExistsAsync(tableName, column))
            {
                var error = $"Missing column '{column}' in table '{tableName}' - this will cause summary failures";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }
        }

        _logger.LogDebug("Table '{TableName}' has all expected columns", tableName);
    }

    /// <summary>
    /// Save or update daily statistics
    /// </summary>
    public async Task SaveDailyStatsAsync(DailyStats stats)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var sql = @"
                INSERT OR REPLACE INTO daily_stats 
                (date, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance)
                VALUES (@date, @keyCount, @mouseDistance, @leftClicks, @rightClicks, @middleClicks, @scrollDistance)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@date", stats.Date);
            command.Parameters.AddWithValue("@keyCount", stats.KeyCount);
            command.Parameters.AddWithValue("@mouseDistance", stats.MouseDistance);
            command.Parameters.AddWithValue("@leftClicks", stats.LeftClicks);
            command.Parameters.AddWithValue("@rightClicks", stats.RightClicks);
            command.Parameters.AddWithValue("@middleClicks", stats.MiddleClicks);
            command.Parameters.AddWithValue("@scrollDistance", stats.ScrollDistance);

            await command.ExecuteNonQueryAsync();
            _logger.LogDebug("Saved daily stats for {Date}", stats.Date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save daily stats for {Date}", stats.Date);
            throw;
        }
    }

    /// <summary>
    /// Get daily statistics for a specific date
    /// </summary>
    public async Task<DailyStats?> GetDailyStatsAsync(string date)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var sql = @"
                SELECT date, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance
                FROM daily_stats 
                WHERE date = @date";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@date", date);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new DailyStats
                {
                    Date = reader.GetString(0),
                    KeyCount = reader.GetInt32(1),
                    MouseDistance = reader.GetDouble(2),
                    LeftClicks = reader.GetInt32(3),
                    RightClicks = reader.GetInt32(4),
                    MiddleClicks = reader.GetInt32(5),
                    ScrollDistance = reader.GetDouble(6)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily stats for {Date}", date);
            throw;
        }
    }

    /// <summary>
    /// Get daily statistics for a date range
    /// </summary>
    public async Task<List<DailyStats>> GetDailyStatsRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var results = new List<DailyStats>();

        try
        {
            var sql = @"
                SELECT date, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance
                FROM daily_stats 
                WHERE date >= @startDate AND date <= @endDate
                ORDER BY date";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DailyStats
                {
                    Date = reader.GetString(0),
                    KeyCount = reader.GetInt32(1),
                    MouseDistance = reader.GetDouble(2),
                    LeftClicks = reader.GetInt32(3),
                    RightClicks = reader.GetInt32(4),
                    MiddleClicks = reader.GetInt32(5),
                    ScrollDistance = reader.GetDouble(6)
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily stats range from {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// Save multiple raw key/mouse events in a batch (more efficient than individual saves)
    /// </summary>
    public async Task SaveKeyMouseEventsBatchAsync(IEnumerable<KeyMouseEvent> events)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var eventList = events.ToList();
        if (!eventList.Any()) return;

        try
        {
            using var transaction = _connection.BeginTransaction();
            
            var sql = @"
                INSERT INTO key_mouse_events 
                (timestamp, event_type, key, mouse_dx, mouse_dy, mouse_button, wheel_delta)
                VALUES (@timestamp, @eventType, @key, @mouseDx, @mouseDy, @mouseButton, @wheelDelta)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;

            foreach (var keyMouseEvent in eventList)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@timestamp", keyMouseEvent.Timestamp);
                command.Parameters.AddWithValue("@eventType", keyMouseEvent.EventType);
                command.Parameters.AddWithValue("@key", keyMouseEvent.Key ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@mouseDx", keyMouseEvent.MouseDx ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@mouseDy", keyMouseEvent.MouseDy ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@mouseButton", keyMouseEvent.MouseButton ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@wheelDelta", keyMouseEvent.WheelDelta ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            _logger.LogDebug("Saved batch of {Count} key/mouse events", eventList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch of key/mouse events");
            throw;
        }
    }

    /// <summary>
    /// Save raw key/mouse event (optional detailed logging)
    /// </summary>
    public async Task SaveKeyMouseEventAsync(KeyMouseEvent keyMouseEvent)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var sql = @"
                INSERT INTO key_mouse_events 
                (timestamp, event_type, key, mouse_dx, mouse_dy, mouse_button, wheel_delta)
                VALUES (@timestamp, @eventType, @key, @mouseDx, @mouseDy, @mouseButton, @wheelDelta)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@timestamp", keyMouseEvent.Timestamp);
            command.Parameters.AddWithValue("@eventType", keyMouseEvent.EventType);
            command.Parameters.AddWithValue("@key", keyMouseEvent.Key ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@mouseDx", keyMouseEvent.MouseDx ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@mouseDy", keyMouseEvent.MouseDy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@mouseButton", keyMouseEvent.MouseButton ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@wheelDelta", keyMouseEvent.WheelDelta ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
            _logger.LogTrace("Saved single key/mouse event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save key/mouse event");
            throw;
        }
    }

    /// <summary>
    /// Clean up old data based on retention days
    /// </summary>
    public async Task CleanupOldDataAsync(int retentionDays)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var cutoffDate = DateTime.Today.AddDays(-retentionDays).ToString("yyyy-MM-dd");

            // Clean daily stats
            var dailyStatsSql = "DELETE FROM daily_stats WHERE date < @cutoffDate";
            using var dailyCommand = _connection.CreateCommand();
            dailyCommand.CommandText = dailyStatsSql;
            dailyCommand.Parameters.AddWithValue("@cutoffDate", cutoffDate);
            var dailyDeleted = await dailyCommand.ExecuteNonQueryAsync();

            // Clean hourly stats
            var hourlyStatsSql = "DELETE FROM hourly_stats WHERE date < @cutoffDate";
            using var hourlyCommand = _connection.CreateCommand();
            hourlyCommand.CommandText = hourlyStatsSql;
            hourlyCommand.Parameters.AddWithValue("@cutoffDate", cutoffDate);
            var hourlyDeleted = await hourlyCommand.ExecuteNonQueryAsync();

            // Clean raw events
            var eventsSql = "DELETE FROM key_mouse_events WHERE date(timestamp) < @cutoffDate";
            using var eventsCommand = _connection.CreateCommand();
            eventsCommand.CommandText = eventsSql;
            eventsCommand.Parameters.AddWithValue("@cutoffDate", cutoffDate);
            var eventsDeleted = await eventsCommand.ExecuteNonQueryAsync();

            _logger.LogInformation("Cleaned up {DailyDeleted} daily stats, {HourlyDeleted} hourly stats, and {EventsDeleted} events older than {RetentionDays} days", 
                dailyDeleted, hourlyDeleted, eventsDeleted, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old data");
            throw;
        }
    }

    /// <summary>
    /// Save or update hourly statistics
    /// </summary>
    public async Task SaveHourlyStatsAsync(string date, int hour, DailyStats stats)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var sql = @"
                INSERT OR REPLACE INTO hourly_stats 
                (date, hour, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance)
                VALUES (@date, @hour, @keyCount, @mouseDistance, @leftClicks, @rightClicks, @middleClicks, @scrollDistance)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@date", date);
            command.Parameters.AddWithValue("@hour", hour);
            command.Parameters.AddWithValue("@keyCount", stats.KeyCount);
            command.Parameters.AddWithValue("@mouseDistance", stats.MouseDistance);
            command.Parameters.AddWithValue("@leftClicks", stats.LeftClicks);
            command.Parameters.AddWithValue("@rightClicks", stats.RightClicks);
            command.Parameters.AddWithValue("@middleClicks", stats.MiddleClicks);
            command.Parameters.AddWithValue("@scrollDistance", stats.ScrollDistance);

            await command.ExecuteNonQueryAsync();
            _logger.LogDebug("Saved hourly stats for {Date} hour {Hour}", date, hour);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save hourly stats for {Date} hour {Hour}", date, hour);
            throw;
        }
    }

    /// <summary>
    /// Get hourly statistics for a specific date
    /// </summary>
    public async Task<List<HourlyStats>> GetHourlyStatsAsync(string date)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var results = new List<HourlyStats>();

        try
        {
            var sql = @"
                SELECT date, hour, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance
                FROM hourly_stats 
                WHERE date = @date
                ORDER BY hour";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@date", date);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new HourlyStats
                {
                    Date = reader.GetString(0),
                    Hour = reader.GetInt32(1),
                    KeyCount = reader.GetInt32(2),
                    MouseDistance = reader.GetDouble(3),
                    LeftClicks = reader.GetInt32(4),
                    RightClicks = reader.GetInt32(5),
                    MiddleClicks = reader.GetInt32(6),
                    ScrollDistance = reader.GetDouble(7)
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hourly stats for {Date}", date);
            throw;
        }
    }

    /// <summary>
    /// Get lifetime totals across all daily statistics
    /// </summary>
    public async Task<LifetimeStats> GetLifetimeStatsAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var sql = @"
                SELECT 
                    SUM(key_count) as total_keys,
                    SUM(mouse_distance) as total_mouse_distance,
                    SUM(left_clicks) as total_left_clicks,
                    SUM(right_clicks) as total_right_clicks,
                    SUM(middle_clicks) as total_middle_clicks,
                    SUM(scroll_distance) as total_scroll_distance,
                    MIN(date) as first_date,
                    MAX(date) as last_date,
                    COUNT(*) as total_days
                FROM daily_stats 
                WHERE key_count > 0 OR mouse_distance > 0 OR left_clicks > 0 OR right_clicks > 0 OR middle_clicks > 0";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new LifetimeStats
                {
                    TotalKeys = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                    TotalMouseDistance = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1),
                    TotalLeftClicks = reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                    TotalRightClicks = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                    TotalMiddleClicks = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                    TotalScrollDistance = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5),
                    FirstDate = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LastDate = reader.IsDBNull(7) ? null : reader.GetString(7),
                    TotalDays = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                };
            }

            // Return empty stats if no data found
            return new LifetimeStats
            {
                TotalKeys = 0,
                TotalMouseDistance = 0.0,
                TotalLeftClicks = 0,
                TotalRightClicks = 0,
                TotalMiddleClicks = 0,
                TotalScrollDistance = 0.0,
                FirstDate = null,
                LastDate = null,
                TotalDays = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lifetime statistics");
            throw;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
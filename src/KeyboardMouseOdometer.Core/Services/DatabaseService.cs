using KeyboardMouseOdometer.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

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
        // Configure connection string with options to prevent file locking issues
        if (databasePath == ":memory:")
        {
            _connectionString = $"Data Source={databasePath}";
        }
        else
        {
            // For file-based databases, use Mode=ReadWriteCreate and disable shared cache
            _connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate;Cache=Private";
        }
    }

    /// <summary>
    /// Initialize database and create tables if they don't exist
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database with connection: {ConnectionString}", _connectionString);
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
            _logger.LogDebug("Database connection opened successfully");

            // Enable WAL mode for better reliability and concurrent access
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL";
                var walResult = await cmd.ExecuteScalarAsync();
                _logger.LogInformation("Database journal mode set to: {JournalMode}", walResult);
            }

            // Enable synchronous mode for data safety
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA synchronous=NORMAL";
                await cmd.ExecuteNonQueryAsync();
            }

            // Set busy timeout to handle concurrent access
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA busy_timeout=5000";
                await cmd.ExecuteNonQueryAsync();
            }

            await CreateTablesAsync();
            _logger.LogInformation("Database initialized successfully with WAL mode");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database with connection string: {ConnectionString}", _connectionString);
            throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
        }
    }

    private async Task CreateTablesAsync()
    {
        // Create schema_version table for database versioning
        var createVersionTable = @"
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER PRIMARY KEY,
                applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

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

        // Create key_stats table for individual key tracking (heatmap feature)
        var createKeyStatsTable = @"
            CREATE TABLE IF NOT EXISTS key_stats (
                date TEXT,
                hour INTEGER,
                key_code TEXT,
                count INTEGER DEFAULT 0,
                PRIMARY KEY (date, hour, key_code)
            )";

        // Create indexes for efficient queries
        var createKeyStatsIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_key_stats_date_key ON key_stats(date, key_code);
            CREATE INDEX IF NOT EXISTS idx_key_stats_key ON key_stats(key_code);";

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
        
        command.CommandText = createVersionTable;
        await command.ExecuteNonQueryAsync();
        
        command.CommandText = createDailyStatsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createHourlyStatsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createKeyStatsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createKeyStatsIndexes;
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

            // Get current database version
            var currentVersion = await GetDatabaseVersionAsync();
            _logger.LogInformation("Current database version: {Version}", currentVersion);

            // Migration 1: Add scroll_distance to daily_stats (legacy migration)
            if (currentVersion < 1)
            {
                await MigrateDailyStatsScrollDistanceAsync();
                await MigrateHourlyStatsScrollDistanceAsync();
                await MigrateKeyMouseEventsColumnsAsync();
                await SetDatabaseVersionAsync(1);
            }

            // Migration 2: Add key_stats table for heatmap feature (v2)
            if (currentVersion < 2)
            {
                await MigrateToVersion2Async();
                await SetDatabaseVersionAsync(2);
            }

            // Migration 3: Add aggregation views for simpler architecture
            if (currentVersion < 3)
            {
                await MigrateToVersion3Async();
                await SetDatabaseVersionAsync(3);
            }

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
    /// Get the current database schema version
    /// </summary>
    private async Task<int> GetDatabaseVersionAsync()
    {
        try
        {
            var sql = "SELECT MAX(version) FROM schema_version";
            using var command = _connection!.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result);
        }
        catch
        {
            // If table doesn't exist or error, assume version 0
            return 0;
        }
    }

    /// <summary>
    /// Set the database schema version
    /// </summary>
    private async Task SetDatabaseVersionAsync(int version)
    {
        var sql = "INSERT INTO schema_version (version) VALUES (@version)";
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@version", version);
        await command.ExecuteNonQueryAsync();
        _logger.LogInformation("Database version updated to {Version}", version);
    }

    /// <summary>
    /// Migration to version 2: Add key_stats table if it doesn't exist
    /// </summary>
    private async Task MigrateToVersion2Async()
    {
        try
        {
            _logger.LogInformation("Migrating database to version 2 (keyboard heatmap support)");
            
            // Check if key_stats table already exists
            var tableExists = await TableExistsAsync("key_stats");
            if (!tableExists)
            {
                var createKeyStatsTable = @"
                    CREATE TABLE key_stats (
                        date TEXT,
                        hour INTEGER,
                        key_code TEXT,
                        count INTEGER DEFAULT 0,
                        PRIMARY KEY (date, hour, key_code)
                    )";
                
                await ExecuteSqlAsync(createKeyStatsTable);
                
                // Create indexes
                await ExecuteSqlAsync("CREATE INDEX idx_key_stats_date_key ON key_stats(date, key_code)");
                await ExecuteSqlAsync("CREATE INDEX idx_key_stats_key ON key_stats(key_code)");
                
                _logger.LogInformation("Created key_stats table with indexes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate to version 2");
            throw;
        }
    }

    /// <summary>
    /// Migration to version 3: Add aggregation views for simpler architecture
    /// </summary>
    private async Task MigrateToVersion3Async()
    {
        try
        {
            _logger.LogInformation("Migrating database to version 3 (aggregation views)");
            
            // Drop existing views if they exist (to recreate with latest schema)
            await ExecuteSqlAsync("DROP VIEW IF EXISTS weekly_stats");
            await ExecuteSqlAsync("DROP VIEW IF EXISTS monthly_stats");
            await ExecuteSqlAsync("DROP VIEW IF EXISTS lifetime_stats_view");
            await ExecuteSqlAsync("DROP VIEW IF EXISTS today_hourly_stats");
            
            // Create weekly stats view
            var createWeeklyView = @"
                CREATE VIEW weekly_stats AS
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
                ORDER BY date";
            await ExecuteSqlAsync(createWeeklyView);
            
            // Create monthly stats view
            var createMonthlyView = @"
                CREATE VIEW monthly_stats AS
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
                ORDER BY date";
            await ExecuteSqlAsync(createMonthlyView);
            
            // Create lifetime stats view
            var createLifetimeView = @"
                CREATE VIEW lifetime_stats_view AS
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
                FROM daily_stats";
            await ExecuteSqlAsync(createLifetimeView);
            
            // Create today's hourly breakdown view
            var createTodayHourlyView = @"
                CREATE VIEW today_hourly_stats AS
                SELECT 
                    hour,
                    key_count,
                    mouse_distance,
                    click_count,
                    scroll_distance
                FROM hourly_stats
                WHERE date = date('now')
                ORDER BY hour";
            await ExecuteSqlAsync(createTodayHourlyView);
            
            _logger.LogInformation("Created aggregation views successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate to version 3");
            throw;
        }
    }
    
    /// <summary>
    /// Check if a table exists
    /// </summary>
    private async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync();
        return result != null;
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
    /// Increment stats atomically using UPSERT (INSERT ON CONFLICT UPDATE)
    /// This eliminates the need for complex rollover logic!
    /// </summary>
    public async Task IncrementStatsAsync(
        string date, 
        int hour,
        int keyCount = 0,
        double mouseDistance = 0,
        int leftClicks = 0,
        int rightClicks = 0,
        int middleClicks = 0,
        double scrollDistance = 0)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();
        try
        {
            // Update daily stats with UPSERT
            var dailySql = @"
                INSERT INTO daily_stats (date, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance)
                VALUES (@date, @keyCount, @mouseDistance, @leftClicks, @rightClicks, @middleClicks, @scrollDistance)
                ON CONFLICT(date) DO UPDATE SET
                    key_count = key_count + @keyCount,
                    mouse_distance = mouse_distance + @mouseDistance,
                    left_clicks = left_clicks + @leftClicks,
                    right_clicks = right_clicks + @rightClicks,
                    middle_clicks = middle_clicks + @middleClicks,
                    scroll_distance = scroll_distance + @scrollDistance";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = dailySql;
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@date", date);
                command.Parameters.AddWithValue("@keyCount", keyCount);
                command.Parameters.AddWithValue("@mouseDistance", mouseDistance);
                command.Parameters.AddWithValue("@leftClicks", leftClicks);
                command.Parameters.AddWithValue("@rightClicks", rightClicks);
                command.Parameters.AddWithValue("@middleClicks", middleClicks);
                command.Parameters.AddWithValue("@scrollDistance", scrollDistance);
                await command.ExecuteNonQueryAsync();
            }

            // Update hourly stats with UPSERT
            var hourlySql = @"
                INSERT INTO hourly_stats (date, hour, key_count, mouse_distance, click_count, scroll_distance)
                VALUES (@date, @hour, @keyCount, @mouseDistance, @clickCount, @scrollDistance)
                ON CONFLICT(date, hour) DO UPDATE SET
                    key_count = key_count + @keyCount,
                    mouse_distance = mouse_distance + @mouseDistance,
                    click_count = click_count + @clickCount,
                    scroll_distance = scroll_distance + @scrollDistance";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = hourlySql;
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@date", date);
                command.Parameters.AddWithValue("@hour", hour);
                command.Parameters.AddWithValue("@keyCount", keyCount);
                command.Parameters.AddWithValue("@mouseDistance", mouseDistance);
                command.Parameters.AddWithValue("@clickCount", leftClicks + rightClicks + middleClicks);
                command.Parameters.AddWithValue("@scrollDistance", scrollDistance);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to increment stats for {Date} hour {Hour}", date, hour);
            throw;
        }
    }

    /// <summary>
    /// Increment key stats for heatmap using UPSERT
    /// </summary>
    public async Task IncrementKeyStatsAsync(string date, int hour, string keyIdentifier, int count = 1)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            var sql = @"
                INSERT INTO key_stats (date, hour, key_code, count)
                VALUES (@date, @hour, @keyIdentifier, @count)
                ON CONFLICT(date, hour, key_code) DO UPDATE SET
                    count = count + @count";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@date", date);
            command.Parameters.AddWithValue("@hour", hour);
            command.Parameters.AddWithValue("@keyIdentifier", keyIdentifier);
            command.Parameters.AddWithValue("@count", count);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment key stats for {Key}", keyIdentifier);
            throw;
        }
    }

    /// <summary>
    /// Save or update daily statistics with transaction support
    /// </summary>
    public async Task SaveDailyStatsAsync(DailyStats stats)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();
        try
        {
            var sql = @"
                INSERT OR REPLACE INTO daily_stats 
                (date, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance)
                VALUES (@date, @keyCount, @mouseDistance, @leftClicks, @rightClicks, @middleClicks, @scrollDistance)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.Parameters.AddWithValue("@date", stats.Date);
            command.Parameters.AddWithValue("@keyCount", stats.KeyCount);
            command.Parameters.AddWithValue("@mouseDistance", stats.MouseDistance);
            command.Parameters.AddWithValue("@leftClicks", stats.LeftClicks);
            command.Parameters.AddWithValue("@rightClicks", stats.RightClicks);
            command.Parameters.AddWithValue("@middleClicks", stats.MiddleClicks);
            command.Parameters.AddWithValue("@scrollDistance", stats.ScrollDistance);

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            _logger.LogDebug("Saved daily stats for {Date}", stats.Date);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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
            using var transaction = (SqliteTransaction)_connection.BeginTransaction();
            
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
    /// Save or update hourly statistics with transaction support
    /// </summary>
    public async Task SaveHourlyStatsAsync(string date, int hour, DailyStats stats)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();
        try
        {
            var sql = @"
                INSERT OR REPLACE INTO hourly_stats 
                (date, hour, key_count, mouse_distance, left_clicks, right_clicks, middle_clicks, scroll_distance)
                VALUES (@date, @hour, @keyCount, @mouseDistance, @leftClicks, @rightClicks, @middleClicks, @scrollDistance)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.Parameters.AddWithValue("@date", date);
            command.Parameters.AddWithValue("@hour", hour);
            command.Parameters.AddWithValue("@keyCount", stats.KeyCount);
            command.Parameters.AddWithValue("@mouseDistance", stats.MouseDistance);
            command.Parameters.AddWithValue("@leftClicks", stats.LeftClicks);
            command.Parameters.AddWithValue("@rightClicks", stats.RightClicks);
            command.Parameters.AddWithValue("@middleClicks", stats.MiddleClicks);
            command.Parameters.AddWithValue("@scrollDistance", stats.ScrollDistance);

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            _logger.LogDebug("Saved hourly stats for {Date} hour {Hour}", date, hour);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to save hourly stats for {Date} hour {Hour}", date, hour);
            throw;
        }
    }

    /// <summary>
    /// Save or update hourly statistics using HourlyStats object
    /// </summary>
    public async Task SaveHourlyStatsAsync(HourlyStats hourlyStats)
    {
        var dailyStatsFormat = new DailyStats
        {
            Date = hourlyStats.Date,
            KeyCount = hourlyStats.KeyCount,
            MouseDistance = hourlyStats.MouseDistance,
            LeftClicks = hourlyStats.LeftClicks,
            RightClicks = hourlyStats.RightClicks,
            MiddleClicks = hourlyStats.MiddleClicks,
            ScrollDistance = hourlyStats.ScrollDistance
        };

        await SaveHourlyStatsAsync(hourlyStats.Date, hourlyStats.Hour, dailyStatsFormat);
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

    /// <summary>
    /// Save or update key statistics (for heatmap feature)
    /// </summary>
    public async Task SaveKeyStatsAsync(string date, int hour, Dictionary<string, int> keyCounts)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        if (!keyCounts.Any()) return;

        try
        {
            using var transaction = (SqliteTransaction)_connection.BeginTransaction();
            
            var sql = @"
                INSERT OR REPLACE INTO key_stats 
                (date, hour, key_code, count)
                VALUES (@date, @hour, @keyCode, @count)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;

            foreach (var kvp in keyCounts)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@date", date);
                command.Parameters.AddWithValue("@hour", hour);
                command.Parameters.AddWithValue("@keyCode", kvp.Key);
                command.Parameters.AddWithValue("@count", kvp.Value);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            _logger.LogDebug("Saved key stats for {Date} hour {Hour} ({Count} keys)", date, hour, keyCounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save key stats for {Date} hour {Hour}", date, hour);
            throw;
        }
    }

    /// <summary>
    /// Save multiple key statistics in batch (for heatmap feature)
    /// </summary>
    public async Task SaveKeyStatsBatchAsync(List<KeyStats> keyStatsList)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        if (!keyStatsList.Any()) return;

        try
        {
            using var transaction = (SqliteTransaction)_connection.BeginTransaction();
            
            var sql = @"
                INSERT OR REPLACE INTO key_stats 
                (date, hour, key_code, count)
                VALUES (@date, @hour, @keyCode, @count)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;

            foreach (var keyStats in keyStatsList)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@date", keyStats.Date);
                command.Parameters.AddWithValue("@hour", keyStats.Hour);
                command.Parameters.AddWithValue("@keyCode", keyStats.KeyCode);
                command.Parameters.AddWithValue("@count", keyStats.Count);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            _logger.LogDebug("Batch saved {Count} key stats entries", keyStatsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch save key stats ({Count} entries)", keyStatsList.Count);
            throw;
        }
    }

    /// <summary>
    /// Get key statistics for a specific date and hour
    /// </summary>
    public async Task<Dictionary<string, int>> GetKeyStatsAsync(string date, int hour)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var results = new Dictionary<string, int>();

        try
        {
            var sql = @"
                SELECT key_code, count
                FROM key_stats 
                WHERE date = @date AND hour = @hour";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@date", date);
            command.Parameters.AddWithValue("@hour", hour);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var keyIdentifier = reader.GetString(0);
                var count = reader.GetInt32(1);
                results[keyIdentifier] = count;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get key stats for {Date} hour {Hour}", date, hour);
            throw;
        }
    }

    /// <summary>
    /// Get key statistics for a specific date range
    /// </summary>
    public async Task<Dictionary<string, long>> GetKeyStatsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var results = new Dictionary<string, long>();

        try
        {
            var sql = @"
                SELECT key_code, SUM(count) as total_count
                FROM key_stats 
                WHERE date >= @startDate AND date <= @endDate
                GROUP BY key_code
                ORDER BY total_count DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var keyCode = reader.GetString(0);
                var count = reader.GetInt64(1);
                results[keyCode] = count;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get key stats for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// Get key statistics for today
    /// </summary>
    public async Task<Dictionary<string, long>> GetTodayKeyStatsAsync()
    {
        var today = DateTime.Today;
        return await GetKeyStatsByDateRangeAsync(today, today);
    }

    /// <summary>
    /// Get key statistics for the last 7 days
    /// </summary>
    public async Task<Dictionary<string, long>> GetWeeklyKeyStatsAsync()
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-6);
        return await GetKeyStatsByDateRangeAsync(startDate, endDate);
    }

    /// <summary>
    /// Get key statistics for the last 30 days
    /// </summary>
    public async Task<Dictionary<string, long>> GetMonthlyKeyStatsAsync()
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-29);
        return await GetKeyStatsByDateRangeAsync(startDate, endDate);
    }

    /// <summary>
    /// Get database connection for advanced operations (testing use)
    /// </summary>
    public async Task<SqliteConnection> GetConnectionAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        // Return the existing connection, ensuring it's open
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }
        
        return _connection;
    }

    /// <summary>
    /// Get lifetime key statistics
    /// </summary>
    public async Task<Dictionary<string, long>> GetLifetimeKeyStatsAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var results = new Dictionary<string, long>();

        try
        {
            var sql = @"
                SELECT key_code, SUM(count) as total_count
                FROM key_stats 
                GROUP BY key_code
                ORDER BY total_count DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var keyCode = reader.GetString(0);
                var count = reader.GetInt64(1);
                results[keyCode] = count;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lifetime key stats");
            throw;
        }
    }

    /// <summary>
    /// Get the top N most used keys for a date range
    /// </summary>
    public async Task<List<(string KeyCode, long Count)>> GetTopKeysAsync(DateTime startDate, DateTime endDate, int topN = 10)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var results = new List<(string KeyCode, long Count)>();

        try
        {
            var sql = @"
                SELECT key_code, SUM(count) as total_count
                FROM key_stats 
                WHERE date >= @startDate AND date <= @endDate
                GROUP BY key_code
                ORDER BY total_count DESC
                LIMIT @limit";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@limit", topN);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var keyCode = reader.GetString(0);
                var count = reader.GetInt64(1);
                results.Add((keyCode, count));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top {TopN} keys for date range", topN);
            throw;
        }
    }

    /// <summary>
    /// Create a backup of the database
    /// </summary>
    public async Task<bool> CreateBackupAsync(string backupPath)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            // Ensure backup directory exists
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir))
                Directory.CreateDirectory(backupDir);

            // Use SQLite VACUUM INTO command for safe backup
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"VACUUM INTO '{backupPath}'";
            await cmd.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Database backup created at {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup at {BackupPath}", backupPath);
            return false;
        }
    }

    /// <summary>
    /// Perform database integrity check
    /// </summary>
    public async Task<bool> CheckIntegrityAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check";
            var result = await cmd.ExecuteScalarAsync();
            
            var isOk = result?.ToString() == "ok";
            if (!isOk)
            {
                _logger.LogWarning("Database integrity check failed: {Result}", result);
            }
            else
            {
                _logger.LogDebug("Database integrity check passed");
            }
            
            return isOk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform database integrity check");
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_connection != null)
            {
                // Ensure any pending transactions are completed
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    // Execute PRAGMA optimize before closing
                    try
                    {
                        using var cmd = _connection.CreateCommand();
                        cmd.CommandText = "PRAGMA optimize";
                        cmd.ExecuteNonQuery();
                    }
                    catch { /* Ignore errors during optimization */ }
                    
                    // Close the connection properly
                    _connection.Close();
                }
                
                // Dispose the connection
                _connection.Dispose();
                _connection = null;
            }
            
            // Force SQLite to release all resources
            SqliteConnection.ClearAllPools();
            
            // Trigger garbage collection to ensure finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect(); // Second collection to handle any finalizers that created new objects
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Warning during database service disposal");
        }
    }
}
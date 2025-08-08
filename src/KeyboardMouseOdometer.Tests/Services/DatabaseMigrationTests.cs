using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class DatabaseMigrationTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseService _databaseService;
    private readonly Mock<ILogger<DatabaseService>> _loggerMock;

    public DatabaseMigrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_migration_{Guid.NewGuid()}.db");
        _loggerMock = new Mock<ILogger<DatabaseService>>();
        _databaseService = new DatabaseService(_loggerMock.Object, _testDbPath);
    }

    [Fact]
    public async Task InitializeAsync_CreatesSchemaVersionTable()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert - Check that schema_version table exists
        var tableExists = await TableExistsAsync("schema_version");
        Assert.True(tableExists, "schema_version table should be created");
    }

    [Fact]
    public async Task InitializeAsync_CreatesKeyStatsTable()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert - Check that key_stats table exists
        var tableExists = await TableExistsAsync("key_stats");
        Assert.True(tableExists, "key_stats table should be created");
    }

    [Fact]
    public async Task InitializeAsync_CreatesKeyStatsIndexes()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert - Check that indexes exist
        var indexExists1 = await IndexExistsAsync("idx_key_stats_date_key");
        var indexExists2 = await IndexExistsAsync("idx_key_stats_key");
        
        Assert.True(indexExists1, "idx_key_stats_date_key index should be created");
        Assert.True(indexExists2, "idx_key_stats_key index should be created");
    }

    [Fact]
    public async Task InitializeAsync_SetsCorrectDatabaseVersion()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert - Database should be at version 3 (includes aggregation views)
        var version = await GetDatabaseVersionAsync();
        Assert.Equal(3, version);
    }

    [Fact]
    public async Task InitializeAsync_CreatesAggregationViews()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert - Check that all aggregation views exist
        var weeklyViewExists = await ViewExistsAsync("weekly_stats");
        var monthlyViewExists = await ViewExistsAsync("monthly_stats");
        var lifetimeViewExists = await ViewExistsAsync("lifetime_stats_view");
        var todayHourlyViewExists = await ViewExistsAsync("today_hourly_stats");
        
        Assert.True(weeklyViewExists, "weekly_stats view should be created");
        Assert.True(monthlyViewExists, "monthly_stats view should be created");
        Assert.True(lifetimeViewExists, "lifetime_stats_view should be created");
        Assert.True(todayHourlyViewExists, "today_hourly_stats view should be created");
    }

    [Fact]
    public async Task SaveKeyStatsAsync_StoresDataCorrectly()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = 10;
        var keyStats = new Dictionary<string, int>
        {
            { "A", 100 },
            { "Space", 50 },
            { "Enter", 25 }
        };

        // Act
        await _databaseService.SaveKeyStatsAsync(date, hour, keyStats);

        // Assert - Retrieve and verify
        var retrievedStats = await _databaseService.GetTodayKeyStatsAsync();
        Assert.NotNull(retrievedStats);
        Assert.Equal(100, retrievedStats["A"]);
        Assert.Equal(50, retrievedStats["Space"]);
        Assert.Equal(25, retrievedStats["Enter"]);
    }

    [Fact]
    public async Task GetTodayKeyStatsAsync_ReturnsCorrectData()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var keyStats = new Dictionary<string, int>
        {
            { "A", 200 },
            { "B", 150 }
        };
        await _databaseService.SaveKeyStatsAsync(today, 12, keyStats);

        // Act
        var result = await _databaseService.GetTodayKeyStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(200, result["A"]);
        Assert.Equal(150, result["B"]);
    }

    [Fact]
    public async Task GetWeeklyKeyStatsAsync_AggregatesCorrectly()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var today = DateTime.Today;
        
        // Add data for multiple days
        for (int i = 0; i < 7; i++)
        {
            var date = today.AddDays(-i).ToString("yyyy-MM-dd");
            var keyStats = new Dictionary<string, int>
            {
                { "A", 10 },
                { "B", 5 }
            };
            await _databaseService.SaveKeyStatsAsync(date, 12, keyStats);
        }

        // Act
        var result = await _databaseService.GetWeeklyKeyStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(70, result["A"]); // 10 * 7 days
        Assert.Equal(35, result["B"]); // 5 * 7 days
    }

    [Fact]
    public async Task GetTopKeysAsync_ReturnsTopNKeys()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var keyStats = new Dictionary<string, int>
        {
            { "A", 500 },
            { "B", 300 },
            { "C", 200 },
            { "D", 100 },
            { "E", 50 }
        };
        await _databaseService.SaveKeyStatsAsync(today, 14, keyStats);

        // Act
        var result = await _databaseService.GetTopKeysAsync(DateTime.Today, DateTime.Today, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[0].KeyCode);  // Use named tuple property
        Assert.Equal(500, result[0].Count);    // Use named tuple property
        Assert.Equal("B", result[1].KeyCode);
        Assert.Equal(300, result[1].Count);
        Assert.Equal("C", result[2].KeyCode);
        Assert.Equal(200, result[2].Count);
    }

    [Fact]
    public async Task SaveKeyStatsAsync_UpdatesExistingData()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = 15;
        
        var initialStats = new Dictionary<string, int> { { "A", 100 } };
        await _databaseService.SaveKeyStatsAsync(date, hour, initialStats);
        
        var updatedStats = new Dictionary<string, int> { { "A", 200 } };

        // Act
        await _databaseService.SaveKeyStatsAsync(date, hour, updatedStats);

        // Assert
        var result = await _databaseService.GetTodayKeyStatsAsync();
        Assert.Equal(200, result["A"]); // Should be updated, not added
    }

    [Fact]
    public async Task Migration_PreservesExistingDailyStats()
    {
        // Arrange - Create v1 database with daily stats
        await _databaseService.InitializeAsync();
        var dailyStats = new Core.Models.DailyStats
        {
            Date = DateTime.Today.ToString("yyyy-MM-dd"),
            KeyCount = 1000,
            MouseDistance = 500.5,
            LeftClicks = 100,
            RightClicks = 50,
            MiddleClicks = 10,
            ScrollDistance = 25.5
        };
        await _databaseService.SaveDailyStatsAsync(dailyStats);

        // Act - Reinitialize to trigger migration
        var newDbService = new DatabaseService(_loggerMock.Object, _testDbPath);
        await newDbService.InitializeAsync();

        // Assert - Daily stats should still exist
        var retrievedStats = await newDbService.GetDailyStatsAsync(dailyStats.Date);
        Assert.NotNull(retrievedStats);
        Assert.Equal(dailyStats.KeyCount, retrievedStats.KeyCount);
        Assert.Equal(dailyStats.MouseDistance, retrievedStats.MouseDistance);
        newDbService.Dispose();
    }

    [Fact]
    public async Task GetLifetimeKeyStatsAsync_AggregatesAllData()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        // Add data for multiple days
        for (int i = 0; i < 30; i++)
        {
            var date = DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd");
            var keyStats = new Dictionary<string, int>
            {
                { "Space", 100 },
                { "Enter", 50 }
            };
            await _databaseService.SaveKeyStatsAsync(date, 10, keyStats);
        }

        // Act
        var result = await _databaseService.GetLifetimeKeyStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3000, result["Space"]); // 100 * 30 days
        Assert.Equal(1500, result["Enter"]); // 50 * 30 days
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDbPath}");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<bool> IndexExistsAsync(string indexName)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDbPath}");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name=@indexName";
        command.Parameters.AddWithValue("@indexName", indexName);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<bool> ViewExistsAsync(string viewName)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDbPath}");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='view' AND name=@viewName";
        command.Parameters.AddWithValue("@viewName", viewName);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<int> GetDatabaseVersionAsync()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_testDbPath}");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX(version) FROM schema_version";
        var result = await command.ExecuteScalarAsync();
        return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result);
    }

    public void Dispose()
    {
        _databaseService?.Dispose();
        
        // Clean up test database file
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
using FluentAssertions;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class DatabaseServiceTests : IDisposable
{
    private readonly Mock<ILogger<DatabaseService>> _mockLogger;
    private readonly DatabaseService _databaseService;
    private readonly string _testDatabasePath;

    public DatabaseServiceTests()
    {
        _mockLogger = new Mock<ILogger<DatabaseService>>();
        _testDatabasePath = Path.GetTempFileName();
        _databaseService = new DatabaseService(_mockLogger.Object, _testDatabasePath);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateDatabaseTables()
    {
        // Act
        await _databaseService.InitializeAsync();

        // Assert
        // If no exception is thrown, the initialization was successful
        File.Exists(_testDatabasePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveDailyStatsAsync_ShouldSaveStatsToDatabase()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var stats = new DailyStats
        {
            Date = "2024-01-15",
            KeyCount = 100,
            MouseDistance = 1.5,
            LeftClicks = 50,
            RightClicks = 25,
            MiddleClicks = 5
        };

        // Act
        await _databaseService.SaveDailyStatsAsync(stats);

        // Assert
        var retrievedStats = await _databaseService.GetDailyStatsAsync("2024-01-15");
        retrievedStats.Should().NotBeNull();
        retrievedStats!.Date.Should().Be("2024-01-15");
        retrievedStats.KeyCount.Should().Be(100);
        retrievedStats.MouseDistance.Should().Be(1.5);
        retrievedStats.LeftClicks.Should().Be(50);
        retrievedStats.RightClicks.Should().Be(25);
        retrievedStats.MiddleClicks.Should().Be(5);
    }

    [Fact]
    public async Task GetDailyStatsAsync_WithNonExistentDate_ShouldReturnNull()
    {
        // Arrange
        await _databaseService.InitializeAsync();

        // Act
        var result = await _databaseService.GetDailyStatsAsync("2024-01-01");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDailyStatsAsync_ShouldUpdateExistingRecord()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var initialStats = new DailyStats
        {
            Date = "2024-01-15",
            KeyCount = 100,
            MouseDistance = 1.5
        };

        var updatedStats = new DailyStats
        {
            Date = "2024-01-15",
            KeyCount = 200,
            MouseDistance = 3.0,
            LeftClicks = 10
        };

        // Act
        await _databaseService.SaveDailyStatsAsync(initialStats);
        await _databaseService.SaveDailyStatsAsync(updatedStats);

        // Assert
        var retrievedStats = await _databaseService.GetDailyStatsAsync("2024-01-15");
        retrievedStats.Should().NotBeNull();
        retrievedStats!.KeyCount.Should().Be(200);
        retrievedStats.MouseDistance.Should().Be(3.0);
        retrievedStats.LeftClicks.Should().Be(10);
    }

    [Fact]
    public async Task GetDailyStatsRangeAsync_ShouldReturnStatsInRange()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        var stats1 = DailyStats.CreateForDate(new DateTime(2024, 1, 1));
        stats1.KeyCount = 100;
        
        var stats2 = DailyStats.CreateForDate(new DateTime(2024, 1, 2));
        stats2.KeyCount = 200;
        
        var stats3 = DailyStats.CreateForDate(new DateTime(2024, 1, 5)); // Outside range
        stats3.KeyCount = 300;

        await _databaseService.SaveDailyStatsAsync(stats1);
        await _databaseService.SaveDailyStatsAsync(stats2);
        await _databaseService.SaveDailyStatsAsync(stats3);

        // Act
        var result = await _databaseService.GetDailyStatsRangeAsync(
            new DateTime(2024, 1, 1), 
            new DateTime(2024, 1, 3));

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.KeyCount == 100);
        result.Should().Contain(s => s.KeyCount == 200);
        result.Should().NotContain(s => s.KeyCount == 300);
    }

    [Fact]
    public async Task SaveKeyMouseEventAsync_ShouldSaveEventToDatabase()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var keyEvent = new KeyMouseEvent
        {
            Timestamp = DateTime.Now,
            EventType = "key_down",
            Key = "A"
        };

        // Act & Assert
        // Should not throw an exception
        await _databaseService.SaveKeyMouseEventAsync(keyEvent);
    }

    [Fact]
    public async Task CleanupOldDataAsync_ShouldRemoveOldRecords()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        var oldStats = DailyStats.CreateForDate(DateTime.Today.AddDays(-100));
        oldStats.KeyCount = 100;
        
        var recentStats = DailyStats.CreateForDate(DateTime.Today.AddDays(-10));
        recentStats.KeyCount = 200;

        await _databaseService.SaveDailyStatsAsync(oldStats);
        await _databaseService.SaveDailyStatsAsync(recentStats);

        // Act
        await _databaseService.CleanupOldDataAsync(30); // Keep last 30 days

        // Assert
        var oldResult = await _databaseService.GetDailyStatsAsync(oldStats.Date);
        var recentResult = await _databaseService.GetDailyStatsAsync(recentStats.Date);
        
        oldResult.Should().BeNull(); // Should be deleted
        recentResult.Should().NotBeNull(); // Should be kept
    }

    public void Dispose()
    {
        _databaseService?.Dispose();
        
        if (File.Exists(_testDatabasePath))
        {
            File.Delete(_testDatabasePath);
        }
    }
}
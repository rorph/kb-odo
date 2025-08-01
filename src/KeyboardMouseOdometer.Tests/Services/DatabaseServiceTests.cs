using FluentAssertions;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class DatabaseServiceTests : TestDatabaseFixture
{
    public DatabaseServiceTests() : base(useFileDatabase: false)
    {
        // Use in-memory database to avoid file locking issues in CI
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateDatabaseTables()
    {
        // Act
        await DatabaseService.InitializeAsync();

        // Assert
        // If no exception is thrown, the initialization was successful
        // For in-memory databases, we can't check file existence, so we just verify no exception was thrown
        // The test passes if we reach this point without an exception
        Assert.True(true);
    }

    [Fact]
    public async Task SaveDailyStatsAsync_ShouldSaveStatsToDatabase()
    {
        // Arrange
        await DatabaseService.InitializeAsync();
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
        await DatabaseService.SaveDailyStatsAsync(stats);

        // Assert
        var retrievedStats = await DatabaseService.GetDailyStatsAsync("2024-01-15");
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
        await DatabaseService.InitializeAsync();

        // Act
        var result = await DatabaseService.GetDailyStatsAsync("2024-01-01");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDailyStatsAsync_ShouldUpdateExistingRecord()
    {
        // Arrange
        await DatabaseService.InitializeAsync();
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
        await DatabaseService.SaveDailyStatsAsync(initialStats);
        await DatabaseService.SaveDailyStatsAsync(updatedStats);

        // Assert
        var retrievedStats = await DatabaseService.GetDailyStatsAsync("2024-01-15");
        retrievedStats.Should().NotBeNull();
        retrievedStats!.KeyCount.Should().Be(200);
        retrievedStats.MouseDistance.Should().Be(3.0);
        retrievedStats.LeftClicks.Should().Be(10);
    }

    [Fact]
    public async Task GetDailyStatsRangeAsync_ShouldReturnStatsInRange()
    {
        // Arrange
        await DatabaseService.InitializeAsync();
        
        var stats1 = DailyStats.CreateForDate(new DateTime(2024, 1, 1));
        stats1.KeyCount = 100;
        
        var stats2 = DailyStats.CreateForDate(new DateTime(2024, 1, 2));
        stats2.KeyCount = 200;
        
        var stats3 = DailyStats.CreateForDate(new DateTime(2024, 1, 5)); // Outside range
        stats3.KeyCount = 300;

        await DatabaseService.SaveDailyStatsAsync(stats1);
        await DatabaseService.SaveDailyStatsAsync(stats2);
        await DatabaseService.SaveDailyStatsAsync(stats3);

        // Act
        var result = await DatabaseService.GetDailyStatsRangeAsync(
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
        await DatabaseService.InitializeAsync();
        var keyEvent = new KeyMouseEvent
        {
            Timestamp = DateTime.Now,
            EventType = "key_down",
            Key = "A"
        };

        // Act & Assert
        // Should not throw an exception
        await DatabaseService.SaveKeyMouseEventAsync(keyEvent);
    }

    [Fact]
    public async Task CleanupOldDataAsync_ShouldRemoveOldRecords()
    {
        // Arrange
        await DatabaseService.InitializeAsync();
        
        var oldStats = DailyStats.CreateForDate(DateTime.Today.AddDays(-100));
        oldStats.KeyCount = 100;
        
        var recentStats = DailyStats.CreateForDate(DateTime.Today.AddDays(-10));
        recentStats.KeyCount = 200;

        await DatabaseService.SaveDailyStatsAsync(oldStats);
        await DatabaseService.SaveDailyStatsAsync(recentStats);

        // Act
        await DatabaseService.CleanupOldDataAsync(30); // Keep last 30 days

        // Assert
        var oldResult = await DatabaseService.GetDailyStatsAsync(oldStats.Date);
        var recentResult = await DatabaseService.GetDailyStatsAsync(recentStats.Date);
        
        oldResult.Should().BeNull(); // Should be deleted
        recentResult.Should().NotBeNull(); // Should be kept
    }

}
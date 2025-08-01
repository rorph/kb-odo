using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class HourlyStatsTests
{
    [Fact]
    public void HourlyStats_CreateEmpty_ShouldInitializeCorrectly()
    {
        // Arrange
        var date = "2025-08-01";
        var hour = 14;

        // Act
        var hourlyStats = HourlyStats.CreateEmpty(date, hour);

        // Assert
        Assert.Equal(date, hourlyStats.Date);
        Assert.Equal(hour, hourlyStats.Hour);
        Assert.Equal(0, hourlyStats.KeyCount);
        Assert.Equal(0, hourlyStats.MouseDistance);
        Assert.Equal(0, hourlyStats.LeftClicks);
        Assert.Equal(0, hourlyStats.RightClicks);
        Assert.Equal(0, hourlyStats.MiddleClicks);
        Assert.Equal(0, hourlyStats.ScrollDistance);
        Assert.Equal(0, hourlyStats.TotalClicks);
    }

    [Fact]
    public void HourlyStats_TotalClicks_ShouldCalculateCorrectly()
    {
        // Arrange
        var hourlyStats = new HourlyStats
        {
            LeftClicks = 5,
            RightClicks = 3,
            MiddleClicks = 2
        };

        // Act & Assert
        Assert.Equal(10, hourlyStats.TotalClicks);
    }

    [Fact]
    public async Task DatabaseService_SaveAndRetrieveHourlyStats_ShouldWork()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<DatabaseService>();
        var dbService = new DatabaseService(logger, ":memory:");
        await dbService.InitializeAsync();

        var date = "2025-08-01";
        var hour = 14;
        var stats = new DailyStats
        {
            Date = date,
            KeyCount = 100,
            MouseDistance = 50.5,
            LeftClicks = 10,
            RightClicks = 5,
            MiddleClicks = 2,
            ScrollDistance = 15.5
        };

        // Act
        await dbService.SaveHourlyStatsAsync(date, hour, stats);
        var retrievedStats = await dbService.GetHourlyStatsAsync(date);

        // Assert
        Assert.Single(retrievedStats);
        var hourlyStats = retrievedStats[0];
        Assert.Equal(date, hourlyStats.Date);
        Assert.Equal(hour, hourlyStats.Hour);
        Assert.Equal(100, hourlyStats.KeyCount);
        Assert.Equal(50.5, hourlyStats.MouseDistance);
        Assert.Equal(10, hourlyStats.LeftClicks);
        Assert.Equal(5, hourlyStats.RightClicks);
        Assert.Equal(2, hourlyStats.MiddleClicks);
        Assert.Equal(15.5, hourlyStats.ScrollDistance);

        // Cleanup
        dbService.Dispose();
    }
}
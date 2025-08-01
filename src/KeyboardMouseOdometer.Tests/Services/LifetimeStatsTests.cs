using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class LifetimeStatsTests : TestDatabaseFixture
{
    public LifetimeStatsTests() : base(useFileDatabase: false)
    {
        // Use in-memory database to avoid file locking issues in CI
        // Don't initialize in constructor - let each test do it
    }

    [Fact]
    public async Task GetLifetimeStatsAsync_EmptyDatabase_ReturnsZeroStats()
    {
        // Arrange
        await InitializeDatabaseAsync();
        
        // Act
        var lifetimeStats = await DatabaseService.GetLifetimeStatsAsync();

        // Assert
        Assert.Equal(0, lifetimeStats.TotalKeys);
        Assert.Equal(0.0, lifetimeStats.TotalMouseDistance);
        Assert.Equal(0, lifetimeStats.TotalLeftClicks);
        Assert.Equal(0, lifetimeStats.TotalRightClicks);
        Assert.Equal(0, lifetimeStats.TotalMiddleClicks);
        Assert.Equal(0.0, lifetimeStats.TotalScrollDistance);
        Assert.Null(lifetimeStats.FirstDate);
        Assert.Null(lifetimeStats.LastDate);
        Assert.Equal(0, lifetimeStats.TotalDays);
        Assert.Equal("No data available", lifetimeStats.GetTrackingPeriod());
    }

    [Fact]
    public async Task GetLifetimeStatsAsync_WithSampleData_CalculatesCorrectTotals()
    {
        // Arrange
        await InitializeDatabaseAsync();
        
        var day1Stats = new DailyStats
        {
            Date = "2024-01-01",
            KeyCount = 1000,
            MouseDistance = 100.5,
            LeftClicks = 50,
            RightClicks = 25,
            MiddleClicks = 5,
            ScrollDistance = 10.2
        };

        var day2Stats = new DailyStats
        {
            Date = "2024-01-02",
            KeyCount = 2000,
            MouseDistance = 200.3,
            LeftClicks = 75,
            RightClicks = 30,
            MiddleClicks = 10,
            ScrollDistance = 15.8
        };

        var day3Stats = new DailyStats
        {
            Date = "2024-01-03",
            KeyCount = 1500,
            MouseDistance = 150.7,
            LeftClicks = 60,
            RightClicks = 20,
            MiddleClicks = 8,
            ScrollDistance = 12.5
        };

        await DatabaseService.SaveDailyStatsAsync(day1Stats);
        await DatabaseService.SaveDailyStatsAsync(day2Stats);
        await DatabaseService.SaveDailyStatsAsync(day3Stats);

        // Act
        var lifetimeStats = await DatabaseService.GetLifetimeStatsAsync();

        // Assert
        Assert.Equal(4500, lifetimeStats.TotalKeys); // 1000 + 2000 + 1500
        Assert.Equal(451.5, lifetimeStats.TotalMouseDistance); // 100.5 + 200.3 + 150.7
        Assert.Equal(185, lifetimeStats.TotalLeftClicks); // 50 + 75 + 60
        Assert.Equal(75, lifetimeStats.TotalRightClicks); // 25 + 30 + 20
        Assert.Equal(23, lifetimeStats.TotalMiddleClicks); // 5 + 10 + 8
        Assert.Equal(283, lifetimeStats.TotalClicks); // 185 + 75 + 23
        Assert.Equal(38.5, lifetimeStats.TotalScrollDistance, precision: 1); // 10.2 + 15.8 + 12.5
        Assert.Equal("2024-01-01", lifetimeStats.FirstDate);
        Assert.Equal("2024-01-03", lifetimeStats.LastDate);
        Assert.Equal(3, lifetimeStats.TotalDays);
        Assert.Equal("From: 2024-01-01 to 2024-01-03", lifetimeStats.GetTrackingPeriod());
    }

    [Fact]
    public async Task GetLifetimeStatsAsync_WithSingleDay_ShowsSinceFormat()
    {
        // Arrange
        await InitializeDatabaseAsync();
        
        var dayStats = new DailyStats
        {
            Date = "2024-01-01",
            KeyCount = 1000,
            MouseDistance = 100.0,
            LeftClicks = 50,
            RightClicks = 25,
            MiddleClicks = 5,
            ScrollDistance = 10.0
        };

        await DatabaseService.SaveDailyStatsAsync(dayStats);

        // Act
        var lifetimeStats = await DatabaseService.GetLifetimeStatsAsync();

        // Assert
        Assert.Equal("Since: 2024-01-01", lifetimeStats.GetTrackingPeriod());
        Assert.Equal(1, lifetimeStats.TotalDays);
    }

    [Fact]
    public async Task GetLifetimeStatsAsync_IgnoresEmptyDays_CountsOnlyActiveDays()
    {
        // Arrange
        await InitializeDatabaseAsync();
        
        var activeDay = new DailyStats
        {
            Date = "2024-01-01",
            KeyCount = 1000,
            MouseDistance = 100.0,
            LeftClicks = 50,
            RightClicks = 25,
            MiddleClicks = 5,
            ScrollDistance = 10.0
        };

        var emptyDay = new DailyStats
        {
            Date = "2024-01-02",
            KeyCount = 0,
            MouseDistance = 0.0,
            LeftClicks = 0,
            RightClicks = 0,
            MiddleClicks = 0,
            ScrollDistance = 0.0
        };

        await DatabaseService.SaveDailyStatsAsync(activeDay);
        await DatabaseService.SaveDailyStatsAsync(emptyDay);

        // Act
        var lifetimeStats = await DatabaseService.GetLifetimeStatsAsync();

        // Assert
        Assert.Equal(1, lifetimeStats.TotalDays); // Should only count the active day
        Assert.Equal("Since: 2024-01-01", lifetimeStats.GetTrackingPeriod()); // Should show only the active day
    }

}
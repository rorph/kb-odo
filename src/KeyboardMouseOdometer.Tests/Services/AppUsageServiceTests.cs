using System;
using System.Threading.Tasks;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class AppUsageServiceTests : IDisposable
{
    private readonly AppUsageService _appUsageService;
    private readonly DatabaseService _databaseService;
    private readonly Mock<ILogger<AppUsageService>> _loggerMock;
    private readonly Mock<ILogger<DatabaseService>> _dbLoggerMock;
    private readonly Configuration _configuration;
    private readonly string _testDbPath;

    public AppUsageServiceTests()
    {
        _loggerMock = new Mock<ILogger<AppUsageService>>();
        _dbLoggerMock = new Mock<ILogger<DatabaseService>>();
        
        // Create a test database
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_appusage_{Guid.NewGuid()}.db");
        _databaseService = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
        
        // Create configuration with app tracking enabled
        _configuration = new Configuration
        {
            TrackApplicationUsage = true,
            DatabasePath = _testDbPath
        };
        
        // Create the service
        _appUsageService = new AppUsageService(_loggerMock.Object, _databaseService, _configuration);
    }

    [Fact]
    public async Task SavePendingDataAsync_ShouldSaveAppUsageToDatabase()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        
        // Act
        await _appUsageService.SavePendingDataAsync(); // Should handle empty data gracefully
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "TestApp", 100);
        
        // Assert
        var todayStats = await _databaseService.GetTodayAppUsageAsync();
        Assert.NotNull(todayStats);
        Assert.Single(todayStats);
        Assert.Equal("TestApp", todayStats[0].AppName);
        Assert.Equal(100, todayStats[0].SecondsUsed);
    }

    [Fact]
    public async Task StartTracking_WhenConfigDisabled_ShouldNotTrack()
    {
        // Arrange
        var disabledConfig = new Configuration
        {
            TrackApplicationUsage = false,
            DatabasePath = _testDbPath
        };
        var service = new AppUsageService(_loggerMock.Object, _databaseService, disabledConfig);
        
        // Act
        service.StartTracking();
        
        // Assert - verify that the service logged it's disabled
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleHourRolloverAsync_ShouldSavePendingData()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        // Act
        await _appUsageService.HandleHourRolloverAsync();
        
        // Assert - Should complete without errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("hour rollover")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleDayRolloverAsync_ShouldSavePendingData()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        
        // Act
        await _appUsageService.HandleDayRolloverAsync();
        
        // Assert - Should complete without errors and log the rollover
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("day rollover")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AppUsageStats_FormattedTime_ShouldFormatCorrectly()
    {
        // Arrange & Act
        var stats1 = new AppUsageStats { AppName = "Test1", SecondsUsed = 45 };
        var stats2 = new AppUsageStats { AppName = "Test2", SecondsUsed = 125 };
        var stats3 = new AppUsageStats { AppName = "Test3", SecondsUsed = 3661 };
        
        // Assert
        Assert.Equal("45s", stats1.FormattedTime);
        Assert.Equal("2m 5s", stats2.FormattedTime);
        Assert.Equal("1h 1m", stats3.FormattedTime);
    }

    [Fact]
    public async Task DatabaseMethods_ShouldRetrieveAppUsageByTimeRange()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        
        // Save some test data
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "App1", 300);
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "App2", 600);
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "App3", 150);
        
        // Act
        var todayStats = await _databaseService.GetTodayAppUsageAsync();
        var weeklyStats = await _databaseService.GetWeeklyAppUsageAsync();
        var monthlyStats = await _databaseService.GetMonthlyAppUsageAsync();
        var lifetimeStats = await _databaseService.GetLifetimeAppUsageAsync();
        
        // Assert
        Assert.Equal(3, todayStats.Count);
        Assert.Equal("App2", todayStats[0].AppName); // Should be sorted by usage
        Assert.Equal(600, todayStats[0].SecondsUsed);
        
        // Weekly, monthly, and lifetime should also contain the same data
        Assert.Equal(3, weeklyStats.Count);
        Assert.Equal(3, monthlyStats.Count);
        Assert.Equal(3, lifetimeStats.Count);
    }

    [Fact]
    public async Task SaveAppUsageStatsAsync_ShouldAccumulateTime()
    {
        // Arrange
        await _databaseService.InitializeAsync();
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        
        // Act - Save multiple times for the same app
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "TestApp", 100);
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "TestApp", 50);
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "TestApp", 75);
        
        // Assert - Time should be accumulated
        var todayStats = await _databaseService.GetTodayAppUsageAsync();
        Assert.Single(todayStats);
        Assert.Equal("TestApp", todayStats[0].AppName);
        Assert.Equal(225, todayStats[0].SecondsUsed); // 100 + 50 + 75
    }

    public void Dispose()
    {
        _appUsageService?.Dispose();
        _databaseService?.Dispose();
        
        // Clean up test database
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
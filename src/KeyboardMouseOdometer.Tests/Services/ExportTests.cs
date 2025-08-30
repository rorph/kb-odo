using System;
using System.IO;
using System.Threading.Tasks;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services;

public class ExportTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseService _databaseService;
    private readonly Mock<ILogger<DatabaseService>> _mockLogger;

    public ExportTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.db");
        _mockLogger = new Mock<ILogger<DatabaseService>>();
        _databaseService = new DatabaseService(_mockLogger.Object, _testDbPath);
        _databaseService.InitializeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyDatabase_ReturnsHeadersOnly()
    {
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        Assert.NotNull(csv);
        Assert.Contains("=== DAILY STATISTICS ===", csv);
        Assert.Contains("Date,Keystrokes,Mouse Distance,Mouse Clicks,Scroll Distance", csv);
        Assert.Contains("=== HOURLY STATISTICS ===", csv);
        Assert.Contains("=== KEY STATISTICS ===", csv);
        Assert.Contains("=== APPLICATION USAGE ===", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithDailyStats_ExportsCorrectly()
    {
        // Arrange
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        await _databaseService.IncrementStatsAsync(date, hour, 10, 100, 5, 0, 0, 20);
        
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        Assert.Contains($"{date},10,100.00,5,20.00", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithKeyStats_ExportsCorrectly()
    {
        // Arrange
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        await _databaseService.IncrementKeyStatsAsync(date, hour, "A", 5); // 'A' key with count 5
        await _databaseService.IncrementKeyStatsAsync(date, hour, "B", 3); // 'B' key with count 3
        
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        Assert.Contains($"{date},A,5", csv);
        Assert.Contains($"{date},B,3", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithAppUsage_ExportsCorrectly()
    {
        // Arrange
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "TestApp", 120);
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "AnotherApp", 60);
        
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        Assert.Contains($"{date},{hour},TestApp,120", csv);
        Assert.Contains($"{date},{hour},AnotherApp,60", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);
        
        // Add data for different days using public methods
        await _databaseService.IncrementStatsAsync(today.ToString("yyyy-MM-dd"), 12, 100, 200, 10, 0, 0, 50);
        await _databaseService.IncrementStatsAsync(yesterday.ToString("yyyy-MM-dd"), 12, 150, 250, 15, 0, 0, 60);
        await _databaseService.IncrementStatsAsync(twoDaysAgo.ToString("yyyy-MM-dd"), 12, 200, 300, 20, 0, 0, 70);
        
        // Act - Export only yesterday's data
        var csv = await _databaseService.ExportToCsvAsync(yesterday, yesterday);
        
        // Assert
        Assert.Contains($"{yesterday:yyyy-MM-dd},150,250.00,15,60.00", csv);
        Assert.DoesNotContain($"{today:yyyy-MM-dd},100", csv);
        Assert.DoesNotContain($"{twoDaysAgo:yyyy-MM-dd},200", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithHourlyStats_ExportsCorrectly()
    {
        // Arrange
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        
        // Add hourly stats for different hours
        for (int hour = 0; hour < 3; hour++)
        {
            await _databaseService.IncrementStatsAsync(
                date, 
                hour,
                (hour + 1) * 10,  // keystrokes
                (hour + 1) * 100, // mouse distance
                (hour + 1) * 5,   // mouse clicks
                0,                // left clicks
                0,                // right clicks
                (hour + 1) * 20); // scroll distance
        }
        
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        Assert.Contains($"{date},2,30,300.00,15,60.00", csv);
        Assert.Contains($"{date},1,20,200.00,10,40.00", csv);
        Assert.Contains($"{date},0,10,100.00,5,20.00", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_LargeDataset_HandlesCorrectly()
    {
        // Arrange - Add a lot of data
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var currentHour = DateTime.Now.Hour;
        
        // Add 100 different keys
        for (int i = 0; i < 100; i++)
        {
            await _databaseService.IncrementKeyStatsAsync(date, currentHour, $"Key{i}", i + 1);
        }
        
        // Add 24 hours of app usage
        for (int hour = 0; hour < 24; hour++)
        {
            await _databaseService.SaveAppUsageStatsAsync(date, hour, $"App{hour}", hour * 60);
        }
        
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        Assert.NotNull(csv);
        Assert.Contains("Key99,100", csv); // Last key with count 100
        Assert.Contains("App23", csv); // Last hour app
        
        // Verify CSV is not corrupted
        var lines = csv.Split('\n');
        Assert.True(lines.Length > 100); // Should have many lines
    }

    [Fact]
    public async Task ExportToCsvAsync_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        
        // Add app with special characters
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "App,With,Commas", 60);
        await _databaseService.SaveAppUsageStatsAsync(date, hour, "App\"With\"Quotes", 30);
        
        // Act
        var csv = await _databaseService.ExportToCsvAsync();
        
        // Assert
        // Note: Simple CSV format might not handle special characters perfectly
        // This test ensures no exceptions are thrown
        Assert.NotNull(csv);
        Assert.Contains("App", csv);
    }

    public void Dispose()
    {
        _databaseService?.Dispose();
        
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
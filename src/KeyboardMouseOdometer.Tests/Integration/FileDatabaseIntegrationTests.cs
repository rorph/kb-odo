using FluentAssertions;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Integration;

/// <summary>
/// Integration tests that specifically test file-based database operations.
/// These tests are more prone to CI failures due to file locking, but are important
/// for ensuring the application works correctly with actual database files.
/// </summary>
public class FileDatabaseIntegrationTests : TestDatabaseFixture
{
    public FileDatabaseIntegrationTests() : base(useFileDatabase: true)
    {
        // These tests specifically require file-based databases
    }

    [Fact]
    public async Task FileDatabase_InitializeAsync_ShouldCreateDatabaseFile()
    {
        // Act
        await DatabaseService.InitializeAsync();

        // Assert
        File.Exists(TestDatabasePath).Should().BeTrue();
    }

    [Fact]
    public async Task FileDatabase_PersistData_ShouldSurviveDisposal()
    {
        // Arrange
        await DatabaseService.InitializeAsync();
        var testStats = new DailyStats
        {
            Date = "2024-01-15",
            KeyCount = 1000,
            MouseDistance = 500.5,
            LeftClicks = 100,
            RightClicks = 50,
            MiddleClicks = 10,
            ScrollDistance = 25.5
        };

        // Act - Save data and dispose the service
        await DatabaseService.SaveDailyStatsAsync(testStats);
        DatabaseService.Dispose();

        // Create a new service instance with the same database path
        var newService = new DatabaseService(MockLogger.Object, TestDatabasePath);
        await newService.InitializeAsync();

        // Assert - Data should still be there
        var retrievedStats = await newService.GetDailyStatsAsync("2024-01-15");
        retrievedStats.Should().NotBeNull();
        retrievedStats!.KeyCount.Should().Be(1000);
        retrievedStats.MouseDistance.Should().Be(500.5);
        retrievedStats.LeftClicks.Should().Be(100);
        retrievedStats.RightClicks.Should().Be(50);
        retrievedStats.MiddleClicks.Should().Be(10);
        retrievedStats.ScrollDistance.Should().Be(25.5);

        // Cleanup
        newService.Dispose();
    }

    [Fact]
    public async Task FileDatabase_ConcurrentAccess_ShouldHandleGracefully()
    {
        // This test verifies that our database can handle multiple connections
        // which is important for real-world usage
        
        // Arrange
        await DatabaseService.InitializeAsync();
        
        var service2 = new DatabaseService(MockLogger.Object, TestDatabasePath);
        await service2.InitializeAsync();

        try
        {
            var stats1 = new DailyStats { Date = "2024-01-01", KeyCount = 100 };
            var stats2 = new DailyStats { Date = "2024-01-02", KeyCount = 200 };

            // Act - Write from both services
            await DatabaseService.SaveDailyStatsAsync(stats1);
            await service2.SaveDailyStatsAsync(stats2);

            // Assert - Both should be readable
            var retrieved1 = await DatabaseService.GetDailyStatsAsync("2024-01-01");
            var retrieved2 = await service2.GetDailyStatsAsync("2024-01-02");

            retrieved1.Should().NotBeNull();
            retrieved1!.KeyCount.Should().Be(100);
            
            retrieved2.Should().NotBeNull();
            retrieved2!.KeyCount.Should().Be(200);
        }
        finally
        {
            service2.Dispose();
        }
    }
}
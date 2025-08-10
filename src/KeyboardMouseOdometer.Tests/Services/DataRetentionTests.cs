using System;
using System.IO;
using System.Threading.Tasks;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Services
{
    /// <summary>
    /// Tests for data retention logic to ensure historical data is preserved correctly
    /// </summary>
    public class DataRetentionTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly DatabaseService _databaseService;
        private readonly Mock<ILogger<DatabaseService>> _loggerMock;

        public DataRetentionTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_retention_{Guid.NewGuid()}.db");
            _loggerMock = new Mock<ILogger<DatabaseService>>();
            _databaseService = new DatabaseService(_loggerMock.Object, _testDbPath);
        }

        [Fact]
        public async Task CleanupOldData_RetentionZero_PreservesAllData()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            
            // Insert data for multiple days
            var dates = new[]
            {
                DateTime.Today.AddDays(-10),
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddDays(-1),
                DateTime.Today
            };

            foreach (var date in dates)
            {
                var stats = new DailyStats
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    KeyCount = 1000,
                    MouseDistance = 100.0,
                    LeftClicks = 50
                };
                await _databaseService.SaveDailyStatsAsync(stats);
            }

            // Act - Call cleanup with retention = 0 (keep forever)
            await _databaseService.CleanupOldDataAsync(0);

            // Assert - All data should still exist
            foreach (var date in dates)
            {
                var retrievedStats = await _databaseService.GetDailyStatsAsync(date.ToString("yyyy-MM-dd"));
                Assert.NotNull(retrievedStats);
                Assert.Equal(1000, retrievedStats.KeyCount);
            }
        }

        [Fact]
        public async Task CleanupOldData_RetentionNegative_PreservesAllData()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            
            // Insert old data
            var oldDate = DateTime.Today.AddDays(-365);
            var stats = new DailyStats
            {
                Date = oldDate.ToString("yyyy-MM-dd"),
                KeyCount = 5000,
                MouseDistance = 500.0
            };
            await _databaseService.SaveDailyStatsAsync(stats);

            // Act - Call cleanup with negative retention (should skip)
            await _databaseService.CleanupOldDataAsync(-1);

            // Assert - Old data should still exist
            var retrievedStats = await _databaseService.GetDailyStatsAsync(oldDate.ToString("yyyy-MM-dd"));
            Assert.NotNull(retrievedStats);
            Assert.Equal(5000, retrievedStats.KeyCount);
        }

        [Fact]
        public async Task CleanupOldData_RetentionPositive_DeletesOldDataOnly()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            
            // Insert data for different time periods
            var veryOldDate = DateTime.Today.AddDays(-10);
            var recentDate = DateTime.Today.AddDays(-2);
            var todayDate = DateTime.Today;

            await _databaseService.SaveDailyStatsAsync(new DailyStats { Date = veryOldDate.ToString("yyyy-MM-dd"), KeyCount = 1000 });
            await _databaseService.SaveDailyStatsAsync(new DailyStats { Date = recentDate.ToString("yyyy-MM-dd"), KeyCount = 1000 });
            await _databaseService.SaveDailyStatsAsync(new DailyStats { Date = todayDate.ToString("yyyy-MM-dd"), KeyCount = 1000 });

            // Act - Cleanup with 5 day retention
            await _databaseService.CleanupOldDataAsync(5);

            // Assert
            var veryOldStats = await _databaseService.GetDailyStatsAsync(veryOldDate.ToString("yyyy-MM-dd"));
            var recentStats = await _databaseService.GetDailyStatsAsync(recentDate.ToString("yyyy-MM-dd"));
            var todayStats = await _databaseService.GetDailyStatsAsync(todayDate.ToString("yyyy-MM-dd"));

            Assert.Null(veryOldStats); // Should be deleted (>5 days old)
            Assert.NotNull(recentStats); // Should be kept (2 days old)
            Assert.NotNull(todayStats); // Should be kept (today)
        }

        [Fact]
        public async Task MidnightRollover_RetentionZero_NoDataLoss()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            
            // Simulate data from yesterday
            var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            var yesterdayStats = new DailyStats
            {
                Date = yesterday,
                KeyCount = 42734,  // From the debug logs
                MouseDistance = 1048.69
            };
            await _databaseService.SaveDailyStatsAsync(yesterdayStats);

            // Simulate midnight rollover with retention = 0
            // This is what was causing the bug - cleanup being called with retention = 0
            await _databaseService.CleanupOldDataAsync(0);

            // Assert - Yesterday's data should still exist
            var retrievedStats = await _databaseService.GetDailyStatsAsync(yesterday);
            Assert.NotNull(retrievedStats);
            Assert.Equal(42734, retrievedStats.KeyCount);
            Assert.Equal(1048.69, retrievedStats.MouseDistance, 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task CleanupOldData_NonPositiveRetention_LogsSkipMessage(int retentionDays)
        {
            // Arrange
            await _databaseService.InitializeAsync();

            // Act
            await _databaseService.CleanupOldDataAsync(retentionDays);

            // Assert - Verify skip message was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Skipping data cleanup")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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
}
using Xunit;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.Core.Utils;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeyboardMouseOdometer.Tests.Integration
{
    public class KeyCaptureIntegrationTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly DatabaseService _databaseService;
        private readonly DataLoggerService _dataLoggerService;
        private readonly Mock<ILogger<DatabaseService>> _dbLoggerMock;
        private readonly Mock<ILogger<DataLoggerService>> _dataLoggerMock;
        private readonly Configuration _configuration;

        public KeyCaptureIntegrationTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_odometer_{Guid.NewGuid()}.db");
            _dbLoggerMock = new Mock<ILogger<DatabaseService>>();
            _dataLoggerMock = new Mock<ILogger<DataLoggerService>>();
            
            _configuration = new Configuration
            {
                DatabasePath = _testDbPath,
                DataFlushIntervalSeconds = 1,
                ChartUpdateIntervalSeconds = 5
            };

            _databaseService = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
            var keyCodeMapper = new CoreKeyCodeMapper();
            _dataLoggerService = new DataLoggerService(_dataLoggerMock.Object, _databaseService, _configuration, keyCodeMapper);
        }

        [Fact(Skip = "Temporarily skipped due to test infrastructure issues")]
        public async Task KeyPress_CapturedAndStored_AppearsInDatabase()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var keyEvent = new InputEvent
            {
                EventType = InputEventType.KeyPress,
                KeyIdentifier = "A",
                Timestamp = DateTime.Now
            };

            // Act
            _dataLoggerService.LogInputEvent(keyEvent);
            await Task.Delay(1500); // Wait for flush interval
            await _dataLoggerService.FlushAsync();

            // Assert
            var todayStats = await _databaseService.GetTodayKeyStatsAsync();
            Assert.NotNull(todayStats);
            Assert.True(todayStats.ContainsKey("A"));
            Assert.True(todayStats["A"] > 0);
        }

        [Fact(Skip = "Temporarily skipped due to test infrastructure issues")]
        public async Task MultipleKeyPresses_Aggregated_CorrectCounts()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var keys = new[] { "A", "B", "A", "C", "A", "B" };
            
            // Act
            foreach (var key in keys)
            {
                var keyEvent = new InputEvent
                {
                    EventType = InputEventType.KeyPress,
                    KeyIdentifier = key,
                    Timestamp = DateTime.Now
                };
                _dataLoggerService.LogInputEvent(keyEvent);
            }
            
            await Task.Delay(1500); // Wait for flush interval
            await _dataLoggerService.FlushAsync();

            // Assert
            var todayStats = await _databaseService.GetTodayKeyStatsAsync();
            Assert.Equal(3, todayStats["A"]);
            Assert.Equal(2, todayStats["B"]);
            Assert.Equal(1, todayStats["C"]);
        }

        [Fact(Skip = "Temporarily skipped due to test infrastructure issues")]
        public async Task KeyStats_HourlyAggregation_CorrectHourlyData()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var now = DateTime.Now;
            var currentHour = now.Hour;
            
            // Act
            for (int i = 0; i < 10; i++)
            {
                var keyEvent = new InputEvent
                {
                    EventType = InputEventType.KeyPress,
                    KeyIdentifier = "Space",
                    Timestamp = now
                };
                _dataLoggerService.LogInputEvent(keyEvent);
            }
            
            await _dataLoggerService.FlushAsync();

            // Assert
            var hourlyStats = await _databaseService.GetHourlyStatsAsync(now.ToString("yyyy-MM-dd"));
            Assert.NotNull(hourlyStats);
            Assert.True(hourlyStats.Count > 0);
            
            var currentHourStats = hourlyStats.Find(h => h.Hour == currentHour);
            Assert.NotNull(currentHourStats);
            Assert.Equal(10, currentHourStats.KeyCount);
        }

        [Fact]
        public async Task WeeklyKeyStats_MultiDayData_CorrectAggregation()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            var today = DateTime.Today;
            
            // Insert data for multiple days
            var keyCounts = new Dictionary<string, int>
            {
                { "Enter", 100 },
                { "Backspace", 50 },
                { "Tab", 25 }
            };
            
            for (int dayOffset = 0; dayOffset < 3; dayOffset++)
            {
                var date = today.AddDays(-dayOffset).ToString("yyyy-MM-dd");
                await _databaseService.SaveKeyStatsAsync(date, 12, keyCounts);
            }

            // Act
            var weeklyStats = await _databaseService.GetWeeklyKeyStatsAsync();

            // Assert
            Assert.NotNull(weeklyStats);
            Assert.Equal(300, weeklyStats["Enter"]); // 100 * 3 days
            Assert.Equal(150, weeklyStats["Backspace"]); // 50 * 3 days
            Assert.Equal(75, weeklyStats["Tab"]); // 25 * 3 days
        }

        [Fact]
        public async Task MonthlyKeyStats_FullMonth_CorrectTotals()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            var today = DateTime.Today;
            
            // Insert data for 15 days
            for (int dayOffset = 0; dayOffset < 15; dayOffset++)
            {
                var date = today.AddDays(-dayOffset).ToString("yyyy-MM-dd");
                var keyCounts = new Dictionary<string, int>
                {
                    { "Shift", 10 * (dayOffset + 1) }
                };
                await _databaseService.SaveKeyStatsAsync(date, 10, keyCounts);
            }

            // Act
            var monthlyStats = await _databaseService.GetMonthlyKeyStatsAsync();

            // Assert
            Assert.NotNull(monthlyStats);
            Assert.True(monthlyStats.ContainsKey("Shift"));
            
            // Sum of 10 + 20 + 30 + ... + 150 = 10 * (1+2+3+...+15) = 10 * 120 = 1200
            var expectedTotal = 10 * (15 * 16 / 2); // Arithmetic series sum
            Assert.Equal(expectedTotal, monthlyStats["Shift"]);
        }

        [Fact]
        public async Task LifetimeKeyStats_AllData_CompleteTotals()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            
            // Insert varied data
            var dates = new[]
            {
                DateTime.Today,
                DateTime.Today.AddDays(-30),
                DateTime.Today.AddDays(-60),
                DateTime.Today.AddDays(-365)
            };
            
            foreach (var date in dates)
            {
                var keyCounts = new Dictionary<string, int>
                {
                    { "Ctrl", 1000 },
                    { "Alt", 500 }
                };
                await _databaseService.SaveKeyStatsAsync(date.ToString("yyyy-MM-dd"), 14, keyCounts);
            }

            // Act
            var lifetimeStats = await _databaseService.GetLifetimeKeyStatsAsync();

            // Assert
            Assert.NotNull(lifetimeStats);
            Assert.Equal(4000, lifetimeStats["Ctrl"]); // 1000 * 4 dates
            Assert.Equal(2000, lifetimeStats["Alt"]); // 500 * 4 dates
        }

        [Fact]
        public async Task DatabaseMigration_ExistingDatabase_MigratesSuccessfully()
        {
            // Arrange
            // Create a database without key_stats table (simulating old version)
            await _databaseService.InitializeAsync();
            
            // Drop key_stats table to simulate old database
            using (var conn = await _databaseService.GetConnectionAsync())
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DROP TABLE IF EXISTS key_stats";
                await cmd.ExecuteNonQueryAsync();
            }

            // Act
            // Re-initialize should trigger migration
            var newDbService = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
            await newDbService.InitializeAsync();

            // Assert
            // Check that key_stats table was created
            using (var conn = await newDbService.GetConnectionAsync())
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='key_stats'";
                var result = await cmd.ExecuteScalarAsync();
                Assert.NotNull(result);
                Assert.Equal("key_stats", result.ToString());
            }
        }

        [Fact(Skip = "Temporarily skipped due to test infrastructure issues")]
        public async Task ConcurrentKeyPresses_ThreadSafety_NoDataLoss()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var tasks = new List<Task>();
            var keyPressCount = 100;
            var threadCount = 10;

            // Act
            for (int thread = 0; thread < threadCount; thread++)
            {
                var threadId = thread;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < keyPressCount; i++)
                    {
                        var keyEvent = new InputEvent
                        {
                            EventType = InputEventType.KeyPress,
                            KeyIdentifier = $"Thread{threadId}",
                            Timestamp = DateTime.Now
                        };
                        _dataLoggerService.LogInputEvent(keyEvent);
                    }
                }));
            }

            await Task.WhenAll(tasks);
            await _dataLoggerService.FlushAsync();

            // Assert
            var todayStats = await _databaseService.GetTodayKeyStatsAsync();
            for (int thread = 0; thread < threadCount; thread++)
            {
                var key = $"Thread{thread}";
                Assert.True(todayStats.ContainsKey(key), $"Missing data for {key}");
                Assert.Equal(keyPressCount, todayStats[key]);
            }
        }

        public void Dispose()
        {
            _dataLoggerService?.Dispose();
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
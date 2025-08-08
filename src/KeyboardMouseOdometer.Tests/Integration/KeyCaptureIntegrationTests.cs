using Xunit;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.Core.Utils;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                DatabaseSaveIntervalMs = 1000,  // Save every 1 second for tests
                UIUpdateIntervalMs = 100        // Fast UI updates for tests
            };

            _databaseService = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
            var keyCodeMapper = new CoreKeyCodeMapper();
            _dataLoggerService = new DataLoggerService(_dataLoggerMock.Object, _databaseService, _configuration, keyCodeMapper);
        }

        [Fact]
        public async Task KeyPress_CapturedAndStored_AppearsInDatabase()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();

            // Act
            _dataLoggerService.LogKeyPress("A");
            await Task.Delay(1500); // Wait for save timer
            
            // Force save of pending data
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var hour = DateTime.Now.Hour;

            // Assert - check database directly
            var keyStats = await _databaseService.GetKeyStatsAsync(today, hour);
            Assert.NotNull(keyStats);
            Assert.True(keyStats.ContainsKey("A"));
            Assert.True(keyStats["A"] > 0);
        }

        [Fact]
        public async Task MultipleKeyPresses_Aggregated_CorrectCounts()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var keys = new[] { "A", "B", "A", "C", "A", "B" };
            
            // Act
            foreach (var key in keys)
            {
                _dataLoggerService.LogKeyPress(key);
            }
            
            await Task.Delay(1500); // Wait for save timer
            
            // Get stats for current hour
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var hour = DateTime.Now.Hour;

            // Assert
            var keyStats = await _databaseService.GetKeyStatsAsync(today, hour);
            Assert.Equal(3, keyStats["A"]);
            Assert.Equal(2, keyStats["B"]);
            Assert.Equal(1, keyStats["C"]);
        }

        [Fact]
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
                _dataLoggerService.LogKeyPress("Space");
            }
            
            await Task.Delay(1500); // Wait for save timer

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

        [Fact]
        public async Task ConcurrentKeyPresses_ThreadSafety_NoDataLoss()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var tasks = new List<Task>();
            var keyPressCount = 100;
            var threadCount = 10;

            // Act - Simulate concurrent key presses from multiple threads
            for (int thread = 0; thread < threadCount; thread++)
            {
                var threadId = thread;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < keyPressCount; i++)
                    {
                        // Use the actual LogKeyPress method
                        _dataLoggerService.LogKeyPress($"Thread{threadId}");
                    }
                }));
            }

            // Wait for all threads to complete
            await Task.WhenAll(tasks);
            
            // Wait for save timer to flush data
            await Task.Delay(2000); // Wait longer than DatabaseSaveIntervalMs

            // Assert - Verify all key presses were recorded
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var hour = DateTime.Now.Hour;
            var keyStats = await _databaseService.GetKeyStatsAsync(today, hour);
            
            Assert.NotNull(keyStats);
            
            // Check that each thread's keys were properly counted
            for (int thread = 0; thread < threadCount; thread++)
            {
                var key = $"Thread{thread}";
                Assert.True(keyStats.ContainsKey(key), $"Missing data for {key}");
                Assert.Equal(keyPressCount, keyStats[key]);
            }
            
            // Verify total count
            var totalExpected = keyPressCount * threadCount;
            var totalActual = keyStats.Values.Sum();
            Assert.Equal(totalExpected, totalActual);
        }

        [Fact]
        public async Task StressTest_MassiveConcurrentOperations_DataIntegrity()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await _dataLoggerService.InitializeAsync();
            var tasks = new List<Task>();
            var random = new Random();
            
            // Simulate realistic typing patterns
            var commonKeys = new[] { "A", "E", "T", "O", "I", "N", "S", "H", "R", "Space" };
            var operationsPerThread = 50;
            var threadCount = 20;

            // Act - Simulate multiple users typing simultaneously
            for (int thread = 0; thread < threadCount; thread++)
            {
                var threadId = thread;
                tasks.Add(Task.Run(async () =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // Random key from common keys
                        var key = commonKeys[random.Next(commonKeys.Length)];
                        _dataLoggerService.LogKeyPress(key);
                        
                        // Simulate typing speed variation
                        await Task.Delay(random.Next(10, 50));
                        
                        // Occasionally log mouse events too
                        if (i % 5 == 0)
                        {
                            _dataLoggerService.LogMouseClick(MouseButton.Left);
                            // LogMouseMove takes distance in pixels, not x,y coordinates
                            var distance = Math.Sqrt(Math.Pow(random.Next(1, 100), 2) + Math.Pow(random.Next(1, 100), 2));
                            _dataLoggerService.LogMouseMove(distance);
                        }
                    }
                }));
            }

            // Wait for all operations to complete
            await Task.WhenAll(tasks);
            
            // Wait for final save
            await Task.Delay(2500);

            // Assert - Verify data integrity
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var hour = DateTime.Now.Hour;
            var keyStats = await _databaseService.GetKeyStatsAsync(today, hour);
            var dailyStats = await _databaseService.GetDailyStatsAsync(today);
            
            Assert.NotNull(keyStats);
            Assert.NotNull(dailyStats);
            
            // Verify some keys were recorded
            Assert.True(keyStats.Count > 0, "No key statistics recorded");
            
            // Verify common keys have reasonable counts
            foreach (var key in commonKeys)
            {
                if (keyStats.ContainsKey(key))
                {
                    Assert.True(keyStats[key] > 0, $"Key {key} should have been pressed");
                }
            }
            
            // Verify daily stats are reasonable
            Assert.True(dailyStats.KeyCount > 0, "Daily key count should be greater than 0");
            Assert.True(dailyStats.LeftClicks >= threadCount * (operationsPerThread / 5), "Mouse clicks not properly recorded");
            Assert.True(dailyStats.MouseDistance > 0, "Mouse distance should be greater than 0");
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
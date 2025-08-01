using FluentAssertions;
using KeyboardMouseOdometer.Core.Models;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Models;

public class DailyStatsTests
{
    [Fact]
    public void CreateForToday_ShouldReturnStatsWithTodaysDate()
    {
        // Act
        var stats = DailyStats.CreateForToday();

        // Assert
        stats.Date.Should().Be(DateTime.Today.ToString("yyyy-MM-dd"));
        stats.KeyCount.Should().Be(0);
        stats.MouseDistance.Should().Be(0.0);
        stats.LeftClicks.Should().Be(0);
        stats.RightClicks.Should().Be(0);
        stats.MiddleClicks.Should().Be(0);
    }

    [Fact]
    public void CreateForDate_ShouldReturnStatsWithSpecifiedDate()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);

        // Act
        var stats = DailyStats.CreateForDate(testDate);

        // Assert
        stats.Date.Should().Be("2024-01-15");
        stats.KeyCount.Should().Be(0);
        stats.MouseDistance.Should().Be(0.0);
        stats.TotalClicks.Should().Be(0);
    }

    [Fact]
    public void TotalClicks_ShouldReturnSumOfAllClicks()
    {
        // Arrange
        var stats = new DailyStats
        {
            LeftClicks = 10,
            RightClicks = 5,
            MiddleClicks = 2
        };

        // Act
        var totalClicks = stats.TotalClicks;

        // Assert
        totalClicks.Should().Be(17);
    }

    [Fact]
    public void GetDateTime_ShouldReturnCorrectDateTime()
    {
        // Arrange
        var stats = new DailyStats { Date = "2024-01-15" };

        // Act
        var dateTime = stats.GetDateTime();

        // Assert
        dateTime.Should().Be(new DateTime(2024, 1, 15));
    }

    [Theory]
    [InlineData("2024-01-01")]
    [InlineData("2023-12-31")]
    [InlineData("2024-06-15")]
    public void GetDateTime_ShouldParseVariousDatesCorrectly(string dateString)
    {
        // Arrange
        var stats = new DailyStats { Date = dateString };

        // Act
        var dateTime = stats.GetDateTime();

        // Assert
        var expectedDate = DateTime.ParseExact(dateString, "yyyy-MM-dd", null);
        dateTime.Should().Be(expectedDate);
    }
}
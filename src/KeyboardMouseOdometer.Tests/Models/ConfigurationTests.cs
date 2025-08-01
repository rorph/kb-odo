using FluentAssertions;
using KeyboardMouseOdometer.Core.Models;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Models;

public class ConfigurationTests
{
    [Fact]
    public void CreateDefault_ShouldReturnValidConfiguration()
    {
        // Act
        var config = Configuration.CreateDefault();

        // Assert
        config.Should().NotBeNull();
        config.TrackKeystrokes.Should().BeTrue();
        config.TrackMouseMovement.Should().BeTrue();
        config.TrackMouseClicks.Should().BeTrue();
        config.DatabaseRetentionDays.Should().Be(90);
        config.DistanceUnit.Should().Be("metric");
        config.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(90, true)]
    [InlineData(365, true)]
    [InlineData(3650, true)]
    [InlineData(0, true)]  // 0 = never delete, should be valid
    [InlineData(-1, false)]
    [InlineData(3651, false)]
    public void IsValid_ShouldValidateRetentionDays(int retentionDays, bool expectedValid)
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.DatabaseRetentionDays = retentionDays;

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("metric", true)]
    [InlineData("imperial", true)]
    [InlineData("pixels", true)]
    [InlineData("m", false)]
    [InlineData("km", false)]
    [InlineData("px", false)]
    [InlineData("meters", false)]
    [InlineData("kilometers", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldValidateDistanceUnit(string? distanceUnit, bool expectedValid)
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.DistanceUnit = distanceUnit!;

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(1000, true)]
    [InlineData(99, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsValid_ShouldValidateStatisticsUpdateInterval(int intervalMs, bool expectedValid)
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.StatisticsUpdateIntervalMs = intervalMs;

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(5000, true)]
    [InlineData(999, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsValid_ShouldValidateDatabaseSaveInterval(int intervalMs, bool expectedValid)
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.DatabaseSaveIntervalMs = intervalMs;

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("odometer.db", true)]
    [InlineData("data/stats.db", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    public void IsValid_ShouldValidateDatabasePath(string? databasePath, bool expectedValid)
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.DatabasePath = databasePath!;

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(0, 0)]  // 0 retention days should be valid (never delete)
    [InlineData(90, 0)]  // Normal retention should be valid
    [InlineData(-1, 1)]  // Negative retention should have 1 error
    [InlineData(4000, 1)]  // Too high retention should have 1 error
    public void GetValidationErrors_ShouldReturnCorrectErrorCount(int retentionDays, int expectedErrorCount)
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.DatabaseRetentionDays = retentionDays;

        // Act
        var errors = config.GetValidationErrors();

        // Assert
        errors.Should().HaveCount(expectedErrorCount);
    }

    [Fact]
    public void GetValidationErrors_ShouldReturnSpecificErrorForInvalidDistanceUnit()
    {
        // Arrange
        var config = Configuration.CreateDefault();
        config.DistanceUnit = "invalid";

        // Act
        var errors = config.GetValidationErrors();

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Contain("Distance unit must be 'metric', 'imperial', or 'pixels'")
            .And.Contain("invalid");
    }
}
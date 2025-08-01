using FluentAssertions;
using KeyboardMouseOdometer.Core.Utils;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Utils;

public class DistanceCalculatorTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 3, 4, 5)] // 3-4-5 triangle
    [InlineData(0, 0, 5, 12, 13)] // 5-12-13 triangle
    [InlineData(1, 1, 4, 5, 5)] // sqrt((4-1)² + (5-1)²) = sqrt(9+16) = 5
    public void CalculatePixelDistance_ShouldReturnCorrectDistance(int x1, int y1, int x2, int y2, double expected)
    {
        // Act
        var result = DistanceCalculator.CalculatePixelDistance(x1, y1, x2, y2);

        // Assert
        result.Should().BeApproximately(expected, 0.001);
    }

    [Theory]
    [InlineData(96, 96, 0.0254)] // 1 inch at 96 DPI = 0.0254 meters
    [InlineData(192, 96, 0.0508)] // 2 inches at 96 DPI = 0.0508 meters
    [InlineData(0, 96, 0)] // 0 pixels = 0 meters
    [InlineData(48, 96, 0.0127)] // 0.5 inches at 96 DPI = 0.0127 meters
    public void PixelsToMeters_ShouldConvertCorrectly(double pixels, double dpi, double expectedMeters)
    {
        // Act
        var result = DistanceCalculator.PixelsToMeters(pixels, dpi);

        // Assert
        result.Should().BeApproximately(expectedMeters, 0.0001);
    }

    [Theory]
    [InlineData(1000, 1)]
    [InlineData(2500, 2.5)]
    [InlineData(500, 0.5)]
    [InlineData(0, 0)]
    public void MetersToKilometers_ShouldConvertCorrectly(double meters, double expectedKilometers)
    {
        // Act
        var result = DistanceCalculator.MetersToKilometers(meters);

        // Assert
        result.Should().Be(expectedKilometers);
    }

    [Theory]
    [InlineData(0.5, "m", "0.50 m")]
    [InlineData(1.234, "m", "1.23 m")]
    [InlineData(123.456, "m", "123 m")]
    [InlineData(1234.5, "m", "1.23 km")]
    [InlineData(1000, "km", "1.00 km")]
    [InlineData(12345, "km", "12.35 km")]
    public void FormatDistance_ShouldFormatCorrectly(double meters, string unit, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistance(meters, unit);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.1, "0.10 m")]
    [InlineData(1.5, "1.5 m")]
    [InlineData(123.456, "123 m")]
    [InlineData(1234.5, "1.2 km")]
    [InlineData(12345, "12 km")]
    public void FormatDistanceAuto_ShouldChooseAppropriateUnit(double meters, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistanceAuto(meters);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, 3, 4, 96, 0.0013229)] // 5 pixels at 96 DPI ≈ 0.0013229 meters
    [InlineData(0, 0, 96, 0, 96, 0.0254)] // 96 pixels (1 inch) at 96 DPI = 0.0254 meters
    public void CalculateMouseTravelMeters_ShouldCalculateCorrectly(int x1, int y1, int x2, int y2, double dpi, double expectedMeters)
    {
        // Act
        var result = DistanceCalculator.CalculateMouseTravelMeters(x1, y1, x2, y2, dpi);

        // Assert
        result.Should().BeApproximately(expectedMeters, 0.000001);
    }

    [Theory]
    [InlineData(0, 0, 100, 100, true)] // Normal movement
    [InlineData(0, 0, 1000, 1000, true)] // Large but valid movement
    [InlineData(0, 0, 3000, 0, true)] // At the limit
    [InlineData(0, 0, 3001, 0, false)] // Over the limit
    [InlineData(0, 0, 5000, 5000, false)] // Way over the limit
    public void IsValidMouseMovement_ShouldValidateMovement(int x1, int y1, int x2, int y2, bool expected)
    {
        // Act
        var result = DistanceCalculator.IsValidMouseMovement(x1, y1, x2, y2);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void PixelsToMeters_WithNegativePixels_ShouldReturnZero()
    {
        // Act
        var result = DistanceCalculator.PixelsToMeters(-100);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void PixelsToMeters_WithZeroOrNegativeDpi_ShouldUseStandardDpi()
    {
        // Act
        var resultZeroDpi = DistanceCalculator.PixelsToMeters(96, 0);
        var resultNegativeDpi = DistanceCalculator.PixelsToMeters(96, -50);
        var expectedResult = DistanceCalculator.PixelsToMeters(96, DistanceCalculator.StandardDpi);

        // Assert
        resultZeroDpi.Should().Be(expectedResult);
        resultNegativeDpi.Should().Be(expectedResult);
    }

    // Tests for the new FormatDistanceAutoScale method

    [Theory]
    [InlineData(0, "metric", "0.0 mm")]
    [InlineData(0, "imperial", "0.00 in")]
    [InlineData(0, "pixels", "0.0 px")]
    [InlineData(-1, "metric", "0.0 mm")]
    [InlineData(-1, "imperial", "0.00 in")]
    [InlineData(-1, "pixels", "0.0 px")]
    public void FormatDistanceAutoScale_WithZeroOrNegativeDistance_ShouldReturnZero(double meters, string unitSystem, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistanceAutoScale(meters, unitSystem);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.001, "metric", "1.0 mm")] // Millimeters
    [InlineData(0.05, "metric", "50.0 mm")]
    [InlineData(0.1, "metric", "10.0 cm")] // Centimeters
    [InlineData(0.5, "metric", "50.0 cm")]
    [InlineData(1.0, "metric", "1.00 m")] // Meters
    [InlineData(50.5, "metric", "50.50 m")]
    [InlineData(150.0, "metric", "150 m")]
    [InlineData(1000.0, "metric", "1.00 km")] // Kilometers
    [InlineData(1500.0, "metric", "1.50 km")]
    [InlineData(150000.0, "metric", "150 km")]
    [InlineData(1000000.0, "metric", "1.00 Mm")] // Megameters
    [InlineData(2500000.0, "metric", "2.50 Mm")]
    public void FormatDistanceAutoScale_WithMetricSystem_ShouldAutoScale(double meters, string unitSystem, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistanceAutoScale(meters, unitSystem);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.001, "imperial", "0.04 in")] // Inches (very small)
    [InlineData(0.0254, "imperial", "1.00 in")] // 1 inch exactly
    [InlineData(0.1, "imperial", "3.94 in")]
    [InlineData(0.3048, "imperial", "1.00 ft")] // 1 foot exactly
    [InlineData(1.0, "imperial", "3.28 ft")] // Feet
    [InlineData(100.0, "imperial", "328 ft")]
    [InlineData(500.0, "imperial", "1640 ft")]
    [InlineData(1609.34, "imperial", "5280 ft")] // 1 mile = 5280 feet, but since < 5280 feet threshold, shows as feet
    [InlineData(3218.68, "imperial", "2.00 mi")] // Miles
    [InlineData(160934.0, "imperial", "100.00 mi")]
    public void FormatDistanceAutoScale_WithImperialSystem_ShouldAutoScale(double meters, string unitSystem, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistanceAutoScale(meters, unitSystem);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.0002645833, "pixels", "1.0 px")] // ~1 pixel at 96 DPI
    [InlineData(0.26458333, "pixels", "1000 px")] // This should be ~1000 pixels, not 1 Kpx
    [InlineData(264.58333, "pixels", "1000.0 Kpx")] // This should be ~1000 Kpx, not 1 Mpx
    [InlineData(264583.33, "pixels", "1000.0 Mpx")] // Should be ~1000 Mpx, not 1.0 Gpx
    [InlineData(0.001, "pixels", "3.8 px")]
    [InlineData(0.1, "pixels", "378 px")]
    [InlineData(1.0, "pixels", "3.8 Kpx")]
    [InlineData(100.0, "pixels", "378.0 Kpx")]
    [InlineData(1000.0, "pixels", "3.8 Mpx")]
    public void FormatDistanceAutoScale_WithPixelSystem_ShouldAutoScale(double meters, string unitSystem, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistanceAutoScale(meters, unitSystem);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1.0, "invalid", "1.00 m")] // Invalid unit system defaults to metric
    [InlineData(1.0, "", "1.00 m")] // Empty unit system defaults to metric
    [InlineData(1.0, "METRIC", "1.00 m")] // Case insensitive
    [InlineData(1.0, "Imperial", "3.28 ft")] // Case insensitive
    [InlineData(1.0, "PIXELS", "3.8 Kpx")] // Case insensitive - 1m ≈ 3779px ≈ 3.8 Kpx
    public void FormatDistanceAutoScale_WithEdgeCases_ShouldHandleCorrectly(double meters, string unitSystem, string expected)
    {
        // Act
        var result = DistanceCalculator.FormatDistanceAutoScale(meters, unitSystem);

        // Assert
        result.Should().Be(expected);
    }
}
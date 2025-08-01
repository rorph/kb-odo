namespace KeyboardMouseOdometer.Core.Utils;

/// <summary>
/// Utility class for calculating distances and converting units
/// </summary>
public static class DistanceCalculator
{
    /// <summary>
    /// Standard DPI for distance calculations (Windows default)
    /// </summary>
    public const double StandardDpi = 96.0;

    /// <summary>
    /// Inches to meters conversion factor
    /// </summary>
    public const double InchesToMeters = 0.0254;

    /// <summary>
    /// Meters to feet conversion factor
    /// </summary>
    public const double MetersToFeet = 3.28084;

    /// <summary>
    /// Feet to miles conversion factor
    /// </summary>
    public const double FeetToMiles = 1.0 / 5280.0;

    /// <summary>
    /// Calculate Euclidean distance between two points
    /// </summary>
    /// <param name="x1">First point X coordinate</param>
    /// <param name="y1">First point Y coordinate</param>
    /// <param name="x2">Second point X coordinate</param>
    /// <param name="y2">Second point Y coordinate</param>
    /// <returns>Distance in pixels</returns>
    public static double CalculatePixelDistance(int x1, int y1, int x2, int y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculate distance between two points using double precision
    /// </summary>
    public static double CalculatePixelDistance(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Convert pixels to meters using standard DPI
    /// </summary>
    /// <param name="pixels">Distance in pixels</param>
    /// <param name="dpi">DPI to use for conversion (defaults to 96)</param>
    /// <returns>Distance in meters</returns>
    public static double PixelsToMeters(double pixels, double dpi = StandardDpi)
    {
        if (pixels < 0) return 0;
        if (dpi <= 0) dpi = StandardDpi;
        
        // Convert pixels to inches, then inches to meters
        var inches = pixels / dpi;
        return inches * InchesToMeters;
    }

    /// <summary>
    /// Convert meters to kilometers
    /// </summary>
    /// <param name="meters">Distance in meters</param>
    /// <returns>Distance in kilometers</returns>
    public static double MetersToKilometers(double meters)
    {
        return meters / 1000.0;
    }

    /// <summary>
    /// Convert meters back to pixels using standard DPI
    /// </summary>
    /// <param name="meters">Distance in meters</param>
    /// <param name="dpi">DPI to use for conversion (defaults to 96)</param>
    /// <returns>Distance in pixels</returns>
    public static double MetersToPixels(double meters, double dpi = StandardDpi)
    {
        if (meters < 0) return 0;
        if (dpi <= 0) dpi = StandardDpi;
        
        // Convert meters to inches, then inches to pixels
        var inches = meters / InchesToMeters;
        return inches * dpi;
    }

    /// <summary>
    /// Format distance for display based on magnitude
    /// </summary>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <param name="preferredUnit">Preferred unit ("m", "km", or "px")</param>
    /// <param name="dpi">DPI for pixel conversion (defaults to 96)</param>
    /// <returns>Formatted distance string</returns>
    public static string FormatDistance(double distanceMeters, string preferredUnit = "m", double dpi = StandardDpi)
    {
        if (distanceMeters < 0) return "0 m";

        if (preferredUnit.ToLowerInvariant() == "px")
        {
            var pixels = MetersToPixels(distanceMeters, dpi);
            return pixels >= 10000 
                ? $"{pixels:F0} px" 
                : $"{pixels:F1} px";
        }
        else if (preferredUnit.ToLowerInvariant() == "km" || distanceMeters >= 1000)
        {
            var kilometers = MetersToKilometers(distanceMeters);
            return kilometers >= 100 
                ? $"{kilometers:F0} km" 
                : $"{kilometers:F2} km";
        }
        else
        {
            return distanceMeters >= 100 
                ? $"{distanceMeters:F0} m" 
                : $"{distanceMeters:F2} m";
        }
    }

    /// <summary>
    /// Format distance for display with automatic unit selection
    /// </summary>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <returns>Formatted distance string with appropriate unit</returns>
    public static string FormatDistanceAuto(double distanceMeters)
    {
        if (distanceMeters < 0) return "0 m";

        if (distanceMeters >= 10000) // 10+ km
        {
            return $"{MetersToKilometers(distanceMeters):F0} km";
        }
        else if (distanceMeters >= 1000) // 1-10 km
        {
            return $"{MetersToKilometers(distanceMeters):F1} km";
        }
        else if (distanceMeters >= 100) // 100+ m
        {
            return $"{distanceMeters:F0} m";
        }
        else if (distanceMeters >= 1) // 1+ m
        {
            return $"{distanceMeters:F1} m";
        }
        else // < 1 m
        {
            return $"{distanceMeters:F2} m";
        }
    }

    /// <summary>
    /// Format distance with automatic unit scaling based on magnitude
    /// </summary>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <param name="unitSystem">Unit system: "metric", "imperial", or "pixels"</param>
    /// <param name="dpi">DPI for pixel conversion (defaults to 96)</param>
    /// <returns>Formatted distance string with appropriate auto-scaled unit</returns>
    public static string FormatDistanceAutoScale(double distanceMeters, string unitSystem = "metric", double dpi = StandardDpi)
    {
        if (distanceMeters < 0) return GetZeroDistanceString(unitSystem);

        return unitSystem.ToLowerInvariant() switch
        {
            "pixels" => FormatPixelsAutoScale(distanceMeters, dpi),
            "imperial" => FormatImperialAutoScale(distanceMeters),
            "metric" or _ => FormatMetricAutoScale(distanceMeters)
        };
    }

    /// <summary>
    /// Format pixels with automatic scaling (px, Kpx, Mpx, Gpx)
    /// </summary>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <param name="dpi">DPI for conversion</param>
    /// <returns>Formatted pixel distance string</returns>
    private static string FormatPixelsAutoScale(double distanceMeters, double dpi)
    {
        var pixels = MetersToPixels(distanceMeters, dpi);

        if (pixels >= 1_000_000_000) // Gigapixels
        {
            var gpx = pixels / 1_000_000_000;
            return $"{gpx:F2} Gpx";
        }
        else if (pixels >= 1_000_000) // Megapixels
        {
            var mpx = pixels / 1_000_000;
            return $"{mpx:F1} Mpx";
        }
        else if (pixels >= 1_000) // Kilopixels
        {
            var kpx = pixels / 1_000;
            return $"{kpx:F1} Kpx";
        }
        else // Pixels
        {
            return pixels >= 100 ? $"{pixels:F0} px" : $"{pixels:F1} px";
        }
    }

    /// <summary>
    /// Format metric units with automatic scaling (mm, cm, m, km, Mm)
    /// </summary>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <returns>Formatted metric distance string</returns>
    private static string FormatMetricAutoScale(double distanceMeters)
    {
        if (distanceMeters >= 1_000_000) // Megameters
        {
            var mm = distanceMeters / 1_000_000;
            return $"{mm:F2} Mm";
        }
        else if (distanceMeters >= 1_000) // Kilometers
        {
            var km = distanceMeters / 1_000;
            return km >= 100 ? $"{km:F0} km" : $"{km:F2} km";
        }
        else if (distanceMeters >= 1) // Meters
        {
            return distanceMeters >= 100 ? $"{distanceMeters:F0} m" : $"{distanceMeters:F2} m";
        }
        else if (distanceMeters >= 0.1) // Centimeters
        {
            var cm = distanceMeters * 100;
            return $"{cm:F1} cm";
        }
        else // Millimeters
        {
            var mmValue = distanceMeters * 1000;
            return $"{mmValue:F1} mm";
        }
    }

    /// <summary>
    /// Format imperial units with automatic scaling (in, ft, mi)
    /// </summary>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <returns>Formatted imperial distance string</returns>
    private static string FormatImperialAutoScale(double distanceMeters)
    {
        var feet = distanceMeters * MetersToFeet;
        
        if (feet >= 5280) // Miles
        {
            var miles = feet * FeetToMiles;
            return miles >= 100 ? $"{miles:F0} mi" : $"{miles:F2} mi";
        }
        else if (feet >= 1) // Feet
        {
            return feet >= 100 ? $"{feet:F0} ft" : $"{feet:F2} ft";
        }
        else // Inches
        {
            var inches = feet * 12;
            return $"{inches:F2} in";
        }
    }

    /// <summary>
    /// Get zero distance string for the specified unit system
    /// </summary>
    /// <param name="unitSystem">Unit system</param>
    /// <returns>Zero distance string</returns>
    private static string GetZeroDistanceString(string unitSystem)
    {
        return unitSystem.ToLowerInvariant() switch
        {
            "pixels" => "0.0 px",
            "imperial" => "0.00 in",
            "metric" or _ => "0.0 mm"
        };
    }

    /// <summary>
    /// Calculate approximate mouse travel distance for multi-monitor setups
    /// </summary>
    /// <param name="x1">First point X coordinate</param>
    /// <param name="y1">First point Y coordinate</param>
    /// <param name="x2">Second point X coordinate</param>
    /// <param name="y2">Second point Y coordinate</param>
    /// <param name="dpi">DPI for conversion</param>
    /// <returns>Distance in meters</returns>
    public static double CalculateMouseTravelMeters(int x1, int y1, int x2, int y2, double dpi = StandardDpi)
    {
        var pixelDistance = CalculatePixelDistance(x1, y1, x2, y2);
        return PixelsToMeters(pixelDistance, dpi);
    }

    /// <summary>
    /// Calculate more realistic mouse travel distance considering mouse sensitivity
    /// This provides a scaling factor that's more reasonable for typical usage
    /// </summary>
    /// <param name="pixelDistance">Distance in pixels</param>
    /// <param name="mouseSensitivityFactor">Sensitivity factor (1.0 = normal, higher = more sensitive)</param>
    /// <param name="dpi">Display DPI</param>
    /// <returns>Scaled distance in meters</returns>
    public static double CalculateRealisticMouseTravelMeters(double pixelDistance, double mouseSensitivityFactor = 1.0, double dpi = StandardDpi)
    {
        if (pixelDistance <= 0) return 0;
        if (mouseSensitivityFactor <= 0) mouseSensitivityFactor = 1.0;
        
        // Apply sensitivity scaling - higher sensitivity means less physical movement needed
        var scaledPixels = pixelDistance / mouseSensitivityFactor;
        return PixelsToMeters(scaledPixels, dpi);
    }

    /// <summary>
    /// Calculate realistic scroll distance in meters
    /// </summary>
    /// <param name="wheelDelta">Windows wheel delta (typically 120 per notch)</param>
    /// <param name="scrollLinesPerNotch">Lines scrolled per notch (typically 3)</param>
    /// <param name="averageLineHeightCm">Average line height in centimeters (typically 0.5-1.0cm)</param>
    /// <returns>Scroll distance in meters</returns>
    public static double CalculateScrollDistanceMeters(int wheelDelta, int scrollLinesPerNotch = 3, double averageLineHeightCm = 0.8)
    {
        if (wheelDelta == 0) return 0;
        
        // Standard Windows: 120 units per notch
        var notches = Math.Abs(wheelDelta) / 120.0;
        var totalLines = notches * scrollLinesPerNotch;
        var totalDistanceCm = totalLines * averageLineHeightCm;
        
        return totalDistanceCm / 100.0; // Convert cm to meters
    }

    /// <summary>
    /// Validate if coordinates represent a reasonable mouse movement
    /// (helps filter out erratic movements or invalid coordinates)
    /// </summary>
    /// <param name="x1">First point X coordinate</param>
    /// <param name="y1">First point Y coordinate</param>
    /// <param name="x2">Second point X coordinate</param>
    /// <param name="y2">Second point Y coordinate</param>
    /// <param name="maxPixelDistance">Maximum reasonable pixel distance (default: 3000 for ultra-wide monitors)</param>
    /// <returns>True if movement seems valid</returns>
    public static bool IsValidMouseMovement(int x1, int y1, int x2, int y2, double maxPixelDistance = 3000)
    {
        var distance = CalculatePixelDistance(x1, y1, x2, y2);
        return distance <= maxPixelDistance && distance >= 0;
    }
}
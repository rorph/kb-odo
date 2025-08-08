namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Heatmap color scheme options
/// </summary>
public enum HeatmapColorScheme
{
    Classic,  // Blue -> Cyan -> Green -> Yellow -> Orange -> Red
    FLIR      // FLIR thermal imaging palette (Black -> Purple -> Red -> Orange -> Yellow -> White)
}

/// <summary>
/// Core-compatible color structure for heatmap visualization
/// </summary>
public struct HeatmapColor
{
    public byte A { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public HeatmapColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public static HeatmapColor FromArgb(byte a, byte r, byte g, byte b)
    {
        return new HeatmapColor(a, r, g, b);
    }

    /// <summary>
    /// Convert to hex string for XAML binding
    /// </summary>
    public string ToHexString()
    {
        return $"#{A:X2}{R:X2}{G:X2}{B:X2}";
    }

    /// <summary>
    /// Calculate color from heat level using specified color scheme
    /// </summary>
    public static HeatmapColor CalculateHeatColor(double heatLevel, HeatmapColorScheme colorScheme = HeatmapColorScheme.Classic)
    {
        // Clamp heat level between 0 and 1
        heatLevel = Math.Max(0, Math.Min(1, heatLevel));

        return colorScheme == HeatmapColorScheme.FLIR 
            ? CalculateFLIRColor(heatLevel) 
            : CalculateClassicColor(heatLevel);
    }

    /// <summary>
    /// Calculate color using classic gradient (Blue -> Cyan -> Green -> Yellow -> Orange -> Red)
    /// </summary>
    private static HeatmapColor CalculateClassicColor(double heatLevel)
    {
        byte r, g, b;

        if (heatLevel < 0.2)
        {
            // Blue to Cyan
            var t = heatLevel / 0.2;
            r = 0;
            g = (byte)(128 * t);
            b = 255;
        }
        else if (heatLevel < 0.4)
        {
            // Cyan to Green
            var t = (heatLevel - 0.2) / 0.2;
            r = 0;
            g = (byte)(128 + 127 * t);
            b = (byte)(255 * (1 - t));
        }
        else if (heatLevel < 0.6)
        {
            // Green to Yellow
            var t = (heatLevel - 0.4) / 0.2;
            r = (byte)(255 * t);
            g = 255;
            b = 0;
        }
        else if (heatLevel < 0.8)
        {
            // Yellow to Orange
            var t = (heatLevel - 0.6) / 0.2;
            r = 255;
            g = (byte)(255 * (1 - 0.5 * t));
            b = 0;
        }
        else
        {
            // Orange to Red
            var t = (heatLevel - 0.8) / 0.2;
            r = 255;
            g = (byte)(128 * (1 - t));
            b = 0;
        }

        return FromArgb(200, r, g, b); // Semi-transparent
    }

    /// <summary>
    /// Calculate color using FLIR thermal imaging palette
    /// Black -> Purple -> Blue -> Red -> Orange -> Yellow -> White
    /// </summary>
    private static HeatmapColor CalculateFLIRColor(double heatLevel)
    {
        byte r, g, b;

        if (heatLevel < 0.14)
        {
            // Black to Deep Purple
            var t = heatLevel / 0.14;
            r = (byte)(17 * t);
            g = 0;
            b = (byte)(36 * t);
        }
        else if (heatLevel < 0.28)
        {
            // Deep Purple to Blue
            var t = (heatLevel - 0.14) / 0.14;
            r = (byte)(17 + 53 * t);
            g = (byte)(7 * t);
            b = (byte)(36 + 100 * t);
        }
        else if (heatLevel < 0.42)
        {
            // Blue to Red
            var t = (heatLevel - 0.28) / 0.14;
            r = (byte)(70 + 138 * t);
            g = (byte)(7 * (1 - t));
            b = (byte)(136 * (1 - t));
        }
        else if (heatLevel < 0.56)
        {
            // Red to Dark Orange
            var t = (heatLevel - 0.42) / 0.14;
            r = (byte)(208 + 27 * t);
            g = (byte)(34 * t);
            b = 0;
        }
        else if (heatLevel < 0.70)
        {
            // Dark Orange to Orange
            var t = (heatLevel - 0.56) / 0.14;
            r = (byte)(235 + 20 * t);
            g = (byte)(34 + 103 * t);
            b = 0;
        }
        else if (heatLevel < 0.85)
        {
            // Orange to Yellow
            var t = (heatLevel - 0.70) / 0.15;
            r = 255;
            g = (byte)(137 + 100 * t);
            b = 0;
        }
        else
        {
            // Yellow to White
            var t = (heatLevel - 0.85) / 0.15;
            r = 255;
            g = (byte)(237 + 18 * t);
            b = (byte)(200 * t);
        }

        return FromArgb(200, r, g, b); // Semi-transparent
    }
}
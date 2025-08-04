namespace KeyboardMouseOdometer.Core.Models;

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
    /// Calculate color from heat level using gradient (Blue -> Cyan -> Green -> Yellow -> Orange -> Red)
    /// </summary>
    public static HeatmapColor CalculateHeatColor(double heatLevel)
    {
        // Clamp heat level between 0 and 1
        heatLevel = Math.Max(0, Math.Min(1, heatLevel));

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
}
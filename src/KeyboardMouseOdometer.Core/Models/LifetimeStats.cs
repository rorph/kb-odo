namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Represents lifetime aggregate statistics across all daily data
/// </summary>
public class LifetimeStats
{
    public long TotalKeys { get; set; } = 0;
    public double TotalMouseDistance { get; set; } = 0.0; // in meters
    public long TotalLeftClicks { get; set; } = 0;
    public long TotalRightClicks { get; set; } = 0;
    public long TotalMiddleClicks { get; set; } = 0;
    public double TotalScrollDistance { get; set; } = 0.0; // in meters
    public string? FirstDate { get; set; } // First day with data (YYYY-MM-DD)
    public string? LastDate { get; set; } // Last day with data (YYYY-MM-DD)
    public int TotalDays { get; set; } = 0; // Number of days with data

    public long TotalClicks => TotalLeftClicks + TotalRightClicks + TotalMiddleClicks;

    /// <summary>
    /// Gets the tracking period in a formatted string
    /// </summary>
    public string GetTrackingPeriod()
    {
        if (string.IsNullOrEmpty(FirstDate) || string.IsNullOrEmpty(LastDate))
            return "No data available";

        if (FirstDate == LastDate)
            return $"Since: {FirstDate}";

        return $"From: {FirstDate} to {LastDate}";
    }

    /// <summary>
    /// Gets the first tracking date as DateTime object
    /// </summary>
    public DateTime? GetFirstDateTime()
    {
        if (string.IsNullOrEmpty(FirstDate))
            return null;

        return DateTime.ParseExact(FirstDate, "yyyy-MM-dd", null);
    }

    /// <summary>
    /// Gets the last tracking date as DateTime object
    /// </summary>
    public DateTime? GetLastDateTime()
    {
        if (string.IsNullOrEmpty(LastDate))
            return null;

        return DateTime.ParseExact(LastDate, "yyyy-MM-dd", null);
    }
}
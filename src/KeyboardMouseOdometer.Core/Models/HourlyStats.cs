namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Model representing statistics for a specific hour of the day
/// </summary>
public class HourlyStats
{
    public string Date { get; set; } = string.Empty;
    public int Hour { get; set; }
    public int KeyCount { get; set; }
    public double MouseDistance { get; set; }
    public int LeftClicks { get; set; }
    public int RightClicks { get; set; }
    public int MiddleClicks { get; set; }
    public double ScrollDistance { get; set; }

    public int TotalClicks => LeftClicks + RightClicks + MiddleClicks;

    /// <summary>
    /// Create empty hourly stats for a specific date and hour
    /// </summary>
    public static HourlyStats CreateEmpty(string date, int hour)
    {
        return new HourlyStats
        {
            Date = date,
            Hour = hour,
            KeyCount = 0,
            MouseDistance = 0,
            LeftClicks = 0,
            RightClicks = 0,
            MiddleClicks = 0,
            ScrollDistance = 0
        };
    }
}
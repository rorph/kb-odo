namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Represents daily aggregated statistics as per PROJECT_SPEC database schema
/// </summary>
public class DailyStats
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD format
    public int KeyCount { get; set; } = 0;
    public double MouseDistance { get; set; } = 0.0; // in meters
    public int LeftClicks { get; set; } = 0;
    public int RightClicks { get; set; } = 0;
    public int MiddleClicks { get; set; } = 0;
    public double ScrollDistance { get; set; } = 0.0; // in meters

    public int TotalClicks => LeftClicks + RightClicks + MiddleClicks;

    /// <summary>
    /// Creates a DailyStats for today with zero values
    /// </summary>
    public static DailyStats CreateForToday()
    {
        return new DailyStats
        {
            Date = DateTime.Today.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>
    /// Creates a DailyStats for a specific date with zero values
    /// </summary>
    public static DailyStats CreateForDate(DateTime date)
    {
        return new DailyStats
        {
            Date = date.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>
    /// Gets the date as DateTime object
    /// </summary>
    public DateTime GetDateTime()
    {
        return DateTime.ParseExact(Date, "yyyy-MM-dd", null);
    }
}

/// <summary>
/// Represents a raw key/mouse event for optional detailed logging
/// </summary>
public class KeyMouseEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty; // key_down, mouse_move, mouse_click, mouse_scroll
    public string? Key { get; set; }
    public double? MouseDx { get; set; }
    public double? MouseDy { get; set; }
    public string? MouseButton { get; set; }
    public int? WheelDelta { get; set; }
}
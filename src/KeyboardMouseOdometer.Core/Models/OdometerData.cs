namespace KeyboardMouseOdometer.Core.Models;

public class OdometerData
{
    public DateTime Timestamp { get; set; }
    public long Keystrokes { get; set; }
    public long MouseClicks { get; set; }
    public double MouseDistance { get; set; }
    public long ScrollWheelTicks { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public Dictionary<string, int> KeyFrequency { get; set; } = new();
    public Dictionary<string, int> MouseButtonFrequency { get; set; } = new();

    public long TotalInputs => Keystrokes + MouseClicks + ScrollWheelTicks;
}

public class SessionStatistics
{
    public DateTime SessionStart { get; set; }
    public DateTime SessionEnd { get; set; }
    public TimeSpan Duration { get; set; }
    public long TotalKeystrokes { get; set; }
    public long TotalMouseClicks { get; set; }
    public double TotalMouseDistance { get; set; }
    public long TotalScrollTicks { get; set; }
    public double AverageKeystrokesPerMinute { get; set; }
    public double AverageMouseClicksPerMinute { get; set; }
    public Dictionary<string, int> TopKeys { get; set; } = new();
    public Dictionary<string, int> MouseButtonStats { get; set; } = new();
}

public class DailyStatistics
{
    public DateTime Date { get; set; }
    public long TotalKeystrokes { get; set; }
    public long TotalMouseClicks { get; set; }
    public double TotalMouseDistance { get; set; }
    public long TotalScrollTicks { get; set; }
    public TimeSpan TotalActiveTime { get; set; }
    public int SessionsCount { get; set; }
    public List<SessionStatistics> Sessions { get; set; } = new();
}
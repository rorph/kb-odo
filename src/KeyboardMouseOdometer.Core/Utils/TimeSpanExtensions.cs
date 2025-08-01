namespace KeyboardMouseOdometer.Core.Utils;

public static class TimeSpanExtensions
{
    public static string ToFriendlyString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        
        return $"{timeSpan.Seconds}s";
    }

    public static string ToShortString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h";
        
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        
        return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
    }
}

public static class NumberExtensions
{
    public static string ToFriendlyString(this long number)
    {
        if (number >= 1_000_000)
            return $"{number / 1_000_000.0:F1}M";
        
        if (number >= 1_000)
            return $"{number / 1_000.0:F1}K";
        
        return number.ToString();
    }

    public static string ToDistanceString(this double distance)
    {
        if (distance >= 1000)
            return $"{distance / 1000:F2} km";
        
        return $"{distance:F1} m";
    }
}
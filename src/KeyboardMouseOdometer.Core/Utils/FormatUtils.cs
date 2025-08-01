namespace KeyboardMouseOdometer.Core.Utils;

/// <summary>
/// Utility class for formatting values for display
/// </summary>
public static class FormatUtils
{
    /// <summary>
    /// Format number with appropriate thousands separators
    /// </summary>
    /// <param name="number">Number to format</param>
    /// <returns>Formatted number string</returns>
    public static string FormatNumber(long number)
    {
        return number.ToString("N0");
    }

    /// <summary>
    /// Format key code for display (clean up common key names)
    /// </summary>
    /// <param name="keyCode">Raw key code</param>
    /// <returns>User-friendly key name</returns>
    public static string FormatKeyCode(string keyCode)
    {
        if (string.IsNullOrWhiteSpace(keyCode))
            return string.Empty;

        // Clean up common key codes
        return keyCode switch
        {
            "Space" => "Space",
            "Return" => "Enter",
            "Back" => "Backspace",
            "Tab" => "Tab",
            "Escape" => "Esc",
            "Delete" => "Del",
            "Insert" => "Ins",
            "Home" => "Home",
            "End" => "End",
            "Prior" => "Page Up",
            "Next" => "Page Down",
            "Left" => "←",
            "Right" => "→",
            "Up" => "↑",
            "Down" => "↓",
            "LShiftKey" => "Shift",
            "RShiftKey" => "Shift",
            "LControlKey" => "Ctrl",
            "RControlKey" => "Ctrl",
            "LMenu" => "Alt",
            "RMenu" => "Alt",
            "LWin" => "Win",
            "RWin" => "Win",
            _ => keyCode.Length == 1 ? keyCode.ToUpperInvariant() : keyCode
        };
    }

    /// <summary>
    /// Format time span for display
    /// </summary>
    /// <param name="timeSpan">Time span to format</param>
    /// <returns>Formatted time string</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
            return $"{timeSpan.Seconds}s";
        }
    }

    /// <summary>
    /// Format rate (items per time unit)
    /// </summary>
    /// <param name="count">Number of items</param>
    /// <param name="timeSpan">Time period</param>
    /// <param name="unit">Time unit for display</param>
    /// <returns>Formatted rate string</returns>
    public static string FormatRate(long count, TimeSpan timeSpan, string unit = "min")
    {
        if (timeSpan.TotalSeconds <= 0)
            return "0";

        double rate = unit.ToLowerInvariant() switch
        {
            "sec" => count / timeSpan.TotalSeconds,
            "min" => count / timeSpan.TotalMinutes,
            "hour" => count / timeSpan.TotalHours,
            _ => count / timeSpan.TotalMinutes
        };

        return rate >= 100 ? $"{rate:F0}" : $"{rate:F1}";
    }

    /// <summary>
    /// Truncate text to specified length with ellipsis
    /// </summary>
    /// <param name="text">Text to truncate</param>
    /// <param name="maxLength">Maximum length</param>
    /// <returns>Truncated text</returns>
    public static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }
}
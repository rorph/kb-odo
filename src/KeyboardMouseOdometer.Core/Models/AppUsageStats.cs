namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Represents application usage statistics
/// </summary>
public class AppUsageStats
{
    /// <summary>
    /// The name of the application (executable name)
    /// </summary>
    public string AppName { get; set; } = string.Empty;
    
    /// <summary>
    /// Total seconds the application was in focus
    /// </summary>
    public int SecondsUsed { get; set; }
    
    /// <summary>
    /// Gets the usage time formatted as a readable string
    /// </summary>
    public string FormattedTime
    {
        get
        {
            if (SecondsUsed < 60)
                return $"{SecondsUsed}s";
            
            var minutes = SecondsUsed / 60;
            if (minutes < 60)
                return $"{minutes}m {SecondsUsed % 60}s";
            
            var hours = minutes / 60;
            minutes = minutes % 60;
            return $"{hours}h {minutes}m";
        }
    }
}
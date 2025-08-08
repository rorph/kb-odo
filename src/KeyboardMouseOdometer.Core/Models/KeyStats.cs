namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Model representing statistics for an individual key press within a specific hour
/// Used for heatmap visualization
/// </summary>
public class KeyStats
{
    public string Date { get; set; } = string.Empty;
    public int Hour { get; set; }
    public string KeyCode { get; set; } = string.Empty;
    public int Count { get; set; }
}
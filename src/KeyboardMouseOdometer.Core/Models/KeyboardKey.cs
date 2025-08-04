namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Represents a single key on the keyboard for heatmap visualization
/// </summary>
public class KeyboardKey
{
    /// <summary>
    /// The virtual key code
    /// </summary>
    public CoreKeyCode KeyCode { get; set; }
    
    /// <summary>
    /// Key name for database matching (e.g., "A", "Enter", "Space")
    /// </summary>
    public string KeyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Display text on the key (e.g., "A", "Enter", "Shift")
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;
    
    /// <summary>
    /// Alternative display text (e.g., for shifted characters)
    /// </summary>
    public string? AlternateText { get; set; }
    
    /// <summary>
    /// X position in grid units (0-based)
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Y position in grid units (0-based)
    /// </summary>
    public double Y { get; set; }
    
    /// <summary>
    /// Width in grid units (1.0 = standard key width)
    /// </summary>
    public double Width { get; set; } = 1.0;
    
    /// <summary>
    /// Height in grid units (1.0 = standard key height)
    /// </summary>
    public double Height { get; set; } = 1.0;
    
    /// <summary>
    /// Number of times this key has been pressed
    /// </summary>
    public long PressCount { get; set; }
    
    /// <summary>
    /// Heat level (0.0 to 1.0) for color gradient
    /// </summary>
    public double HeatLevel { get; set; }
    
    /// <summary>
    /// Whether this key is currently highlighted
    /// </summary>
    public bool IsHighlighted { get; set; }
    
    /// <summary>
    /// Key category for styling (e.g., "letter", "number", "function", "modifier")
    /// </summary>
    public string Category { get; set; } = "standard";
}
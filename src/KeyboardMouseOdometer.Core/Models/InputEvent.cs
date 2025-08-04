namespace KeyboardMouseOdometer.Core.Models;

public enum InputEventType
{
    KeyDown,
    KeyUp,
    KeyPress = KeyDown, // Alias for compatibility
    MouseMove,
    MouseClick,
    MouseWheel
}

public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

public class InputEvent
{
    public DateTime Timestamp { get; set; }
    public InputEventType EventType { get; set; }
    /// <summary>
    /// Alias for EventType for compatibility
    /// </summary>
    public InputEventType Type 
    { 
        get => EventType; 
        set => EventType = value; 
    }
    public string? Data { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public MouseButton? MouseButton { get; set; }
    public int WheelDelta { get; set; }
    public string? KeyCode { get; set; }
    /// <summary>
    /// Human-readable key identifier for heatmap tracking (e.g., "A", "Space", "Enter")
    /// </summary>
    public string? KeyIdentifier { get; set; }
}

public class MousePosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime Timestamp { get; set; }

    public double DistanceTo(MousePosition other)
    {
        return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }
}
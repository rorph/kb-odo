using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Utils;
using KeyboardMouseOdometer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace KeyboardMouseOdometer.Core.Services;

/// <summary>
/// Service for monitoring global keyboard and mouse input using Windows API
/// This is the core implementation that will be wrapped by the UI project's hook service
/// </summary>
public class InputMonitoringService
{
    public event EventHandler<KeyboardEventArgs>? KeyPressed;
    public event EventHandler<MouseMoveEventArgs>? MouseMoved;
    public event EventHandler<MouseClickEventArgs>? MouseClicked;
    public event EventHandler<MouseScrollEventArgs>? MouseScrolled;

    private readonly ILogger<InputMonitoringService> _logger;
    private readonly IKeyCodeMapper _keyCodeMapper;
    private MousePosition? _lastMousePosition;
    private DateTime _lastMouseMoveTime = DateTime.Now;

    public InputMonitoringService(ILogger<InputMonitoringService> logger, IKeyCodeMapper keyCodeMapper)
    {
        _logger = logger;
        _keyCodeMapper = keyCodeMapper;
    }

    /// <summary>
    /// Process a keyboard event
    /// </summary>
    public void ProcessKeyboardEvent(string keyCode, bool isKeyDown)
    {
        if (isKeyDown) // Only track key down events to avoid duplicates
        {
            var keyIdentifier = _keyCodeMapper.GetKeyName(keyCode);
            
            var args = new KeyboardEventArgs
            {
                KeyCode = keyCode,
                KeyIdentifier = keyIdentifier,
                Timestamp = DateTime.Now
            };

            KeyPressed?.Invoke(this, args);
            // Removed trace logging to reduce CPU usage
        }
    }

    /// <summary>
    /// Process a mouse movement event
    /// </summary>
    public void ProcessMouseMoveEvent(int x, int y)
    {
        var currentPosition = new MousePosition
        {
            X = x,
            Y = y,
            Timestamp = DateTime.Now
        };

        double distance = 0;
        if (_lastMousePosition != null)
        {
            distance = _lastMousePosition.DistanceTo(currentPosition);
        }

        var args = new MouseMoveEventArgs
        {
            X = x,
            Y = y,
            Distance = distance,
            Timestamp = currentPosition.Timestamp
        };

        MouseMoved?.Invoke(this, args);
        
        _lastMousePosition = currentPosition;
        _lastMouseMoveTime = DateTime.Now;
        
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Process a mouse click event
    /// </summary>
    public void ProcessMouseClickEvent(MouseButton button, int x, int y)
    {
        var args = new MouseClickEventArgs
        {
            Button = button,
            X = x,
            Y = y,
            Timestamp = DateTime.Now
        };

        MouseClicked?.Invoke(this, args);
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Process a mouse scroll event
    /// </summary>
    public void ProcessMouseScrollEvent(int wheelDelta, int x, int y)
    {
        var args = new MouseScrollEventArgs
        {
            WheelDelta = wheelDelta,
            X = x,
            Y = y,
            Timestamp = DateTime.Now
        };

        MouseScrolled?.Invoke(this, args);
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Get the time since last mouse movement (for idle detection)
    /// </summary>
    public TimeSpan TimeSinceLastMouseMove => DateTime.Now - _lastMouseMoveTime;
}

// Event argument classes
public class KeyboardEventArgs : EventArgs
{
    public string KeyCode { get; set; } = string.Empty;
    public string KeyIdentifier { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class MouseMoveEventArgs : EventArgs
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Distance { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MouseClickEventArgs : EventArgs
{
    public MouseButton Button { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MouseScrollEventArgs : EventArgs
{
    public int WheelDelta { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime Timestamp { get; set; }
}
using Gma.System.MouseKeyHook;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace KeyboardMouseOdometer.UI.Services;

/// <summary>
/// Service that implements global keyboard and mouse hooks using GlobalMouseKeyHook library
/// </summary>
public class GlobalHookService : IDisposable
{
    private readonly ILogger<GlobalHookService> _logger;
    private readonly InputMonitoringService _inputMonitoringService;
    private readonly DataLoggerService _dataLoggerService;
    private readonly Core.Models.Configuration _configuration;
    
    private IKeyboardMouseEvents? _globalHook;
    private bool _isRunning = false;
    private DateTime _lastMouseProcessTime = DateTime.MinValue;

    public bool IsRunning => _isRunning;

    public GlobalHookService(
        ILogger<GlobalHookService> logger,
        InputMonitoringService inputMonitoringService,
        DataLoggerService dataLoggerService,
        Core.Models.Configuration configuration)
    {
        _logger = logger;
        _inputMonitoringService = inputMonitoringService;
        _dataLoggerService = dataLoggerService;
        _configuration = configuration;

        // Subscribe to input monitoring events
        _inputMonitoringService.KeyPressed += OnKeyPressed;
        _inputMonitoringService.MouseMoved += OnMouseMoved;
        _inputMonitoringService.MouseClicked += OnMouseClicked;
        _inputMonitoringService.MouseScrolled += OnMouseScrolled;
    }

    /// <summary>
    /// Start global keyboard and mouse hooks with comprehensive error handling
    /// </summary>
    public void StartHooks()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Hooks are already running");
            return;
        }

        try
        {
            _logger.LogInformation("Attempting to create global hooks...");
            
            // Test if we can create the hook - this is where most failures occur
            _globalHook = Hook.GlobalEvents();
            
            if (_globalHook == null)
            {
                throw new InvalidOperationException("Failed to create global hook - Hook.GlobalEvents() returned null");
            }
            
            _logger.LogInformation("Global hook object created successfully");

            // Subscribe to events based on configuration with individual error handling
            if (_configuration.TrackKeystrokes)
            {
                try
                {
                    _globalHook.KeyDown += OnGlobalKeyDown;
                    _logger.LogDebug("Keyboard hook event subscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to subscribe to keyboard events");
                    throw new InvalidOperationException("Failed to setup keyboard hook", ex);
                }
            }

            if (_configuration.TrackMouseMovement)
            {
                try
                {
                    _globalHook.MouseMove += OnGlobalMouseMove;
                    _logger.LogDebug("Mouse movement hook event subscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to subscribe to mouse move events");
                    throw new InvalidOperationException("Failed to setup mouse movement hook", ex);
                }
            }

            if (_configuration.TrackMouseClicks)
            {
                try
                {
                    _globalHook.MouseDown += OnGlobalMouseDown;
                    _logger.LogDebug("Mouse click hook event subscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to subscribe to mouse click events");
                    throw new InvalidOperationException("Failed to setup mouse click hook", ex);
                }
            }

            if (_configuration.TrackScrollWheel)
            {
                try
                {
                    _globalHook.MouseWheel += OnGlobalMouseWheel;
                    _logger.LogDebug("Mouse wheel hook event subscribed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to subscribe to mouse wheel events");
                    throw new InvalidOperationException("Failed to setup mouse wheel hook", ex);
                }
            }

            _isRunning = true;
            _logger.LogInformation("Global hooks started successfully with configuration: Keystrokes={KeyTracking}, MouseMove={MouseMove}, MouseClick={MouseClick}, ScrollWheel={ScrollWheel}", 
                _configuration.TrackKeystrokes, _configuration.TrackMouseMovement, _configuration.TrackMouseClicks, _configuration.TrackScrollWheel);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when creating global hooks - insufficient permissions");
            CleanupFailedHook();
            throw new UnauthorizedAccessException("Global hooks require elevated permissions or may be blocked by security software. Try running as Administrator.", ex);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Win32 error when creating global hooks - Windows API failure");
            CleanupFailedHook();
            throw new InvalidOperationException($"Windows API error during hook creation: {ex.Message} (Error Code: {ex.ErrorCode}). This may indicate missing system components or security software interference.", ex);
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogError(ex, "Required DLL not found for global hooks");
            CleanupFailedHook();
            throw new InvalidOperationException("Required system libraries not found. This may indicate missing Visual C++ redistributables or corrupted Windows installation.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when starting global hooks: {ErrorType}", ex.GetType().Name);
            CleanupFailedHook();
            throw new InvalidOperationException($"Failed to initialize global input hooks: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Clean up any partially initialized hook resources
    /// </summary>
    private void CleanupFailedHook()
    {
        try
        {
            if (_globalHook != null)
            {
                _globalHook.Dispose();
                _globalHook = null;
            }
            _isRunning = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during hook cleanup after failed initialization");
        }
    }
    
    /// <summary>
    /// Test if global hooks can be created without actually starting them
    /// </summary>
    public bool CanCreateHooks()
    {
        try
        {
            _logger.LogDebug("Testing hook creation capability...");
            using var testHook = Hook.GlobalEvents();
            _logger.LogDebug("Hook creation test successful");
            return testHook != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hook creation test failed: {ErrorType}", ex.GetType().Name);
            return false;
        }
    }

    /// <summary>
    /// Stop global keyboard and mouse hooks
    /// </summary>
    public void StopHooks()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Hooks are not running");
            return;
        }

        try
        {
            if (_globalHook != null)
            {
                // Unsubscribe from events
                _globalHook.KeyDown -= OnGlobalKeyDown;
                _globalHook.MouseMove -= OnGlobalMouseMove;
                _globalHook.MouseDown -= OnGlobalMouseDown;
                _globalHook.MouseWheel -= OnGlobalMouseWheel;

                // Dispose the hook
                _globalHook.Dispose();
                _globalHook = null;
            }

            _isRunning = false;
            _logger.LogInformation("Global hooks stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop global hooks");
        }
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (_configuration.TrackKeystrokes)
            {
                // Try to get more specific key information if available
                string keyCode;
                
                // Check if this is an extended key event with virtual key code
                if (e is KeyEventArgsExt extArgs)
                {
                    // Use the virtual key code for more accurate key identification
                    keyCode = GetKeyStringFromVirtualCode(extArgs.KeyValue);
                    
                    // Debug logging for NumPad keys
                    if (e.KeyCode.ToString().StartsWith("NumPad") || extArgs.KeyValue >= 0x60 && extArgs.KeyValue <= 0x6F)
                    {
                        _logger.LogDebug("NumPad key detected - KeyCode: {KeyCode}, VirtualKey: 0x{VirtualKey:X2}, Mapped: {Mapped}", 
                            e.KeyCode, extArgs.KeyValue, keyCode);
                    }
                }
                else
                {
                    // Fallback to standard key code
                    keyCode = GetKeyStringFromKeyCode(e.KeyCode);
                    
                    // Debug logging for NumPad keys
                    if (e.KeyCode.ToString().StartsWith("NumPad"))
                    {
                        _logger.LogDebug("NumPad key detected (non-ext) - KeyCode: {KeyCode}, Mapped: {Mapped}", 
                            e.KeyCode, keyCode);
                    }
                }
                
                _inputMonitoringService.ProcessKeyboardEvent(keyCode, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing key down event for key: {KeyCode}", e.KeyCode);
        }
    }
    
    /// <summary>
    /// Convert a virtual key code to the appropriate key string
    /// </summary>
    private string GetKeyStringFromVirtualCode(int virtualKeyCode)
    {
        // Map virtual key codes to their specific keys
        // This properly distinguishes left/right modifiers and numpad keys
        return virtualKeyCode switch
        {
            // Left/Right Shift
            0xA0 => "LShiftKey",  // VK_LSHIFT
            0xA1 => "RShiftKey",  // VK_RSHIFT
            
            // Left/Right Control
            0xA2 => "LControlKey", // VK_LCONTROL
            0xA3 => "RControlKey", // VK_RCONTROL
            
            // Left/Right Alt
            0xA4 => "LMenu",      // VK_LMENU (Left Alt)
            0xA5 => "RMenu",      // VK_RMENU (Right Alt)
            
            // Left/Right Windows
            0x5B => "LWin",       // VK_LWIN
            0x5C => "RWin",       // VK_RWIN
            
            // Numpad keys - Important: These must match CoreKeyCode enum names
            0x60 => "NumPad0",    // VK_NUMPAD0
            0x61 => "NumPad1",    // VK_NUMPAD1
            0x62 => "NumPad2",    // VK_NUMPAD2
            0x63 => "NumPad3",    // VK_NUMPAD3
            0x64 => "NumPad4",    // VK_NUMPAD4
            0x65 => "NumPad5",    // VK_NUMPAD5
            0x66 => "NumPad6",    // VK_NUMPAD6
            0x67 => "NumPad7",    // VK_NUMPAD7
            0x68 => "NumPad8",    // VK_NUMPAD8
            0x69 => "NumPad9",    // VK_NUMPAD9
            0x6A => "Multiply",   // VK_MULTIPLY (Numpad *)
            0x6B => "Add",        // VK_ADD (Numpad +)
            0x6C => "Separator",  // VK_SEPARATOR
            0x6D => "Subtract",   // VK_SUBTRACT (Numpad -)
            0x6E => "Decimal",    // VK_DECIMAL (Numpad .)
            0x6F => "Divide",     // VK_DIVIDE (Numpad /)
            
            // NumLock, CapsLock, ScrollLock
            0x90 => "NumLock",    // VK_NUMLOCK
            0x14 => "Capital",    // VK_CAPITAL (Caps Lock)
            0x91 => "Scroll",     // VK_SCROLL
            
            // Default: try to convert from the key code with OEM fixes
            _ => GetKeyStringFromKeyCode((Keys)virtualKeyCode)
        };
    }
    
    /// <summary>
    /// Convert a Keys enum to the appropriate key string
    /// </summary>
    private string GetKeyStringFromKeyCode(Keys keyCode)
    {
        // Handle special cases and OEM key mappings
        return keyCode switch
        {
            // Modifier keys - default to left if not specified
            Keys.ShiftKey => "LShiftKey",
            Keys.ControlKey => "LControlKey",
            Keys.Menu => "LMenu",
            
            // OEM keys - map to their actual meanings
            // Note: Keys.Oemtilde is the same as Keys.Oem3, Keys.OemOpenBrackets is Keys.Oem4, etc.
            Keys.Oem1 => "OemSemicolon",        // ; :
            Keys.Oem2 => "OemQuestion",         // / ?
            Keys.Oem3 => "OemTilde",            // ~ `
            Keys.Oem4 => "OemOpenBrackets",     // [
            Keys.Oem5 => "OemPipe",             // \ |
            Keys.Oem6 => "OemCloseBrackets",    // ]
            Keys.Oem7 => "OemQuotes",           // ' "
            Keys.OemMinus => "OemMinus",        // - _
            Keys.Oemplus => "OemPlus",          // = +
            Keys.Oemcomma => "OemComma",        // , <
            Keys.OemPeriod => "OemPeriod",      // . >
            
            // Navigation keys
            Keys.Next => "PageDown",            // Page Down key
            Keys.Prior => "PageUp",             // Page Up key
            
            // NumPad keys
            Keys.NumPad0 => "NumPad0",
            Keys.NumPad1 => "NumPad1",
            Keys.NumPad2 => "NumPad2",
            Keys.NumPad3 => "NumPad3",
            Keys.NumPad4 => "NumPad4",
            Keys.NumPad5 => "NumPad5",
            Keys.NumPad6 => "NumPad6",
            Keys.NumPad7 => "NumPad7",
            Keys.NumPad8 => "NumPad8",
            Keys.NumPad9 => "NumPad9",
            Keys.Multiply => "Multiply",
            Keys.Add => "Add",
            Keys.Subtract => "Subtract",
            Keys.Decimal => "Decimal",
            Keys.Divide => "Divide",
            Keys.NumLock => "NumLock",
            
            _ => keyCode.ToString()
        };
    }

    private void OnGlobalMouseMove(object? sender, MouseEventArgs e)
    {
        try
        {
            if (_configuration.TrackMouseMovement)
            {
                // Throttle mouse movement processing to reduce CPU usage
                var now = DateTime.Now;
                if ((now - _lastMouseProcessTime).TotalMilliseconds >= _configuration.MouseMovementThrottleMs)
                {
                    _lastMouseProcessTime = now;
                    _inputMonitoringService.ProcessMouseMoveEvent(e.X, e.Y);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mouse move event at ({X}, {Y})", e.X, e.Y);
        }
    }

    private void OnGlobalMouseDown(object? sender, MouseEventArgs e)
    {
        try
        {
            if (_configuration.TrackMouseClicks)
            {
                var button = e.Button switch
                {
                    MouseButtons.Left => MouseButton.Left,
                    MouseButtons.Right => MouseButton.Right,
                    MouseButtons.Middle => MouseButton.Middle,
                    MouseButtons.XButton1 => MouseButton.XButton1,
                    MouseButtons.XButton2 => MouseButton.XButton2,
                    _ => MouseButton.Left
                };

                _inputMonitoringService.ProcessMouseClickEvent(button, e.X, e.Y);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mouse click event: {Button} at ({X}, {Y})", e.Button, e.X, e.Y);
        }
    }

    private void OnGlobalMouseWheel(object? sender, MouseEventArgs e)
    {
        try
        {
            if (_configuration.TrackScrollWheel)
            {
                _inputMonitoringService.ProcessMouseScrollEvent(e.Delta, e.X, e.Y);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mouse wheel event");
        }
    }

    // Event handlers for logging to database
    private void OnKeyPressed(object? sender, KeyboardEventArgs e)
    {
        _dataLoggerService.LogKeyPress(e.KeyCode);
    }

    private void OnMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        _dataLoggerService.LogMouseMove(e.Distance);
    }

    private void OnMouseClicked(object? sender, MouseClickEventArgs e)
    {
        _dataLoggerService.LogMouseClick(e.Button);
    }

    private void OnMouseScrolled(object? sender, MouseScrollEventArgs e)
    {
        try
        {
            // Log the scroll event to DataLoggerService
            _dataLoggerService.LogMouseScroll(e.WheelDelta);
            _logger.LogTrace("Mouse scrolled: {Delta} at ({X}, {Y})", e.WheelDelta, e.X, e.Y);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mouse scroll event");
        }
    }

    public void Dispose()
    {
        try
        {
            StopHooks();
            
            // Unsubscribe from input monitoring events
            _inputMonitoringService.KeyPressed -= OnKeyPressed;
            _inputMonitoringService.MouseMoved -= OnMouseMoved;
            _inputMonitoringService.MouseClicked -= OnMouseClicked;
            _inputMonitoringService.MouseScrolled -= OnMouseScrolled;
            
            _logger.LogInformation("GlobalHookService disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GlobalHookService disposal");
        }
    }
}
using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.Core.Interfaces;

/// <summary>
/// Interface for mapping key codes to human-readable names
/// </summary>
public interface IKeyCodeMapper
{
    /// <summary>
    /// Convert a CoreKeyCode to a human-readable string
    /// </summary>
    string GetKeyName(CoreKeyCode keyCode);
    
    /// <summary>
    /// Convert a key code string to a human-readable string
    /// </summary>
    string GetKeyName(string? keyCode);
    
    /// <summary>
    /// Convert a virtual key code (int) to a human-readable string
    /// </summary>
    string GetKeyName(int virtualKeyCode);
    
    /// <summary>
    /// Get all mapped keys for keyboard layout
    /// </summary>
    IReadOnlyDictionary<CoreKeyCode, string> GetAllMappedKeys();
}
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Interfaces;

namespace KeyboardMouseOdometer.Core.Utils;

/// <summary>
/// Core implementation of key code mapping that doesn't depend on WPF
/// </summary>
public class CoreKeyCodeMapper : IKeyCodeMapper
{
    private static readonly Dictionary<CoreKeyCode, string> KeyNameMap = new()
    {
        // Letters
        { CoreKeyCode.A, "A" }, { CoreKeyCode.B, "B" }, { CoreKeyCode.C, "C" }, { CoreKeyCode.D, "D" },
        { CoreKeyCode.E, "E" }, { CoreKeyCode.F, "F" }, { CoreKeyCode.G, "G" }, { CoreKeyCode.H, "H" },
        { CoreKeyCode.I, "I" }, { CoreKeyCode.J, "J" }, { CoreKeyCode.K, "K" }, { CoreKeyCode.L, "L" },
        { CoreKeyCode.M, "M" }, { CoreKeyCode.N, "N" }, { CoreKeyCode.O, "O" }, { CoreKeyCode.P, "P" },
        { CoreKeyCode.Q, "Q" }, { CoreKeyCode.R, "R" }, { CoreKeyCode.S, "S" }, { CoreKeyCode.T, "T" },
        { CoreKeyCode.U, "U" }, { CoreKeyCode.V, "V" }, { CoreKeyCode.W, "W" }, { CoreKeyCode.X, "X" },
        { CoreKeyCode.Y, "Y" }, { CoreKeyCode.Z, "Z" },
        
        // Numbers
        { CoreKeyCode.D0, "0" }, { CoreKeyCode.D1, "1" }, { CoreKeyCode.D2, "2" }, { CoreKeyCode.D3, "3" },
        { CoreKeyCode.D4, "4" }, { CoreKeyCode.D5, "5" }, { CoreKeyCode.D6, "6" }, { CoreKeyCode.D7, "7" },
        { CoreKeyCode.D8, "8" }, { CoreKeyCode.D9, "9" },
        
        // Numpad
        { CoreKeyCode.NumPad0, "Num0" }, { CoreKeyCode.NumPad1, "Num1" }, { CoreKeyCode.NumPad2, "Num2" },
        { CoreKeyCode.NumPad3, "Num3" }, { CoreKeyCode.NumPad4, "Num4" }, { CoreKeyCode.NumPad5, "Num5" },
        { CoreKeyCode.NumPad6, "Num6" }, { CoreKeyCode.NumPad7, "Num7" }, { CoreKeyCode.NumPad8, "Num8" },
        { CoreKeyCode.NumPad9, "Num9" },
        { CoreKeyCode.Multiply, "Num*" }, { CoreKeyCode.Add, "Num+" }, { CoreKeyCode.Subtract, "Num-" },
        { CoreKeyCode.Decimal, "Num." }, { CoreKeyCode.Divide, "Num/" },
        
        // Function Keys
        { CoreKeyCode.F1, "F1" }, { CoreKeyCode.F2, "F2" }, { CoreKeyCode.F3, "F3" }, { CoreKeyCode.F4, "F4" },
        { CoreKeyCode.F5, "F5" }, { CoreKeyCode.F6, "F6" }, { CoreKeyCode.F7, "F7" }, { CoreKeyCode.F8, "F8" },
        { CoreKeyCode.F9, "F9" }, { CoreKeyCode.F10, "F10" }, { CoreKeyCode.F11, "F11" }, { CoreKeyCode.F12, "F12" },
        
        // Modifiers
        { CoreKeyCode.LeftShift, "LShift" }, { CoreKeyCode.RightShift, "RShift" },
        { CoreKeyCode.LeftCtrl, "LCtrl" }, { CoreKeyCode.RightCtrl, "RCtrl" },
        { CoreKeyCode.LeftAlt, "LAlt" }, { CoreKeyCode.RightAlt, "RAlt" },
        { CoreKeyCode.LWin, "LWin" }, { CoreKeyCode.RWin, "RWin" },
        { CoreKeyCode.CapsLock, "CapsLock" },
        
        // Special Keys
        { CoreKeyCode.Space, "Space" },
        { CoreKeyCode.Enter, "Enter" },
        { CoreKeyCode.Tab, "Tab" },
        { CoreKeyCode.Back, "Backspace" },
        { CoreKeyCode.Delete, "Delete" },
        { CoreKeyCode.Insert, "Insert" },
        { CoreKeyCode.Home, "Home" },
        { CoreKeyCode.End, "End" },
        { CoreKeyCode.PageUp, "PageUp" },
        { CoreKeyCode.PageDown, "PageDown" },
        { CoreKeyCode.Escape, "Esc" },
        { CoreKeyCode.PrintScreen, "PrtScr" },
        { CoreKeyCode.Scroll, "ScrollLock" },
        { CoreKeyCode.Pause, "Pause" },
        { CoreKeyCode.NumLock, "NumLock" },
        
        // Arrow Keys
        { CoreKeyCode.Up, "Up" },
        { CoreKeyCode.Down, "Down" },
        { CoreKeyCode.Left, "Left" },
        { CoreKeyCode.Right, "Right" },
        
        // Punctuation and Symbols
        { CoreKeyCode.OemTilde, "~" },
        { CoreKeyCode.OemMinus, "-" },
        { CoreKeyCode.OemPlus, "=" },
        { CoreKeyCode.OemOpenBrackets, "[" },
        { CoreKeyCode.OemCloseBrackets, "]" },
        { CoreKeyCode.OemPipe, "\\" },
        { CoreKeyCode.OemSemicolon, ";" },
        { CoreKeyCode.OemQuotes, "'" },
        { CoreKeyCode.OemComma, "," },
        { CoreKeyCode.OemPeriod, "." },
        { CoreKeyCode.OemQuestion, "/" },
        
        // Additional Keys
        { CoreKeyCode.Apps, "Menu" },
        { CoreKeyCode.Sleep, "Sleep" },
        { CoreKeyCode.VolumeUp, "VolUp" },
        { CoreKeyCode.VolumeDown, "VolDown" },
        { CoreKeyCode.VolumeMute, "Mute" },
        { CoreKeyCode.MediaNextTrack, "Next" },
        { CoreKeyCode.MediaPreviousTrack, "Prev" },
        { CoreKeyCode.MediaStop, "Stop" },
        { CoreKeyCode.MediaPlayPause, "Play" },
    };

    /// <summary>
    /// Convert a CoreKeyCode to a human-readable string
    /// </summary>
    public string GetKeyName(CoreKeyCode keyCode)
    {
        return KeyNameMap.TryGetValue(keyCode, out var name) ? name : keyCode.ToString();
    }

    /// <summary>
    /// Convert a key code string to a human-readable string
    /// </summary>
    public string GetKeyName(string? keyCode)
    {
        if (string.IsNullOrEmpty(keyCode))
            return "Unknown";

        // Handle specific key strings from GlobalHookService
        var mappedKey = keyCode switch
        {
            "LShiftKey" => CoreKeyCode.LeftShift,
            "RShiftKey" => CoreKeyCode.RightShift,
            "LControlKey" => CoreKeyCode.LeftCtrl,
            "RControlKey" => CoreKeyCode.RightCtrl,
            "LMenu" => CoreKeyCode.LeftAlt,
            "RMenu" => CoreKeyCode.RightAlt,
            "LWin" => CoreKeyCode.LWin,
            "RWin" => CoreKeyCode.RWin,
            "NumPad0" => CoreKeyCode.NumPad0,
            "NumPad1" => CoreKeyCode.NumPad1,
            "NumPad2" => CoreKeyCode.NumPad2,
            "NumPad3" => CoreKeyCode.NumPad3,
            "NumPad4" => CoreKeyCode.NumPad4,
            "NumPad5" => CoreKeyCode.NumPad5,
            "NumPad6" => CoreKeyCode.NumPad6,
            "NumPad7" => CoreKeyCode.NumPad7,
            "NumPad8" => CoreKeyCode.NumPad8,
            "NumPad9" => CoreKeyCode.NumPad9,
            "Multiply" => CoreKeyCode.Multiply,
            "Add" => CoreKeyCode.Add,
            "Subtract" => CoreKeyCode.Subtract,
            "Decimal" => CoreKeyCode.Decimal,
            "Divide" => CoreKeyCode.Divide,
            "NumLock" => CoreKeyCode.NumLock,
            "Capital" => CoreKeyCode.CapsLock,
            "Scroll" => CoreKeyCode.Scroll,
            
            // Windows Forms Keys enum names
            "Next" => CoreKeyCode.PageDown,
            "Prior" => CoreKeyCode.PageUp,
            "Return" => CoreKeyCode.Enter,
            "Back" => CoreKeyCode.Back,
            
            // Additional OEM mappings
            "Oemtilde" => CoreKeyCode.OemTilde,
            "Oem3" => CoreKeyCode.OemTilde,
            "Oemplus" => CoreKeyCode.OemPlus,
            "Oem6" => CoreKeyCode.OemCloseBrackets,
            "Oem5" => CoreKeyCode.OemPipe,
            "Oem4" => CoreKeyCode.OemOpenBrackets,
            "Oem1" => CoreKeyCode.OemSemicolon,
            "Oem7" => CoreKeyCode.OemQuotes,
            "Oemcomma" => CoreKeyCode.OemComma,
            "Oem2" => CoreKeyCode.OemQuestion,
            
            _ => CoreKeyCode.Unknown
        };
        
        if (mappedKey != CoreKeyCode.Unknown)
        {
            return GetKeyName(mappedKey);
        }

        // Try standard enum parsing
        if (Enum.TryParse<CoreKeyCode>(keyCode, true, out var key))
        {
            return GetKeyName(key);
        }

        return keyCode;
    }

    /// <summary>
    /// Convert a virtual key code (int) to a human-readable string
    /// </summary>
    public string GetKeyName(int virtualKeyCode)
    {
        try
        {
            if (Enum.IsDefined(typeof(CoreKeyCode), virtualKeyCode))
            {
                var key = (CoreKeyCode)virtualKeyCode;
                return GetKeyName(key);
            }
        }
        catch
        {
            // Fall through to return VK code
        }

        return $"VK_{virtualKeyCode}";
    }

    /// <summary>
    /// Get all mapped keys for keyboard layout
    /// </summary>
    public IReadOnlyDictionary<CoreKeyCode, string> GetAllMappedKeys()
    {
        return KeyNameMap;
    }
}
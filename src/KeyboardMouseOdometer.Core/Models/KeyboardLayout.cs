namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Defines the US QWERTY keyboard layout for heatmap visualization
/// </summary>
public static class KeyboardLayout
{
    /// <summary>
    /// Standard key width in grid units
    /// </summary>
    private const double STD = 1.0;
    
    /// <summary>
    /// Get the complete US QWERTY keyboard layout
    /// </summary>
    public static List<KeyboardKey> GetUSQwertyLayout()
    {
        var keys = new List<KeyboardKey>();
        
        // Row 0: Function keys row
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Escape, DisplayText = "Esc", X = 0, Y = 0, Width = 1.25, Category = "function" });
        // Gap for spacing
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F1, DisplayText = "F1", X = 2, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F2, DisplayText = "F2", X = 3, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F3, DisplayText = "F3", X = 4, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F4, DisplayText = "F4", X = 5, Y = 0, Category = "function" });
        // Gap
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F5, DisplayText = "F5", X = 6.5, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F6, DisplayText = "F6", X = 7.5, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F7, DisplayText = "F7", X = 8.5, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F8, DisplayText = "F8", X = 9.5, Y = 0, Category = "function" });
        // Gap
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F9, DisplayText = "F9", X = 11, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F10, DisplayText = "F10", X = 12, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F11, DisplayText = "F11", X = 13, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F12, DisplayText = "F12", X = 14, Y = 0, Category = "function" });
        // Print screen cluster
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.PrintScreen, DisplayText = "PrtSc", X = 15.25, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Scroll, DisplayText = "ScrLk", X = 16.25, Y = 0, Category = "function" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Pause, DisplayText = "Pause", X = 17.25, Y = 0, Category = "function" });
        
        // Row 1: Number row
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemTilde, DisplayText = "~", AlternateText = "`", X = 0, Y = 1.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D1, DisplayText = "1", AlternateText = "!", X = 1, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D2, DisplayText = "2", AlternateText = "@", X = 2, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D3, DisplayText = "3", AlternateText = "#", X = 3, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D4, DisplayText = "4", AlternateText = "$", X = 4, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D5, DisplayText = "5", AlternateText = "%", X = 5, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D6, DisplayText = "6", AlternateText = "^", X = 6, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D7, DisplayText = "7", AlternateText = "&", X = 7, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D8, DisplayText = "8", AlternateText = "*", X = 8, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D9, DisplayText = "9", AlternateText = "(", X = 9, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D0, DisplayText = "0", AlternateText = ")", X = 10, Y = 1.25, Category = "number" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemMinus, DisplayText = "-", AlternateText = "_", X = 11, Y = 1.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemPlus, DisplayText = "=", AlternateText = "+", X = 12, Y = 1.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Back, DisplayText = "Backspace", X = 13, Y = 1.25, Width = 2, Category = "modifier" });
        // Navigation cluster
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Insert, DisplayText = "Ins", X = 15.25, Y = 1.25, Category = "navigation" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Home, DisplayText = "Home", X = 16.25, Y = 1.25, Category = "navigation" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.PageUp, DisplayText = "PgUp", X = 17.25, Y = 1.25, Category = "navigation" });
        
        // Row 2: QWERTY row
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Tab, DisplayText = "Tab", X = 0, Y = 2.25, Width = 1.5, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Q, DisplayText = "Q", X = 1.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.W, DisplayText = "W", X = 2.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.E, DisplayText = "E", X = 3.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.R, DisplayText = "R", X = 4.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.T, DisplayText = "T", X = 5.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Y, DisplayText = "Y", X = 6.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.U, DisplayText = "U", X = 7.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.I, DisplayText = "I", X = 8.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.O, DisplayText = "O", X = 9.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.P, DisplayText = "P", X = 10.5, Y = 2.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemOpenBrackets, DisplayText = "[", AlternateText = "{", X = 11.5, Y = 2.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemCloseBrackets, DisplayText = "]", AlternateText = "}", X = 12.5, Y = 2.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemPipe, DisplayText = "\\", AlternateText = "|", X = 13.5, Y = 2.25, Width = 1.5 });
        // Navigation cluster
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Delete, DisplayText = "Del", X = 15.25, Y = 2.25, Category = "navigation" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.End, DisplayText = "End", X = 16.25, Y = 2.25, Category = "navigation" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.PageDown, DisplayText = "PgDn", X = 17.25, Y = 2.25, Category = "navigation" });
        
        // Row 3: ASDF row
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.CapsLock, DisplayText = "Caps Lock", X = 0, Y = 3.25, Width = 1.75, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.A, DisplayText = "A", X = 1.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.S, DisplayText = "S", X = 2.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.D, DisplayText = "D", X = 3.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.F, DisplayText = "F", X = 4.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.G, DisplayText = "G", X = 5.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.H, DisplayText = "H", X = 6.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.J, DisplayText = "J", X = 7.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.K, DisplayText = "K", X = 8.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.L, DisplayText = "L", X = 9.75, Y = 3.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemSemicolon, DisplayText = ";", AlternateText = ":", X = 10.75, Y = 3.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemQuotes, DisplayText = "'", AlternateText = "\"", X = 11.75, Y = 3.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Enter, DisplayText = "Enter", X = 12.75, Y = 3.25, Width = 2.25, Category = "modifier" });
        
        // Row 4: ZXCV row
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.LeftShift, DisplayText = "Shift", X = 0, Y = 4.25, Width = 2.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Z, DisplayText = "Z", X = 2.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.X, DisplayText = "X", X = 3.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.C, DisplayText = "C", X = 4.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.V, DisplayText = "V", X = 5.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.B, DisplayText = "B", X = 6.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.N, DisplayText = "N", X = 7.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.M, DisplayText = "M", X = 8.25, Y = 4.25, Category = "letter" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemComma, DisplayText = ",", AlternateText = "<", X = 9.25, Y = 4.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemPeriod, DisplayText = ".", AlternateText = ">", X = 10.25, Y = 4.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.OemQuestion, DisplayText = "/", AlternateText = "?", X = 11.25, Y = 4.25 });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.RightShift, DisplayText = "Shift", X = 12.25, Y = 4.25, Width = 2.75, Category = "modifier" });
        // Arrow up
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Up, DisplayText = "↑", X = 16.25, Y = 4.25, Category = "navigation" });
        
        // Row 5: Bottom row
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.LeftCtrl, DisplayText = "Ctrl", X = 0, Y = 5.25, Width = 1.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.LWin, DisplayText = "Win", X = 1.25, Y = 5.25, Width = 1.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.LeftAlt, DisplayText = "Alt", X = 2.5, Y = 5.25, Width = 1.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Space, DisplayText = "Space", X = 3.75, Y = 5.25, Width = 6.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.RightAlt, DisplayText = "Alt", X = 10, Y = 5.25, Width = 1.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.RWin, DisplayText = "Win", X = 11.25, Y = 5.25, Width = 1.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Apps, DisplayText = "Menu", X = 12.5, Y = 5.25, Width = 1.25, Category = "modifier" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.RightCtrl, DisplayText = "Ctrl", X = 13.75, Y = 5.25, Width = 1.25, Category = "modifier" });
        // Arrow keys
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Left, DisplayText = "←", X = 15.25, Y = 5.25, Category = "navigation" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Down, DisplayText = "↓", X = 16.25, Y = 5.25, Category = "navigation" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Right, DisplayText = "→", X = 17.25, Y = 5.25, Category = "navigation" });
        
        // Numpad
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumLock, DisplayText = "Num", X = 18.5, Y = 1.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Divide, DisplayText = "/", X = 19.5, Y = 1.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Multiply, DisplayText = "*", X = 20.5, Y = 1.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Subtract, DisplayText = "-", X = 21.5, Y = 1.25, Category = "numpad" });
        
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad7, DisplayText = "7", X = 18.5, Y = 2.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad8, DisplayText = "8", X = 19.5, Y = 2.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad9, DisplayText = "9", X = 20.5, Y = 2.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Add, DisplayText = "+", X = 21.5, Y = 2.25, Height = 2, Category = "numpad" });
        
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad4, DisplayText = "4", X = 18.5, Y = 3.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad5, DisplayText = "5", X = 19.5, Y = 3.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad6, DisplayText = "6", X = 20.5, Y = 3.25, Category = "numpad" });
        
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad1, DisplayText = "1", X = 18.5, Y = 4.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad2, DisplayText = "2", X = 19.5, Y = 4.25, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad3, DisplayText = "3", X = 20.5, Y = 4.25, Category = "numpad" });
        // Note: Numpad Enter commented out to avoid duplicate key issue with main Enter key
        // TODO: Add NumPadEnter to CoreKeyCode enum to differentiate from main Enter
        // keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Enter, DisplayText = "Enter", X = 21.5, Y = 4.25, Height = 2, Category = "numpad" });
        
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.NumPad0, DisplayText = "0", X = 18.5, Y = 5.25, Width = 2, Category = "numpad" });
        keys.Add(new KeyboardKey { KeyCode = CoreKeyCode.Decimal, DisplayText = ".", X = 20.5, Y = 5.25, Category = "numpad" });
        
        return keys;
    }
    
    /// <summary>
    /// Populate KeyName properties for database matching
    /// </summary>
    public static void PopulateKeyNames(List<KeyboardKey> layout, Interfaces.IKeyCodeMapper keyCodeMapper)
    {
        foreach (var key in layout)
        {
            key.KeyName = keyCodeMapper.GetKeyName(key.KeyCode);
        }
    }
    
    /// <summary>
    /// Create a dictionary mapping key codes to keyboard keys for quick lookup
    /// </summary>
    public static Dictionary<string, KeyboardKey> CreateKeyLookup(List<KeyboardKey> layout, Interfaces.IKeyCodeMapper keyCodeMapper)
    {
        var lookup = new Dictionary<string, KeyboardKey>();
        
        foreach (var key in layout)
        {
            var keyName = keyCodeMapper.GetKeyName(key.KeyCode);
            if (!lookup.ContainsKey(keyName))
            {
                lookup[keyName] = key;
            }
        }
        
        return lookup;
    }
}
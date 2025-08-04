namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Core-compatible key codes that don't depend on WPF
/// Maps to standard Windows virtual key code values for cross-platform compatibility
/// </summary>
public enum CoreKeyCode
{
    // Letters (65-90)
    A = 65, B = 66, C = 67, D = 68, E = 69, F = 70, G = 71, H = 72,
    I = 73, J = 74, K = 75, L = 76, M = 77, N = 78, O = 79, P = 80,
    Q = 81, R = 82, S = 83, T = 84, U = 85, V = 86, W = 87, X = 88,
    Y = 89, Z = 90,
    
    // Numbers (48-57)
    D0 = 48, D1 = 49, D2 = 50, D3 = 51, D4 = 52,
    D5 = 53, D6 = 54, D7 = 55, D8 = 56, D9 = 57,
    
    // Numpad (96-111)
    NumPad0 = 96, NumPad1 = 97, NumPad2 = 98, NumPad3 = 99, NumPad4 = 100,
    NumPad5 = 101, NumPad6 = 102, NumPad7 = 103, NumPad8 = 104, NumPad9 = 105,
    Multiply = 106, Add = 107, Subtract = 109, Decimal = 110, Divide = 111,
    
    // Function Keys (112-123)
    F1 = 112, F2 = 113, F3 = 114, F4 = 115, F5 = 116, F6 = 117,
    F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123,
    
    // Modifiers
    LeftShift = 160, RightShift = 161,
    LeftCtrl = 162, RightCtrl = 163,
    LeftAlt = 164, RightAlt = 165,
    LWin = 91, RWin = 92,
    CapsLock = 20,
    
    // Special Keys
    Space = 32,
    Enter = 13,
    Tab = 9,
    Back = 8,        // Backspace
    Delete = 46,
    Insert = 45,
    Home = 36,
    End = 35,
    PageUp = 33,
    PageDown = 34,
    Escape = 27,
    PrintScreen = 44,
    Scroll = 145,    // ScrollLock
    Pause = 19,
    NumLock = 144,
    
    // Arrow Keys
    Up = 38,
    Down = 40,
    Left = 37,
    Right = 39,
    
    // Punctuation and Symbols (OEM Keys)
    OemTilde = 192,       // ~ `
    OemMinus = 189,       // - _
    OemPlus = 187,        // = +
    OemOpenBrackets = 219, // [ {
    OemCloseBrackets = 221, // ] }
    OemPipe = 220,        // \ |
    OemSemicolon = 186,   // ; :
    OemQuotes = 222,      // ' "
    OemComma = 188,       // , <
    OemPeriod = 190,      // . >
    OemQuestion = 191,    // / ?
    
    // Additional Keys
    Apps = 93,            // Menu key
    Sleep = 95,
    VolumeUp = 175,
    VolumeDown = 174,
    VolumeMute = 173,
    MediaNextTrack = 176,
    MediaPreviousTrack = 177,
    MediaStop = 178,
    MediaPlayPause = 179,
    
    // Unknown/Undefined
    Unknown = 0
}
using System.Windows.Input;
using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.UI.Services;

/// <summary>
/// WPF-compatible implementation of key code mapping that can convert between WPF Key enum and CoreKeyCode
/// </summary>
public class WpfKeyCodeMapper : IKeyCodeMapper
{
    private readonly Core.Utils.CoreKeyCodeMapper _coreMapper = new();
    
    private static readonly Dictionary<Key, CoreKeyCode> WpfToCoreKeyMap = new()
    {
        // Letters
        { Key.A, CoreKeyCode.A }, { Key.B, CoreKeyCode.B }, { Key.C, CoreKeyCode.C }, { Key.D, CoreKeyCode.D },
        { Key.E, CoreKeyCode.E }, { Key.F, CoreKeyCode.F }, { Key.G, CoreKeyCode.G }, { Key.H, CoreKeyCode.H },
        { Key.I, CoreKeyCode.I }, { Key.J, CoreKeyCode.J }, { Key.K, CoreKeyCode.K }, { Key.L, CoreKeyCode.L },
        { Key.M, CoreKeyCode.M }, { Key.N, CoreKeyCode.N }, { Key.O, CoreKeyCode.O }, { Key.P, CoreKeyCode.P },
        { Key.Q, CoreKeyCode.Q }, { Key.R, CoreKeyCode.R }, { Key.S, CoreKeyCode.S }, { Key.T, CoreKeyCode.T },
        { Key.U, CoreKeyCode.U }, { Key.V, CoreKeyCode.V }, { Key.W, CoreKeyCode.W }, { Key.X, CoreKeyCode.X },
        { Key.Y, CoreKeyCode.Y }, { Key.Z, CoreKeyCode.Z },
        
        // Numbers
        { Key.D0, CoreKeyCode.D0 }, { Key.D1, CoreKeyCode.D1 }, { Key.D2, CoreKeyCode.D2 }, { Key.D3, CoreKeyCode.D3 },
        { Key.D4, CoreKeyCode.D4 }, { Key.D5, CoreKeyCode.D5 }, { Key.D6, CoreKeyCode.D6 }, { Key.D7, CoreKeyCode.D7 },
        { Key.D8, CoreKeyCode.D8 }, { Key.D9, CoreKeyCode.D9 },
        
        // Numpad
        { Key.NumPad0, CoreKeyCode.NumPad0 }, { Key.NumPad1, CoreKeyCode.NumPad1 }, { Key.NumPad2, CoreKeyCode.NumPad2 },
        { Key.NumPad3, CoreKeyCode.NumPad3 }, { Key.NumPad4, CoreKeyCode.NumPad4 }, { Key.NumPad5, CoreKeyCode.NumPad5 },
        { Key.NumPad6, CoreKeyCode.NumPad6 }, { Key.NumPad7, CoreKeyCode.NumPad7 }, { Key.NumPad8, CoreKeyCode.NumPad8 },
        { Key.NumPad9, CoreKeyCode.NumPad9 },
        { Key.Multiply, CoreKeyCode.Multiply }, { Key.Add, CoreKeyCode.Add }, { Key.Subtract, CoreKeyCode.Subtract },
        { Key.Decimal, CoreKeyCode.Decimal }, { Key.Divide, CoreKeyCode.Divide },
        
        // Function Keys
        { Key.F1, CoreKeyCode.F1 }, { Key.F2, CoreKeyCode.F2 }, { Key.F3, CoreKeyCode.F3 }, { Key.F4, CoreKeyCode.F4 },
        { Key.F5, CoreKeyCode.F5 }, { Key.F6, CoreKeyCode.F6 }, { Key.F7, CoreKeyCode.F7 }, { Key.F8, CoreKeyCode.F8 },
        { Key.F9, CoreKeyCode.F9 }, { Key.F10, CoreKeyCode.F10 }, { Key.F11, CoreKeyCode.F11 }, { Key.F12, CoreKeyCode.F12 },
        
        // Modifiers
        { Key.LeftShift, CoreKeyCode.LeftShift }, { Key.RightShift, CoreKeyCode.RightShift },
        { Key.LeftCtrl, CoreKeyCode.LeftCtrl }, { Key.RightCtrl, CoreKeyCode.RightCtrl },
        { Key.LeftAlt, CoreKeyCode.LeftAlt }, { Key.RightAlt, CoreKeyCode.RightAlt },
        { Key.LWin, CoreKeyCode.LWin }, { Key.RWin, CoreKeyCode.RWin },
        { Key.CapsLock, CoreKeyCode.CapsLock },
        
        // Special Keys
        { Key.Space, CoreKeyCode.Space },
        { Key.Enter, CoreKeyCode.Enter },
        { Key.Tab, CoreKeyCode.Tab },
        { Key.Back, CoreKeyCode.Back },
        { Key.Delete, CoreKeyCode.Delete },
        { Key.Insert, CoreKeyCode.Insert },
        { Key.Home, CoreKeyCode.Home },
        { Key.End, CoreKeyCode.End },
        { Key.PageUp, CoreKeyCode.PageUp },
        { Key.PageDown, CoreKeyCode.PageDown },
        { Key.Escape, CoreKeyCode.Escape },
        { Key.PrintScreen, CoreKeyCode.PrintScreen },
        { Key.Scroll, CoreKeyCode.Scroll },
        { Key.Pause, CoreKeyCode.Pause },
        { Key.NumLock, CoreKeyCode.NumLock },
        
        // Arrow Keys
        { Key.Up, CoreKeyCode.Up },
        { Key.Down, CoreKeyCode.Down },
        { Key.Left, CoreKeyCode.Left },
        { Key.Right, CoreKeyCode.Right },
        
        // Punctuation and Symbols
        { Key.OemTilde, CoreKeyCode.OemTilde },
        { Key.OemMinus, CoreKeyCode.OemMinus },
        { Key.OemPlus, CoreKeyCode.OemPlus },
        { Key.OemOpenBrackets, CoreKeyCode.OemOpenBrackets },
        { Key.OemCloseBrackets, CoreKeyCode.OemCloseBrackets },
        { Key.OemPipe, CoreKeyCode.OemPipe },
        { Key.OemSemicolon, CoreKeyCode.OemSemicolon },
        { Key.OemQuotes, CoreKeyCode.OemQuotes },
        { Key.OemComma, CoreKeyCode.OemComma },
        { Key.OemPeriod, CoreKeyCode.OemPeriod },
        { Key.OemQuestion, CoreKeyCode.OemQuestion },
        
        // Additional Keys
        { Key.Apps, CoreKeyCode.Apps },
        { Key.Sleep, CoreKeyCode.Sleep },
        { Key.VolumeUp, CoreKeyCode.VolumeUp },
        { Key.VolumeDown, CoreKeyCode.VolumeDown },
        { Key.VolumeMute, CoreKeyCode.VolumeMute },
        { Key.MediaNextTrack, CoreKeyCode.MediaNextTrack },
        { Key.MediaPreviousTrack, CoreKeyCode.MediaPreviousTrack },
        { Key.MediaStop, CoreKeyCode.MediaStop },
        { Key.MediaPlayPause, CoreKeyCode.MediaPlayPause },
    };

    /// <summary>
    /// Convert a WPF Key enum to CoreKeyCode
    /// </summary>
    public CoreKeyCode ConvertFromWpfKey(Key wpfKey)
    {
        return WpfToCoreKeyMap.TryGetValue(wpfKey, out var coreKey) ? coreKey : CoreKeyCode.Unknown;
    }

    /// <summary>
    /// Convert a WPF Key enum to a human-readable string
    /// </summary>
    public string GetKeyName(Key wpfKey)
    {
        var coreKey = ConvertFromWpfKey(wpfKey);
        return _coreMapper.GetKeyName(coreKey);
    }

    /// <summary>
    /// Convert a CoreKeyCode to a human-readable string
    /// </summary>
    public string GetKeyName(CoreKeyCode keyCode)
    {
        return _coreMapper.GetKeyName(keyCode);
    }

    /// <summary>
    /// Convert a key code string to a human-readable string
    /// </summary>
    public string GetKeyName(string? keyCode)
    {
        return _coreMapper.GetKeyName(keyCode);
    }

    /// <summary>
    /// Convert a virtual key code (int) to a human-readable string
    /// </summary>
    public string GetKeyName(int virtualKeyCode)
    {
        return _coreMapper.GetKeyName(virtualKeyCode);
    }

    /// <summary>
    /// Get all mapped keys for keyboard layout
    /// </summary>
    public IReadOnlyDictionary<CoreKeyCode, string> GetAllMappedKeys()
    {
        return _coreMapper.GetAllMappedKeys();
    }
}
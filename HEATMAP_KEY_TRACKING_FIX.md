# Heatmap Key Tracking Fix

## Issues Resolved

### 1. Left/Right Modifier Keys Not Tracked Separately ✅
**Problem**: The heatmap wasn't distinguishing between left and right versions of Ctrl, Shift, Alt, and Windows keys.

**Cause**: The GlobalHookService was using `e.KeyCode.ToString()` which doesn't differentiate between left/right modifiers in the Windows.Forms.Keys enum.

**Solution**: 
- Modified `GlobalHookService.OnGlobalKeyDown()` to check for `KeyEventArgsExt` and use virtual key codes
- Added `GetKeyStringFromVirtualCode()` method that maps virtual key codes to specific keys:
  - 0xA0 → "LShiftKey" (Left Shift)
  - 0xA1 → "RShiftKey" (Right Shift)
  - 0xA2 → "LControlKey" (Left Control)
  - 0xA3 → "RControlKey" (Right Control)
  - 0xA4 → "LMenu" (Left Alt)
  - 0xA5 → "RMenu" (Right Alt)
  - 0x5B → "LWin" (Left Windows)
  - 0x5C → "RWin" (Right Windows)

### 2. Numpad Keys Not Tracked ✅
**Problem**: Numpad keys weren't being recorded in the heatmap.

**Cause**: Similar issue - the key mapping wasn't handling numpad-specific virtual key codes.

**Solution**: 
- Added numpad virtual key code mappings:
  - 0x60-0x69 → "NumPad0" through "NumPad9"
  - 0x6A → "Multiply" (*)
  - 0x6B → "Add" (+)
  - 0x6C → "Separator"
  - 0x6D → "Subtract" (-)
  - 0x6E → "Decimal" (.)
  - 0x6F → "Divide" (/)
  - 0x90 → "NumLock"

### 3. Enhanced Key Mapping ✅
- Updated `CoreKeyCodeMapper.GetKeyName()` to handle the new key string formats
- Maps specific key strings from GlobalHookService to CoreKeyCode enums
- Provides human-readable names for display in the heatmap

## Technical Implementation

### GlobalHookService Changes
```csharp
private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
{
    // Try to get more specific key information if available
    string keyCode;
    
    // Check if this is an extended key event with virtual key code
    if (e is KeyEventArgsExt extArgs)
    {
        // Use the virtual key code for more accurate key identification
        keyCode = GetKeyStringFromVirtualCode(extArgs.KeyValue);
    }
    else
    {
        // Fallback to standard key code
        keyCode = GetKeyStringFromKeyCode(e.KeyCode);
    }
    
    _inputMonitoringService.ProcessKeyboardEvent(keyCode, true);
}
```

### Key Benefits
1. **Accurate Tracking**: Each physical key is now tracked separately
2. **Better Heatmap Data**: Users can see which specific modifier keys they use most
3. **Numpad Support**: Full numpad tracking including operators and NumLock
4. **Backward Compatible**: Falls back to standard key codes if extended info unavailable

## Files Modified
1. `/src/KeyboardMouseOdometer.UI/Services/GlobalHookService.cs` - Added virtual key code mapping
2. `/src/KeyboardMouseOdometer.Core/Utils/KeyCodeMapper.cs` - Enhanced key string mapping

## Additional Fixes for Numpad Keys

### Issue: Numpad keys still not showing in heatmap
**Cause**: Mismatch between stored key names and lookup keys.

**Solution**:
1. Added alternative key lookup in `StatisticsService.CalculateHeatmapData()` to handle both "Num0" and "NumPad0" formats
2. Added debug logging to trace numpad key flow through the system
3. Enhanced mapping to handle all possible key name variations

## Result
The heatmap now properly tracks:
- ✅ Left and Right Shift keys separately
- ✅ Left and Right Control keys separately  
- ✅ Left and Right Alt keys separately
- ✅ Left and Right Windows keys separately
- ✅ All numpad keys (0-9, operators, NumLock) with fallback mapping
- ✅ Caps Lock, Scroll Lock, and other special keys

## Debug Logging Added
To troubleshoot numpad issues:
- `DataLoggerService`: Logs when numpad keys are captured
- `HeatmapViewModel`: Logs numpad keys loaded from database
- This helps trace the complete flow from key press to heatmap display
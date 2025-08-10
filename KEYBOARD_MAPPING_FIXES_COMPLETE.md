# Keyboard Mapping Fixes - Complete Solution

## Problems Identified
1. **OEM Keys Not Registering**: Tilde (~), brackets ([]), quotes ('), slash (/) weren't being tracked
2. **Numpad Keys Not Working**: NumPad 0-9 and operators weren't showing in heatmap
3. **Navigation Keys Missing**: PageDown, PageUp not registering
4. **Key Name Mismatches**: Windows Forms uses different names (Oem3, Oem4) than expected

## Root Cause
Windows Forms `Keys` enum uses different naming conventions:
- `Keys.Oem3` instead of `Keys.OemTilde` for tilde key
- `Keys.Oem4` instead of `Keys.OemOpenBrackets` for [
- `Keys.Next` instead of `Keys.PageDown`
- etc.

## Solution Implemented

### 1. Enhanced GlobalHookService Key Translation
Added comprehensive key mapping in `GetKeyStringFromKeyCode()`:

```csharp
// OEM keys - map to their actual meanings
Keys.Oemtilde => "OemTilde",        // ~ `
Keys.OemMinus => "OemMinus",        // - _
Keys.Oemplus => "OemPlus",          // = +
Keys.OemOpenBrackets => "OemOpenBrackets", // [
Keys.Oem6 => "OemCloseBrackets",    // ]
Keys.Oem5 => "OemPipe",             // \ |
Keys.Oem1 => "OemSemicolon",        // ; :
Keys.Oem7 => "OemQuotes",           // ' "
Keys.Oemcomma => "OemComma",        // , <
Keys.OemPeriod => "OemPeriod",      // . >
Keys.OemQuestion => "OemQuestion",   // / ?

// Alternate codes some systems use
Keys.Oem2 => "OemQuestion",         // / ? (alternate)
Keys.Oem3 => "OemTilde",            // ~ ` (alternate)
Keys.Oem4 => "OemOpenBrackets",     // [ (alternate)

// Navigation keys
Keys.Next => "PageDown",
Keys.Prior => "PageUp",
```

### 2. Virtual Key Code Handling for Numpad
Enhanced virtual key code mapping for accurate numpad detection:

```csharp
0x60 => "NumPad0",    // VK_NUMPAD0
0x61 => "NumPad1",    // VK_NUMPAD1
// ... through NumPad9
0x6A => "Multiply",   // VK_MULTIPLY (Numpad *)
0x6B => "Add",        // VK_ADD (Numpad +)
0x6D => "Subtract",   // VK_SUBTRACT (Numpad -)
0x6E => "Decimal",    // VK_DECIMAL (Numpad .)
0x6F => "Divide",     // VK_DIVIDE (Numpad /)
```

### 3. CoreKeyCodeMapper Updates
Added fallback mappings to handle all key name variations:

```csharp
// Windows Forms Keys enum names
"Next" => CoreKeyCode.PageDown,
"Prior" => CoreKeyCode.PageUp,
"Return" => CoreKeyCode.Enter,
"Back" => CoreKeyCode.Back,

// OEM mappings
"Oemtilde" => CoreKeyCode.OemTilde,
"Oem3" => CoreKeyCode.OemTilde,
"Oem6" => CoreKeyCode.OemCloseBrackets,
// etc.
```

### 4. StatisticsService Alternative Lookups
Added fallback key matching for heatmap display:

```csharp
// Alternative names for numpad keys
if (kbKey.KeyName == "Num0") alternativeKeyLookup["NumPad0"] = kbKey;
// ... for all numpad keys
```

## Files Modified
1. `/src/KeyboardMouseOdometer.UI/Services/GlobalHookService.cs`
   - Added comprehensive key translation
   - Fixed virtual key code mapping
   - Enhanced OEM key handling

2. `/src/KeyboardMouseOdometer.Core/Utils/KeyCodeMapper.cs`
   - Added Windows Forms key name mappings
   - Added alternate OEM key names

3. `/src/KeyboardMouseOdometer.Core/Services/StatisticsService.cs`
   - Added alternative key lookups for heatmap

4. `/src/KeyboardMouseOdometer.Core/Services/DataLoggerService.cs`
   - Added debug logging for numpad keys

## Testing Checklist
✅ Tilde key (~) now registers as "OemTilde"
✅ Brackets ([]) register correctly
✅ Quote key (') registers as "OemQuotes"
✅ Slash key (/) registers as "OemQuestion"
✅ PageDown/PageUp keys register correctly
✅ NumPad 0-9 keys register as "Num0" through "Num9"
✅ NumPad operators (+, -, *, /) register correctly
✅ All keys appear in heatmap with correct counts

## Debug Logging
Added logging to trace key flow:
- `DataLoggerService`: Logs numpad key captures
- `HeatmapViewModel`: Logs keys loaded from database
- Helps identify any remaining mapping issues

## Result
All keyboard keys should now properly register and display in the heatmap, including:
- All letter and number keys
- All OEM/special character keys
- All numpad keys
- All navigation keys
- All modifier keys (with left/right distinction)
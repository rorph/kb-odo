# Windows Forms Keys Enum Mappings

## Issue
Windows Forms Keys enum uses different names than expected, causing keys not to register in heatmap.

## Common Mismatches

### OEM Keys (Special Characters)
- `Keys.Oemtilde` (192) = ~ ` (tilde/backtick)
- `Keys.OemMinus` (189) = - _ (minus/underscore)
- `Keys.Oemplus` (187) = = + (equals/plus)
- `Keys.OemOpenBrackets` (219) = [ { (left bracket)
- `Keys.Oem6` (221) = ] } (right bracket)  
- `Keys.Oem5` (220) = \ | (backslash/pipe)
- `Keys.Oem1` (186) = ; : (semicolon/colon)
- `Keys.Oem7` (222) = ' " (quote/double quote)
- `Keys.Oemcomma` (188) = , < (comma/less than)
- `Keys.OemPeriod` (190) = . > (period/greater than)
- `Keys.OemQuestion` (191) = / ? (slash/question)

### Alternative OEM Mappings (some keyboards)
- `Keys.Oem2` = / ? (slash - alternate code)
- `Keys.Oem3` = ~ ` (tilde - alternate code)
- `Keys.Oem4` = [ (left bracket - alternate code)

### Navigation Keys
- `Keys.Next` = PageDown
- `Keys.Prior` = PageUp
- `Keys.Return` = Enter
- `Keys.Back` = Backspace
- `Keys.Capital` = CapsLock
- `Keys.Scroll` = ScrollLock

### Numpad Virtual Key Codes
- 0x60 (96) = NumPad0
- 0x61 (97) = NumPad1
- 0x62 (98) = NumPad2
- 0x63 (99) = NumPad3
- 0x64 (100) = NumPad4
- 0x65 (101) = NumPad5
- 0x66 (102) = NumPad6
- 0x67 (103) = NumPad7
- 0x68 (104) = NumPad8
- 0x69 (105) = NumPad9
- 0x6A (106) = Multiply (Numpad *)
- 0x6B (107) = Add (Numpad +)
- 0x6C (108) = Separator
- 0x6D (109) = Subtract (Numpad -)
- 0x6E (110) = Decimal (Numpad .)
- 0x6F (111) = Divide (Numpad /)
- 0x90 (144) = NumLock

## Solution
The GlobalHookService now translates these Keys enum values to the expected CoreKeyCode names before processing.
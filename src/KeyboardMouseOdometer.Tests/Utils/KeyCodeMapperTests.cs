using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Utils;
using Xunit;

namespace KeyboardMouseOdometer.Tests.Utils;

public class KeyCodeMapperTests
{
    private readonly IKeyCodeMapper _mapper;

    public KeyCodeMapperTests()
    {
        _mapper = new CoreKeyCodeMapper();
    }

    [Theory]
    [InlineData(CoreKeyCode.A, "A")]
    [InlineData(CoreKeyCode.B, "B")]
    [InlineData(CoreKeyCode.Z, "Z")]
    [InlineData(CoreKeyCode.D0, "0")]
    [InlineData(CoreKeyCode.D1, "1")]
    [InlineData(CoreKeyCode.D9, "9")]
    public void GetKeyName_ForAlphanumericKeys_ReturnsCorrectName(CoreKeyCode keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(CoreKeyCode.Space, "Space")]
    [InlineData(CoreKeyCode.Enter, "Enter")]
    [InlineData(CoreKeyCode.Tab, "Tab")]
    [InlineData(CoreKeyCode.Back, "Backspace")]
    [InlineData(CoreKeyCode.Escape, "Esc")]
    public void GetKeyName_ForSpecialKeys_ReturnsCorrectName(CoreKeyCode keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(CoreKeyCode.LeftShift, "LShift")]
    [InlineData(CoreKeyCode.RightShift, "RShift")]
    [InlineData(CoreKeyCode.LeftCtrl, "LCtrl")]
    [InlineData(CoreKeyCode.RightCtrl, "RCtrl")]
    [InlineData(CoreKeyCode.LeftAlt, "LAlt")]
    [InlineData(CoreKeyCode.RightAlt, "RAlt")]
    public void GetKeyName_ForModifierKeys_ReturnsCorrectName(CoreKeyCode keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(CoreKeyCode.F1, "F1")]
    [InlineData(CoreKeyCode.F5, "F5")]
    [InlineData(CoreKeyCode.F12, "F12")]
    public void GetKeyName_ForFunctionKeys_ReturnsCorrectName(CoreKeyCode keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(CoreKeyCode.NumPad0, "Num0")]
    [InlineData(CoreKeyCode.NumPad5, "Num5")]
    [InlineData(CoreKeyCode.NumPad9, "Num9")]
    [InlineData(CoreKeyCode.Multiply, "Num*")]
    [InlineData(CoreKeyCode.Add, "Num+")]
    [InlineData(CoreKeyCode.Subtract, "Num-")]
    [InlineData(CoreKeyCode.Divide, "Num/")]
    public void GetKeyName_ForNumpadKeys_ReturnsCorrectName(CoreKeyCode keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(CoreKeyCode.Up, "Up")]
    [InlineData(CoreKeyCode.Down, "Down")]
    [InlineData(CoreKeyCode.Left, "Left")]
    [InlineData(CoreKeyCode.Right, "Right")]
    public void GetKeyName_ForArrowKeys_ReturnsCorrectName(CoreKeyCode keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData("Space", "Space")]
    [InlineData("Enter", "Enter")]
    [InlineData("F1", "F1")]
    [InlineData("", "Unknown")]
    [InlineData("InvalidKey", "InvalidKey")]
    public void GetKeyName_FromString_ReturnsExpectedResult(string keyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetKeyName_FromNullString_ReturnsUnknown()
    {
        // Act
        var result = _mapper.GetKeyName((string?)null);

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Theory]
    [InlineData(65, "A")]  // VK_A
    [InlineData(32, "Space")]  // VK_SPACE
    [InlineData(13, "Enter")]  // VK_RETURN
    [InlineData(112, "F1")]  // VK_F1
    [InlineData(9999, "VK_9999")]  // Unknown key
    public void GetKeyName_FromVirtualKeyCode_ReturnsExpectedResult(int virtualKeyCode, string expectedName)
    {
        // Act
        var result = _mapper.GetKeyName(virtualKeyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetAllMappedKeys_ReturnsNonEmptyDictionary()
    {
        // Act
        var result = _mapper.GetAllMappedKeys();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Count > 50); // Should have at least 50 keys mapped
    }

    [Fact]
    public void GetAllMappedKeys_ContainsCommonKeys()
    {
        // Act
        var result = _mapper.GetAllMappedKeys();

        // Assert
        Assert.Contains(CoreKeyCode.A, result.Keys);
        Assert.Contains(CoreKeyCode.Space, result.Keys);
        Assert.Contains(CoreKeyCode.Enter, result.Keys);
        Assert.Contains(CoreKeyCode.Escape, result.Keys);
        Assert.Contains(CoreKeyCode.F1, result.Keys);
    }

    [Theory]
    [InlineData(CoreKeyCode.OemTilde, "~")]
    [InlineData(CoreKeyCode.OemMinus, "-")]
    [InlineData(CoreKeyCode.OemPlus, "=")]
    [InlineData(CoreKeyCode.OemOpenBrackets, "[")]
    [InlineData(CoreKeyCode.OemCloseBrackets, "]")]
    [InlineData(CoreKeyCode.OemPipe, "\\")]
    [InlineData(CoreKeyCode.OemSemicolon, ";")]
    [InlineData(CoreKeyCode.OemQuotes, "'")]
    [InlineData(CoreKeyCode.OemComma, ",")]
    [InlineData(CoreKeyCode.OemPeriod, ".")]
    [InlineData(CoreKeyCode.OemQuestion, "/")]
    public void GetKeyName_ForPunctuationKeys_ReturnsCorrectSymbol(CoreKeyCode keyCode, string expectedSymbol)
    {
        // Act
        var result = _mapper.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedSymbol, result);
    }
}
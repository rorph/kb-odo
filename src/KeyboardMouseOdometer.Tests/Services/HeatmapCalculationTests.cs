using Xunit;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace KeyboardMouseOdometer.Tests.Services
{
    public class HeatmapCalculationTests
    {
        private static void PopulateKeyNamesForTest(List<KeyboardKey> layout)
        {
            // Simple key name mapping for tests
            foreach (var key in layout)
            {
                key.KeyName = key.KeyCode switch
                {
                    CoreKeyCode.A => "A",
                    CoreKeyCode.B => "B", 
                    CoreKeyCode.C => "C",
                    CoreKeyCode.D => "D",
                    _ => key.KeyCode.ToString()
                };
            }
        }
        [Fact]
        public void CalculateHeatmapData_EmptyStats_ReturnsDefaultLayout()
        {
            // Arrange
            var keyStats = new Dictionary<string, long>();
            var layout = Core.Models.KeyboardLayout.GetUSQwertyLayout();
            PopulateKeyNamesForTest(layout);
            
            // Act
            var result = StatisticsService.CalculateHeatmapData(keyStats, layout);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(layout.Count, result.Count);
            Assert.All(result, key => Assert.Equal(0, key.HeatLevel));
            Assert.All(result, key => Assert.Equal(0, key.PressCount));
        }
        
        [Fact]
        public void CalculateHeatmapData_SingleKey_CalculatesCorrectHeat()
        {
            // Arrange
            var keyStats = new Dictionary<string, long>
            {
                { "A", 1000 }
            };
            var layout = Core.Models.KeyboardLayout.GetUSQwertyLayout();
            PopulateKeyNamesForTest(layout);
            
            // Act
            var result = StatisticsService.CalculateHeatmapData(keyStats, layout);
            
            // Assert
            var aKey = result.FirstOrDefault(k => k.KeyName == "A");
            Assert.NotNull(aKey);
            Assert.Equal(1000, aKey.PressCount);
            Assert.Equal(1.0, aKey.HeatLevel); // Maximum heat for the only pressed key
        }
        
        [Fact]
        public void CalculateHeatmapData_MultipleKeys_NormalizesHeatLevels()
        {
            // Arrange
            var keyStats = new Dictionary<string, long>
            {
                { "A", 1000 },
                { "B", 500 },
                { "C", 100 },
                { "D", 1 }
            };
            var layout = Core.Models.KeyboardLayout.GetUSQwertyLayout();
            PopulateKeyNamesForTest(layout);
            
            // Act
            var result = StatisticsService.CalculateHeatmapData(keyStats, layout);
            
            // Assert
            var aKey = result.FirstOrDefault(k => k.KeyName == "A");
            var bKey = result.FirstOrDefault(k => k.KeyName == "B");
            var cKey = result.FirstOrDefault(k => k.KeyName == "C");
            var dKey = result.FirstOrDefault(k => k.KeyName == "D");
            
            Assert.NotNull(aKey);
            Assert.NotNull(bKey);
            Assert.NotNull(cKey);
            Assert.NotNull(dKey);
            
            // Check counts are preserved
            Assert.Equal(1000, aKey.PressCount);
            Assert.Equal(500, bKey.PressCount);
            Assert.Equal(100, cKey.PressCount);
            Assert.Equal(1, dKey.PressCount);
            
            // Check heat levels are normalized (A should have max heat)
            Assert.True(aKey.HeatLevel > bKey.HeatLevel);
            Assert.True(bKey.HeatLevel > cKey.HeatLevel);
            Assert.True(cKey.HeatLevel > dKey.HeatLevel);
            Assert.True(dKey.HeatLevel > 0);
        }
        
        [Fact]
        public void CalculateHeatmapData_LogarithmicNormalization_HandlesLargeRanges()
        {
            // Arrange
            var keyStats = new Dictionary<string, long>
            {
                { "A", 1000000 }, // Very high
                { "B", 1000 },    // Medium
                { "C", 1 }        // Very low
            };
            var layout = Core.Models.KeyboardLayout.GetUSQwertyLayout();
            PopulateKeyNamesForTest(layout);
            
            // Act
            var result = StatisticsService.CalculateHeatmapData(keyStats, layout);
            
            // Assert
            var aKey = result.FirstOrDefault(k => k.KeyName == "A");
            var bKey = result.FirstOrDefault(k => k.KeyName == "B");
            var cKey = result.FirstOrDefault(k => k.KeyName == "C");
            
            // Logarithmic normalization should compress the range
            Assert.NotNull(aKey);
            Assert.NotNull(bKey);
            Assert.NotNull(cKey);
            
            // Heat levels should still maintain order but be compressed
            Assert.True(aKey.HeatLevel <= 1.0);
            Assert.True(bKey.HeatLevel >= 0.0);
            Assert.True(cKey.HeatLevel >= 0.0);
            
            // With logarithmic scaling, the difference should be compressed
            var ratio = aKey.HeatLevel / bKey.HeatLevel;
            Assert.True(ratio < 10, "Logarithmic scaling should compress large differences");
        }
        
        [Fact]
        public void CalculateHeatColor_ZeroHeat_ReturnsDefaultColor()
        {
            // Act
            var color = HeatmapColor.CalculateHeatColor(0);
            
            // Assert - Zero heat returns blue with semi-transparent alpha
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(255, color.B);
            Assert.Equal(200, color.A);
        }
        
        [Fact]
        public void CalculateHeatColor_MaxHeat_ReturnsRedColor()
        {
            // Act
            var color = HeatmapColor.CalculateHeatColor(1.0);
            
            // Assert - Max heat returns pure red with semi-transparent alpha
            Assert.Equal(255, color.R);
            Assert.Equal(0, color.G); // Pure red
            Assert.Equal(0, color.B);
            Assert.Equal(200, color.A);
        }
        
        [Theory]
        [InlineData(0.2, false, true, true)]   // Low heat - cyan (blue to cyan transition)
        [InlineData(0.4, false, true, false)]  // Medium-low - green
        [InlineData(0.6, true, true, false)]   // Medium - yellow
        [InlineData(0.8, true, true, false)]   // High - orange
        public void CalculateHeatColor_IntermediateValues_ReturnsGradientColors(
            double heatLevel, bool hasRed, bool hasGreen, bool hasBlue)
        {
            // Act
            var color = HeatmapColor.CalculateHeatColor(heatLevel);
            
            // Assert - Verify color components based on gradient algorithm
            if (hasRed) Assert.True(color.R > 100, $"Red should be > 100 at heat {heatLevel}, got {color.R}");
            if (hasGreen) Assert.True(color.G > 50, $"Green should be > 50 at heat {heatLevel}, got {color.G}");
            if (hasBlue) Assert.True(color.B > 50, $"Blue should be > 50 at heat {heatLevel}, got {color.B}");
            Assert.Equal(200, color.A); // All colors have semi-transparent alpha
        }
        
        [Fact]
        public void CalculateTypingSpeed_ValidInput_ReturnsCorrectKPM()
        {
            // Arrange
            long keyCount = 500; // 500 keys
            var duration = System.TimeSpan.FromMinutes(2); // 2 minutes
            
            // Act
            var kpm = StatisticsService.CalculateTypingSpeed(keyCount, duration);
            
            // Assert - Method returns keys per minute, not words per minute
            Assert.Equal(250.0, kpm, 1); // 500 keys / 2 minutes = 250 KPM
        }
        
        [Fact]
        public void CalculateTypingSpeed_ZeroDuration_ReturnsZero()
        {
            // Arrange
            long keyCount = 500;
            var duration = System.TimeSpan.Zero;
            
            // Act
            var wpm = StatisticsService.CalculateTypingSpeed(keyCount, duration);
            
            // Assert
            Assert.Equal(0, wpm);
        }
        
        [Fact]
        public void CalculateTypingSpeed_ZeroKeys_ReturnsZero()
        {
            // Arrange
            long keyCount = 0;
            var duration = System.TimeSpan.FromMinutes(5);
            
            // Act
            var wpm = StatisticsService.CalculateTypingSpeed(keyCount, duration);
            
            // Assert
            Assert.Equal(0, wpm);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.UI.Controls
{
    public partial class KeyboardHeatmapControl : UserControl
    {
        private const double KeySize = 40;
        private const double KeySpacing = 45;

        public static readonly DependencyProperty KeyboardLayoutProperty =
            DependencyProperty.Register(nameof(KeyboardLayout), typeof(List<KeyboardKey>), 
                typeof(KeyboardHeatmapControl), new PropertyMetadata(null, OnKeyboardLayoutChanged));

        public static readonly DependencyProperty ColorSchemeProperty =
            DependencyProperty.Register(nameof(ColorScheme), typeof(string),
                typeof(KeyboardHeatmapControl), new PropertyMetadata("Classic", OnColorSchemeChanged));

        public List<KeyboardKey> KeyboardLayout
        {
            get => (List<KeyboardKey>)GetValue(KeyboardLayoutProperty);
            set => SetValue(KeyboardLayoutProperty, value);
        }

        public string ColorScheme
        {
            get => (string)GetValue(ColorSchemeProperty);
            set => SetValue(ColorSchemeProperty, value);
        }

        public KeyboardHeatmapControl()
        {
            InitializeComponent();
            UpdateHeatmapLegend();
        }

        private static void OnKeyboardLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeyboardHeatmapControl control && e.NewValue is List<KeyboardKey> layout)
            {
                control.RenderKeyboard(layout);
            }
        }

        private static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeyboardHeatmapControl control)
            {
                control.UpdateHeatmapLegend();
                
                if (control.KeyboardLayout != null)
                {
                    control.RenderKeyboard(control.KeyboardLayout);
                }
            }
        }

        private void RenderKeyboard(List<KeyboardKey> layout)
        {
            KeyboardCanvas.Children.Clear();

            foreach (var key in layout)
            {
                var button = CreateKeyButton(key);
                Canvas.SetLeft(button, key.X * KeySpacing);
                Canvas.SetTop(button, key.Y * KeySpacing);
                KeyboardCanvas.Children.Add(button);
            }
        }

        private Button CreateKeyButton(KeyboardKey key)
        {
            var button = new Button
            {
                Width = key.Width * KeySize,
                Height = key.Height * KeySize,
                Content = key.DisplayText,
                DataContext = key,
                Style = FindResource("KeyButtonStyle") as Style
            };

            // Handle special key sizes and styles first
            switch (key.Category)
            {
                case "modifier":
                    button.FontSize = 9;
                    break;
                case "function":
                    button.FontSize = 10;
                    break;
                case "navigation":
                    button.FontSize = 9;
                    break;
                case "numpad":
                    // Don't override background for numpad - let heat color show through
                    // Only set default background if no heat level
                    if (key.HeatLevel <= 0)
                    {
                        button.Background = new SolidColorBrush(Color.FromRgb(35, 35, 40));
                    }
                    break;
            }

            // Apply heat color (do this AFTER category styling so it takes precedence)
            if (key.HeatLevel > 0)
            {
                var colorSchemeEnum = ColorScheme == "FLIR" ? HeatmapColorScheme.FLIR : HeatmapColorScheme.Classic;
                var heatColor = HeatmapColor.CalculateHeatColor(key.HeatLevel, colorSchemeEnum);
                var wpfColor = Color.FromArgb(heatColor.A, heatColor.R, heatColor.G, heatColor.B);
                button.Background = new SolidColorBrush(wpfColor);
            }

            return button;
        }
        
        private void UpdateHeatmapLegend()
        {
            if (HeatLegendGradient == null || LowLabel == null || HighLabel == null)
                return;
                
            var linearGradient = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 0)
            };
            
            if (ColorScheme == "FLIR")
            {
                // FLIR palette: Black → Purple → Blue → Red → Orange → Yellow → White
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0.0));         // Black
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(17, 0, 36), 0.14));      // Deep Purple
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(70, 7, 136), 0.28));     // Blue
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(208, 0, 0), 0.42));      // Red
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(235, 34, 0), 0.56));     // Dark Orange
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 137, 0), 0.70));    // Orange
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 237, 0), 0.85));    // Yellow
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 200), 1.0));   // White
                
                // Update label colors for FLIR
                LowLabel.Foreground = new SolidColorBrush(Color.FromRgb(17, 0, 36));  // Deep Purple
                HighLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 200)); // White
            }
            else // Classic
            {
                // Classic palette: Blue → Cyan → Green → Yellow → Orange → Red
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 128, 255), 0.0));     // Blue
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.2));     // Cyan
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 0.4));       // Green
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 0.6));     // Yellow
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 128, 0), 0.8));     // Orange
                linearGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1.0));       // Red
                
                // Update label colors for Classic
                LowLabel.Foreground = new SolidColorBrush(Color.FromRgb(0, 128, 255));  // Blue
                HighLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));   // Red
            }
            
            HeatLegendGradient.Fill = linearGradient;
        }
    }
}
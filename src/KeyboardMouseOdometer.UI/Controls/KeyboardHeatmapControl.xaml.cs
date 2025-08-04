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

        public List<KeyboardKey> KeyboardLayout
        {
            get => (List<KeyboardKey>)GetValue(KeyboardLayoutProperty);
            set => SetValue(KeyboardLayoutProperty, value);
        }

        public KeyboardHeatmapControl()
        {
            InitializeComponent();
        }

        private static void OnKeyboardLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeyboardHeatmapControl control && e.NewValue is List<KeyboardKey> layout)
            {
                control.RenderKeyboard(layout);
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
                var heatColor = HeatmapColor.CalculateHeatColor(key.HeatLevel);
                var wpfColor = Color.FromArgb(heatColor.A, heatColor.R, heatColor.G, heatColor.B);
                button.Background = new SolidColorBrush(wpfColor);
            }

            return button;
        }
    }
}
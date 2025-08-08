using System;
using System.Globalization;
using System.Windows.Data;

namespace KeyboardMouseOdometer.UI.Converters
{
    /// <summary>
    /// Converts a normalized percentage value (0-1) to a pixel height value
    /// </summary>
    public class PercentageToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                // Get the max height from parameter (default to 50 if not specified)
                double maxHeight = 50;
                if (parameter != null)
                {
                    if (parameter is double paramDouble)
                    {
                        maxHeight = paramDouble;
                    }
                    else if (parameter is string paramString && double.TryParse(paramString, out double parsed))
                    {
                        maxHeight = parsed;
                    }
                }
                
                // Ensure percentage is between 0 and 1
                percentage = Math.Max(0, Math.Min(1, percentage));
                
                // Calculate height with a minimum of 2 pixels for visibility
                double height = percentage * maxHeight;
                if (percentage > 0 && height < 2)
                {
                    height = 2;
                }
                
                return height;
            }
            
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
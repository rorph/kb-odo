using System;
using System.Globalization;
using System.Windows.Data;

namespace KeyboardMouseOdometer.UI.Converters
{
    public class GreaterThanZeroConverter : IValueConverter
    {
        private static GreaterThanZeroConverter? _instance;
        public static GreaterThanZeroConverter Instance => _instance ??= new GreaterThanZeroConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d > 0;
            if (value is int i)
                return i > 0;
            if (value is long l)
                return l > 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;

namespace KeyboardMouseOdometer.UI.Converters
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || value == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || value == null || !(bool)value)
                return Binding.DoNothing;

            return Enum.Parse(targetType, parameter.ToString()!);
        }
    }
}
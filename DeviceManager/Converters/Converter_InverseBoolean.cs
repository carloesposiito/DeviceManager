using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_InverseBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return boolean ? false : true;
            }

            throw new InvalidOperationException(
                "[Converter_InverseBoolean]\n" +
                "Target must be a boolean!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(
                "[Converter_BooleanToString]\n" +
                "Not implemented!\n"
                );
        }
    }
}

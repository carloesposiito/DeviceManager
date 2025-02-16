using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_BooleanToString : IValueConverter
    {
        public string TrueValue { get; set; } = "True";
        public string FalseValue { get; set; } = "False";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return boolean ? TrueValue : FalseValue;
            }

            throw new InvalidOperationException(
                "[Converter_BooleanToString]\n" +
                "Target must be a boolean!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (stringValue.Equals(TrueValue, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (stringValue.Equals(FalseValue, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            throw new InvalidOperationException(
                "[Converter_BooleanToString]\n" +
                "Target must be a string matching TrueValue or FalseValue!\n"
                );
        }
    }
}

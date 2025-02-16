using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_DevicesCountToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                return (int)value <= 1 ? Visibility.Collapsed : Visibility.Visible;
            }

            throw new InvalidOperationException(
                "[Converter_DevicesCountToVisibility]\n" +
                "Target must be an integer!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(
                "[Converter_DevicesCountToVisibility]\n" +
                "Not implemented!\n"
                );
        }
    }
}

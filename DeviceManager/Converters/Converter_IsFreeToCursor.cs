using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace DeviceManager.Converters
{
    internal class Converter_IsFreeToCursor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFree)
            {
                return isFree ? Cursors.Arrow : Cursors.Wait;
            }

            throw new InvalidOperationException(
                "[Converter_IsFreeToCursor]\n" +
                "Target must be a boolean!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(
                "[Converter_IsFreeToCursor] Functionality not implemented!");
        }
    }
}

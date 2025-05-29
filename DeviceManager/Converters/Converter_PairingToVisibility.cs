using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_PairingToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility = Visibility.Collapsed;

            if (value is bool pairing)
            {
                resultVisibility = pairing ? Visibility.Visible : Visibility.Collapsed;
            }

            return resultVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

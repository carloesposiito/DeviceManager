using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class Converter_AuthStatusToVisibility_Reverse : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility = Visibility.Visible;

            if (value is Enums.DeviceAuthStatus deviceAuthStatus)
            {
                resultVisibility = deviceAuthStatus.Equals(Enums.DeviceAuthStatus.AUTHORIZED) ? Visibility.Collapsed : Visibility.Visible;
            }

            return resultVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

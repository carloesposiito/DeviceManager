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
            Visibility resultVisibility = Visibility.Collapsed;
            
            if (value != null && value is Device activeDevice)
            {
                resultVisibility = activeDevice.AuthStatus.Equals(Enums.DeviceAuthStatus.AUTHORIZED) ? Visibility.Collapsed : Visibility.Visible;
            }

            return resultVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

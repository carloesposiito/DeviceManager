using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_AuthStatusToVisibility_Reverse : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility;

            if (value is PlatformTools.Enums.DeviceAuthStatus deviceAuthStatus)
            {
                switch (value)
                {
                    case PlatformTools.Enums.DeviceAuthStatus.AUTHORIZED:
                        resultVisibility = Visibility.Visible;
                        break;

                    case PlatformTools.Enums.DeviceAuthStatus.UNAUTHORIZED:
                        resultVisibility = Visibility.Collapsed;
                        break;

                    default:
                        resultVisibility = Visibility.Collapsed;
                        break;
                }

                return resultVisibility;
            }

            throw new InvalidOperationException(
                "[Converter_AuthStatusToVisibility_Reverse]\n" +
                "Target must be a boolean!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(
                "[Converter_AuthStatusToVisibility_Reverse]\n" +
                "Not implemented!\n"
                );
        }
    }
}

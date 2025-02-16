using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_AuthStatusToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility;

            if (value is PlatformTools.Enums.DeviceAuthStatus deviceAuthStatus)
            {
                switch (value)
                {
                    case PlatformTools.Enums.DeviceAuthStatus.AUTHORIZED:
                        resultVisibility = Visibility.Collapsed;
                        break;

                    case PlatformTools.Enums.DeviceAuthStatus.UNAUTHORIZED:
                        resultVisibility = Visibility.Visible;
                        break;

                    default:
                        resultVisibility = Visibility.Visible;
                        break;
                }

                return resultVisibility;
            }

            throw new InvalidOperationException(
                "[Converter_AuthStatusToVisibility]\n" +
                "Target must be a boolean!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(
                "[Converter_AuthStatusToVisibility]\n" +
                "Not implemented!\n"
                );
        }
    }
}

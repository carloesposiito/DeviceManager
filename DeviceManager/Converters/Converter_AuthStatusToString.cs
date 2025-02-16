using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_AuthStatusToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string resultString = string.Empty;

            if (value is PlatformTools.Enums.DeviceAuthStatus deviceAuthStatus)
            {
                switch (value)
                {
                    case PlatformTools.Enums.DeviceAuthStatus.AUTHORIZED:
                        resultString = "AUTHORIZED";
                        break;

                    case PlatformTools.Enums.DeviceAuthStatus.UNAUTHORIZED:
                        resultString = "UNAUTHORIZED";
                        break;

                    default:
                        resultString = "UNKNOWN";
                        break;
                }

                return resultString;
            }

            throw new InvalidOperationException(
                "[Converter_AuthStatusToString]\n" +
                "Target must be a boolean!\n"
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PlatformTools.Enums.DeviceAuthStatus resultAuthStatus = PlatformTools.Enums.DeviceAuthStatus.UNKNOWN;

            if (value is string stringValue)
            {
                switch (value)
                {
                    case PlatformTools.Enums.DeviceAuthStatus.AUTHORIZED:
                        resultAuthStatus = PlatformTools.Enums.DeviceAuthStatus.AUTHORIZED;
                        break;

                    case PlatformTools.Enums.DeviceAuthStatus.UNAUTHORIZED:
                        resultAuthStatus = PlatformTools.Enums.DeviceAuthStatus.UNAUTHORIZED;
                        break;

                    default:
                        break;
                }

                return resultAuthStatus;
            }

            throw new InvalidOperationException(
                "[Converter_AuthStatusToString]\n" +
                "Target must be a DeviceAuthStatus enum!\n"
                );
        }
    }
}

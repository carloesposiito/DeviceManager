using System;
using System.Globalization;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class Converter_ActiveDeviceToIsEnabled : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device activeDevice)
            {
                return activeDevice != null;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(
                "[Converter_ActiveDeviceToIsEnabled] Functionality not implemented!");
        }
    }
}

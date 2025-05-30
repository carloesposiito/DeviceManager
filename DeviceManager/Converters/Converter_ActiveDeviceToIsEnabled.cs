using System;
using System.Globalization;
using PlatformTools;
using System.Windows;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_ActiveDeviceToIsEnabled : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEnabled = false;

            if (value != null)
            {
                if (value is Device connectedDevices)
                {
                    isEnabled = true;
                }
            }

            return isEnabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

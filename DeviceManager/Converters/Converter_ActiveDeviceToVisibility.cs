using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class Converter_ActiveDeviceToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility = Visibility.Collapsed;

            if (value != null)
            {
                if (value is Device connectedDevices)
                {
                    resultVisibility = Visibility.Visible;
                }
            }

            return resultVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

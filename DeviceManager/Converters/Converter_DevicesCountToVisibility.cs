using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class Converter_DevicesCountToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility = Visibility.Collapsed;

            if (value is ObservableCollection<Device> connectedDevices && connectedDevices.Count >= 2)
            {
                int authTrueCount = connectedDevices.Count(device => device.AuthStatus.Equals(Enums.DeviceAuthStatus.AUTHORIZED));

                if (authTrueCount >= 2)
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

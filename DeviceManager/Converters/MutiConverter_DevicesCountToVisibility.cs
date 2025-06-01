using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class MutiConverter_DevicesCountToVisibility : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility resultVisibility = Visibility.Collapsed;

            if (values != null && values.Length.Equals(2))
            {
                Device activeDevice = values[0] as Device;
                ObservableCollection<Device> connectedDevices = values[1] as ObservableCollection<Device>;

                if (connectedDevices.Count >= 2)
                {
                    int authorizedDevices = connectedDevices.Count(device => device.AuthStatus.Equals(Enums.DeviceAuthStatus.AUTHORIZED));
                    if (authorizedDevices >= 2 && activeDevice.AuthStatus.Equals(Enums.DeviceAuthStatus.AUTHORIZED))
                    {
                        resultVisibility = Visibility.Visible;
                    }
                }
            }

            return resultVisibility;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}

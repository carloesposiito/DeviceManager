using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class MultiConverter_DevicesNotEqualToItemSource : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<Device> operationResult = new ObservableCollection<Device>();

            if (values != null && values.Length.Equals(2))
            {
                Device activeDevice = values[0] as Device;
                ObservableCollection<Device> connectedDevices = values[1] as ObservableCollection<Device>;
                if (connectedDevices != null && connectedDevices.Count > 1)
                {
                    operationResult = new ObservableCollection<Device>(connectedDevices);
                    operationResult.Remove(activeDevice);
                }                
            }

            return operationResult;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

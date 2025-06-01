using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class MultiConverter_DevicesNotEqualToIsEnabled
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool operationResult = false;

            if (values != null && values.Length.Equals(2))
            {
                Device activeDevice = values[0] as Device;
                Device destionationDevice = values[1] as Device;
                operationResult = !activeDevice.Equals(destionationDevice);
            }

            return operationResult;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

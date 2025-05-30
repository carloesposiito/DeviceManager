using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using PlatformTools;

namespace DeviceManager.Converters
{
    internal class Converter_ActiveDeviceToTabAppsName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string resultText = string.Empty;

            if (value != null)
            {
                if (value is Device activeDevice)
                {
                    resultText = $"{activeDevice.Model} - Apps";
                }
            }

            return resultText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

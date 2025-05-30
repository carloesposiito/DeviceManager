using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace DeviceManager.Converters
{
    internal class Converter_DirExistsToIsEnabled : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool resultEnabed = false;

            if (value is string directory)
            {
                resultEnabed = Directory.Exists(directory) ? true : false;
            }

            return resultEnabed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

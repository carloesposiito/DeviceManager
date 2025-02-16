using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace DeviceManager.Converters
{
    internal class MultiConverter_ActiveDevice_AuthStatus_ToBoolean : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibilityResult = Visibility.Collapsed;

            if (values == null || values.Length < 2)
            {
                return Binding.DoNothing;
            }

            PlatformTools.Enums.DeviceAuthStatus deviceAuthStatus = 0;
            int activeDevice = -1;

            #region "Casting"

            if (values[0] is PlatformTools.Enums.DeviceAuthStatus authStatus)
            {
                deviceAuthStatus = authStatus;
            }

            if (values[1] is int active)
            {
                activeDevice = active;
            }

            #endregion

            //if (deviceAuthStatus <= 1)
            //{
            //    visibilityResult = Visibility.Hidden;
            //}
            //else
            //{
            //    if (activeDevice.Equals(deviceAuthStatus - 1))
            //    {
            //        visibilityResult = Visibility.Hidden;
            //    }
            //    else
            //    {
            //        visibilityResult = Visibility.Visible;
            //    }
            //}

            return visibilityResult;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

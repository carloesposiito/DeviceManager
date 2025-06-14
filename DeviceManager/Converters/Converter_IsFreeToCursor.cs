﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace DeviceManager.Converters
{
    internal class Converter_IsFreeToCursor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Cursor cursorResult = Cursors.Arrow;

            if (value is bool isFree)
            {
                cursorResult = isFree ? Cursors.Arrow : Cursors.Wait;
            }

            return cursorResult;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

﻿namespace Microsoft.Journal.Common
{
    using System;
    using Windows.UI.Xaml.Data;

    class TimeSpanConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, String culture)
        {
            if (value is TimeSpan)
            {
                TimeSpan span = (TimeSpan)value;
                return span.ToString();
            }
            return "???";
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, String culture)
        {
            throw new InvalidOperationException();
        }
    }
}

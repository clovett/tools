namespace Microsoft.Journal.Common
{
    using System;
    using Windows.UI.Xaml.Data;

    class HourLabelConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, String culture)
        {
            if (value is int)
            {
                int hour = (int)value;

                if (hour == 0 || hour == 24)
                {
                    return "12 AM";
                }
                else if (hour < 12)
                {
                    return hour + " AM";
                }
                else if (hour == 12)
                {
                    return hour + " PM";
                } 
                else 
                {
                    return (hour - 12) + " PM";
                }
            }
            return "???";
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, String culture)
        {
            throw new InvalidOperationException();
        }
    }
}

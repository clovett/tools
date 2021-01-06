using System;
using System.Windows;
using System.Windows.Data;

namespace MyFitness.Controls
{
    class MonthDayIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                if (s.Contains("great", StringComparison.OrdinalIgnoreCase))
                {
                    return "\ue113";
                }
                else if (s.Contains("good", StringComparison.OrdinalIgnoreCase))
                {
                    return "\ue170";
                }
                else if (s.Contains("bad", StringComparison.OrdinalIgnoreCase))
                {
                    return "\ue19E";
                }
                else if (s.Contains("ok", StringComparison.OrdinalIgnoreCase))
                {
                    return "\ue19D";
                }
                return "\ue11B";
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

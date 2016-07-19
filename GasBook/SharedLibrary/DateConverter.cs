using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SharedLibrary
{
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime)
            {
                DateTime dt = (DateTime)value;
                string format = parameter as string;
                if (format != null)
                {
                    return dt.ToString(format);
                }
                return dt.ToString();
            }
            else if (value is DateTimeOffset)
            {
                DateTimeOffset dt = (DateTimeOffset)value;
                string format = parameter as string;
                if (format != null)
                {
                    return dt.ToString(format);
                }
                return dt.ToString();
            }
            return "DateTime?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

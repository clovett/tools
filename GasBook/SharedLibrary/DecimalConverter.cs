using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SharedLibrary
{
    public class DecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal)
            {
                decimal v = (decimal)value;
                string format = parameter as string;
                if (format != null)
                {
                    return v.ToString(format);
                }
                return v.ToString();
            }
            return "DateTime?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

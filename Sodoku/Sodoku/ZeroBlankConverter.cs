using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Sodoku
{
    class ZeroBlankConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int)
            {
                int x = (int)value;
                if (x == 0)
                {
                    return "";
                }
                return x.ToString();
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                int i;
                if (int.TryParse((string)value, out i))
                {
                    return i;
                }
            }
            else if (value is int)
            {
                return value;
            }
            return 0;
        }
    }
}

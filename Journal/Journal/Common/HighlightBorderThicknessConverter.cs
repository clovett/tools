namespace Microsoft.Journal.Common
{
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;

    class HighlightBorderThicknessConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, String culture)
        {
            if (value is bool)
            {
                bool b = (bool)value;
                if (b)
                {
                    return new Thickness(5,0,0,0);
                }
            }
            return new Thickness(0);
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, String culture)
        {
            throw new InvalidOperationException();
        }
    }
}

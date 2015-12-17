using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DateSpinnerSample
{

    public static class WpfUtilities
    {
        internal static T FindResource<T>(this FrameworkElement e, string name)
        {
            object value = null;
            DependencyObject d = e;
            while (d != null)
            {
                FrameworkElement f = d as FrameworkElement;
                if (f != null)
                {
                    if (f.Resources.TryGetValue(name, out value))
                    {
                        return (T)value;
                    }
                }
                d = VisualTreeHelper.GetParent(d);
            }

            if (App.Current.Resources.TryGetValue(name, out value))
            {
                return (T)value;
            }

            return default(T);
        }
    }
}

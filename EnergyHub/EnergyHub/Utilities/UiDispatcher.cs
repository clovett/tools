using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace EnergyHub.Utilities
{
    static class UiDispatcher
    {
        public static CoreDispatcher Dispatcher;

        public static void BeginInvoke(Action action)
        {
            var nowait = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                action();
            }));
        }

        public static void RunOnUIThread(this FrameworkElement element, Action action)
        {
            var nowait = element.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                action();
            }));
        }
    }
}

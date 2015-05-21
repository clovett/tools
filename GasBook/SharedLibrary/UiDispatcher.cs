using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace SharedLibrary
{
    public static class UiDispatcher
    {
        static CoreDispatcher _dispatcher;

        public static void Initialize(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public static void RunOnUIThread(Action a)
        {
            var nowait = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                a();
            }));
        }
    }
}

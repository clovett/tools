using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaAnimatingBarChart.Utilities
{
    public class UiDispatcher
    {
        static UiDispatcher instance;
        Dispatcher dispatcher;

        public static void Initialize()
        {
            instance = new UiDispatcher()
            {
                dispatcher = Dispatcher.UIThread
            };
        }

        public static void RunOnUIThread(Action a)
        {
            var nowait = instance.dispatcher.InvokeAsync(a);
        }
    }
}

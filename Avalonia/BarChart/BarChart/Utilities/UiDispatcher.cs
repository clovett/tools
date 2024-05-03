using Avalonia.Threading;
using System;

namespace BarChart.Utilities
{
    internal class UiDispatcher
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
            if (instance != null && instance.dispatcher != null) {
                var nowait = instance.dispatcher.InvokeAsync(a);
            }
        }
    }
}

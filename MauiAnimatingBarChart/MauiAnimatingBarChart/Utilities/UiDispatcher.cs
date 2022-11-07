using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;

namespace Walkabout.Utilities
{
    /// <summary>
    /// A simple helper class that gives a way to run things on the UI thread.  The app must call Initialize once during app start, using inside OnLaunch.
    /// </summary>
    public class UiDispatcher
    {
        static UiDispatcher instance;
        IDispatcher dispatcher;

        public static void Initialize()
        {
            instance = new UiDispatcher()
            {
                dispatcher = Dispatcher.GetForCurrentThread()
            };
        }

        public static void RunOnUIThread(Action a)
        {
            var nowait = instance.dispatcher.DispatchAsync(a);
        }
    }
}

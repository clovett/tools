using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace RandomNumbers.SharedControls
{
    /// <summary>
    /// A simple helper class that gives a way to run things on the UI thread.  The app must call Initialize once during app start, using inside OnLaunch.
    /// </summary>
    public class UiDispatcher
    {
        static UiDispatcher instance;
        CoreDispatcher dispatcher;

        public static void Initialize()
        {
            instance = new UiDispatcher()
            {
                dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher
            };
        }

        public static void RunOnUIThread(Action a)
        {
            if (instance == null)
            {
                // we must be in headless background task
                a();
            }
            else
            {
                var nowait = instance.dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    a();
                }));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Foundation;

namespace FoscamExplorer
{
    static class DispatcherExtensions
    {
        // RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler
        public static IAsyncAction RunAsync(this Dispatcher dispatcher, CoreDispatcherPriority priority, DispatchedHandler handler)
        {
            DispatcherAsyncAction action = new DispatcherAsyncAction();
            var operation = dispatcher.BeginInvoke(new Action(() =>
            {
                handler();
                action.OnCompleted();
            }));
            return action;
        }
    }

    enum CoreDispatcherPriority { Normal };

    delegate void DispatchedHandler();

    class DispatcherAsyncAction : IAsyncAction
    {
        AsyncStatus status;

        public DispatcherAsyncAction()
        {
            status = AsyncStatus.Started;
        }

        public AsyncActionCompletedHandler Completed
        {
            get;
            set; 
        }

        public void GetResults()
        {
        }

        public void Cancel()
        {
        }

        public void Close()
        {
        }

        public Exception ErrorCode
        {
            get { return null; }
        }

        public uint Id
        {
            get { return (uint)this.GetHashCode();  }
        }

        public AsyncStatus Status
        {
            get { return this.status; }
        }

        internal void OnCompleted()
        {
            status = AsyncStatus.Completed;
            if (Completed != null)
            {
                Completed(this, AsyncStatus.Completed);
            }
        }
    }
}

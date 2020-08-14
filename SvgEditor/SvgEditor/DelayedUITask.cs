using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SvgEditor
{
    /// <summary>
    /// This is an async task controller that consolidates a bunch of async events and responds with 
    /// one delayed task execution once the Tick() method stops being called.  So it consolidates
    /// all the ticks in one delayed response.
    /// </summary>
    public class DelayedUITask
    {
        private System.Action _task;
        private long _delay;
        private Timer _timer;
        private bool _completed;

        public DelayedUITask(TimeSpan delay, System.Action task)
        {
            this._task = task;
            this._delay = delay.Ticks;
            Restart();
        }

        private void Restart()
        {
            if (_timer != null)
            {
                _completed = true;                
                _timer.Dispose();
            }

            _completed = false;
            _timer = new Timer(new TimerCallback(OnTimeout), this, this._delay, (long)Timeout.Infinite);
        }

        /// <summary>
        /// Call this method for each event that you want to consolidate.
        /// This delays the completion of this queued task.
        /// </summary>
        public void Tick()
        {
            if (!_completed)
            {
                Restart();
            }
        }

        public void OnTimeout(object state)
        {
            if (!_completed)
            {
                _completed = true;
                _task();
            }
        }
    }
}

using BtConnectionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutomaticPairingDemo.Bluetooth
{
    class EnableRadioTask : IBtCommandCallback
    {
        IBtRadioController _radioController;
        AutoResetEvent _taskCompleteEvent = new AutoResetEvent(false);
        Exception _taskError;

        public EnableRadioTask(IBtRadioController _radioController)
        {
            this._radioController = _radioController;
            _taskCompleteEvent = new AutoResetEvent(false);
        }

        public async Task EnableRadioAsync(TimeSpan timeout)
        {
            _taskError = null;
            _taskCompleteEvent.Reset();
            // note: this must happen on UI thread.
            _radioController.EnableBluetoothRadio(1, this, 0);

            await Task.Run(new Action(() =>
            {
                if (!_taskCompleteEvent.WaitOne(timeout))
                {
                    _taskError = new Exception("Enable Bluetooth Radio timed out");
                }
            }));

            if (_taskError != null)
            {
                throw _taskError;
            }
        }        

        public void CommandCompleted(int pvContext, int hrError)
        {
            if (hrError != 0)
            {
                _taskError = new Exception(string.Format("EnableBluetoothRadio failed: {0:x}", (uint)hrError));
            }
            _taskCompleteEvent.Set();
        }


    }
}


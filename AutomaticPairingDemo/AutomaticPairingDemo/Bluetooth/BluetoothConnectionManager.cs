using BtConnectionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutomaticPairingDemo.Bluetooth
{
    public class BluetoothConnectionManager 
    {
        IBtRadioController _radioController;

        /// <summary>
        /// This method asynchronously looks for the given device and pairs with it.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device</param>
        /// <returns>If it succeeds it returns null, otherwise it returns exception information
        /// on what went wrong.</returns>
        public async Task PairAsync(string deviceAddress, TimeSpan timeout)
        {
            await Task.Run(new Action(() =>
            {
                EnableRadioTask enableRadio = new EnableRadioTask(_radioController);
                enableRadio.EnableRadio(timeout);
            }));
        }


    }

}

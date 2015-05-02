using BtConnectionManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace AutomaticPairingDemo.Bluetooth
{
    public class BluetoothConnectionManager : IBtConnectionObserverCallback, IBtConnectionObserverCallback2, IDisposable
    {
        IBtRadioController _radioController;
        private IBtConnectionObserver _connectionObserver;
        private int? _callbackHandle;
        const int SHUTDOWN_TIMEOUT = 10 * 1000;
        private ulong _deviceAddress;
        private int _signalStrength;
        private string _deviceName;
        BluetoothRadioState _radioState;
        BluetoothDeviceState _deviceState;        
        string _pinCode;
        byte[] _advertisement;
        bool _requestPending;
        bool _pinCodeRequested;
        AutoResetEvent _deviceFound;
        AutoResetEvent _requestComplete;
        Exception _pairingError;
        PairingRequest _pairingRequest;
        AutoResetEvent _requestCancelledEvent;
        AutoResetEvent _deviceStateChanged;
        bool _requestCancelled;


        /// <summary>
        /// Constructs a new BluetoothConnectionManager
        /// </summary>
        public BluetoothConnectionManager()
        {
            _radioController = (IBtRadioController)(new BtConnectionManager.BtConnectionManager());
            _connectionObserver = (IBtConnectionObserver)_radioController;

            App.Current.Suspending += OnAppSuspending;
            App.Current.UnhandledException += OnAppUnhandledException;
        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // we must be good about cleaning up our use of the connection manager or we can leave it in a horked state...
            Dispose();
        }

        private void OnAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // we must be good about cleaning up our use of the connection manager or we can leave it in a horked state...
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (_connectionObserver != null)
                {
                    if (_callbackHandle.HasValue)
                    {
                        _connectionObserver.UnregisterCallback(_callbackHandle.Value);
                        _callbackHandle = null;
                    }
                    _connectionObserver.Shutdown();
                }

                if (_radioController != null)
                {
                    _radioController.SynchronousShutdown(SHUTDOWN_TIMEOUT);
                }
            }
            catch
            {
            }
            _connectionObserver = null;
            _radioController = null;
            App.Current.Suspending -= OnAppSuspending;
            App.Current.UnhandledException -= OnAppUnhandledException;
        }

        private void StartListening()
        {
            int handle;
            _connectionObserver.RegisterCallback(this, out handle);
            _callbackHandle = handle;
            _radioController.SetInquiryMode(InquiryMode.General);
        }

        /// <summary>
        /// While we are pairing we may receive some advertisement data from the device, this
        /// property will provide that data.
        /// </summary>
        public byte[] Advertisement { get { return _advertisement; } }

        /// <summary>
        /// During pairing process we may find a name for the device
        /// </summary>
        public string DeviceName { get { return _deviceName; } }

        /// <summary>
        /// This method looks for the given device and pairs with it.  If this 
        /// method succeeds then you are ready to fire up the BleGenericGattService to talk 
        /// to the device.  It raised PairingComplete event when the process is complete.
        /// If the bluetooth radio is disabled, it will enable it, and if the device is
        /// already paired it will do nothing and raise the PairingComplete event anyway.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device</param>
        /// <param name="pinCode">Optional pin code to use during pairing request, if any.</param>
        /// <returns>If it succeeds it returns null, otherwise it returns exception information
        /// on what went wrong.</returns>
        public async Task PairDeviceAsync(ulong deviceAddress, string pinCode, TimeSpan timeout)
        {
            if (deviceAddress == 0)
            {
                throw new ArgumentOutOfRangeException("deviceAddress");
            }
            if (_requestPending)
            {
                throw new InvalidOperationException("Pairing is already under way");
            }

            _pairingRequest = null;
            _pinCodeRequested = false;
            _deviceState = BluetoothDeviceState.NotVisible;
            _deviceAddress = deviceAddress;
            _pinCode = pinCode;
            _signalStrength = -127;
            _deviceName = null;
            _advertisement = null;
            _deviceFound = new AutoResetEvent(false);
            _requestComplete = new AutoResetEvent(false);
            _requestCancelledEvent = new AutoResetEvent(false);
            _deviceStateChanged = new AutoResetEvent(false);
            _requestCancelled = false;
            _pairingError = null;

            EnableRadioTask enableRadio = new EnableRadioTask(_radioController);
            await enableRadio.EnableRadioAsync(TimeSpan.FromSeconds(10)); // should be relatively quick.

            StartListening();

            // wait for some time to see if we are sniffing advertisements from the device.
            await Task.Run(new Action(() =>
            {
                int index = AutoResetEvent.WaitAny(new WaitHandle[] { _deviceFound, _requestCancelledEvent }, timeout);
                if (index == WaitHandle.WaitTimeout)
                {
                    Debug.WriteLine("### Timeout waiting for device to appear");                 
                    return;
                }
            }));

            if (_requestCancelled)
            {
                throw new OperationCanceledException();
            }

            if (_deviceState == BluetoothDeviceState.NotVisible)
            {
                throw new Exception(string.Format("Device {0:x} is not advertising or is not in range", _deviceAddress));
            }

            if (_deviceState == BluetoothDeviceState.Connected || _deviceState == BluetoothDeviceState.Paired)
            {
                return;
            }
            else if (_deviceState == BluetoothDeviceState.Visible)
            {
                CreatePairingRequest();
            }

            // wait for some time let pairing complete.
            await Task.Run(new Action(() =>
            {
                int index = AutoResetEvent.WaitAny(new WaitHandle[] { _requestComplete, _requestCancelledEvent }, timeout);
                if (index == WaitHandle.WaitTimeout)
                {
                    Debug.WriteLine("### Timeout waiting for pairing to complete");
                    return;
                }
            }));

            if (_requestCancelled)
            {
                throw new OperationCanceledException();
            }

            if (_pinCodeRequested)
            {
                _pairingRequest.SetPin(this._pinCode);

                // wait for some time let pairing complete now that we've provided the pin code...
                await Task.Run(new Action(() =>
                {
                    int index = AutoResetEvent.WaitAny(new WaitHandle[] { _requestComplete, _requestCancelledEvent }, timeout);
                    if (index == WaitHandle.WaitTimeout)
                    {
                        Debug.WriteLine("### Timeout waiting for pairing to complete");
                        return;
                    }
                }));

                if (_requestCancelled)
                {
                    throw new OperationCanceledException();
                }
            }

            if (_pairingError != null)
            {
                throw _pairingError;
            }

        }


        public async Task UnpairDeviceAsync(ulong deviceAddress, TimeSpan timeout)
        {
            if (deviceAddress == 0)
            {
                throw new ArgumentOutOfRangeException("deviceAddress");
            }
            if (_requestPending)
            {
                throw new InvalidOperationException("Pairing is already under way");
            }

            if (_requestPending)
            {
                throw new InvalidOperationException("Pairing is already under way");
            }

            _pairingRequest = null;
            _pinCodeRequested = false;
            _deviceState = BluetoothDeviceState.NotVisible;
            _deviceAddress = deviceAddress;
            _signalStrength = -127;
            _deviceName = null;
            _advertisement = null;
            _deviceFound = new AutoResetEvent(false);
            _requestComplete = new AutoResetEvent(false);
            _requestCancelledEvent = new AutoResetEvent(false);
            _deviceStateChanged = new AutoResetEvent(false);
            _requestCancelled = false;
            _pairingError = null;

            StartListening();

            _radioController.UnpairDevice(deviceAddress);            

            // wait for some time let pairing complete now that we've provided the pin code...
            await Task.Run(new Action(() =>
            {
                int index = AutoResetEvent.WaitAny(new WaitHandle[] { _deviceStateChanged, _requestCancelledEvent }, timeout);
                if (index == WaitHandle.WaitTimeout)
                {
                    Debug.WriteLine("### Timeout waiting for pairing to complete");
                    return;
                }
            }));

            if (_deviceState == BluetoothDeviceState.Unpairing)
            {
                // wait for some time let pairing complete now that we've provided the pin code...
                await Task.Run(new Action(() =>
                {
                    int index = AutoResetEvent.WaitAny(new WaitHandle[] { _deviceStateChanged, _requestCancelledEvent }, timeout);
                    if (index == WaitHandle.WaitTimeout)
                    {
                        Debug.WriteLine("### Timeout waiting for pairing to complete");
                        return;
                    }
                }));
            }

            Debug.WriteLine(string.Format("Unpairing device state is {0}", _deviceState));

        }

        public void Cancel()
        {
            _requestCancelledEvent.Set();
            _requestCancelled = true;
        }

        private void CreatePairingRequest()
        {
            if (_requestPending == true)
            {
                return;
            }
            _requestPending = true;

            // must happen on UI thread
            _pairingRequest = new PairingRequest(_radioController, _deviceAddress);
            _pairingRequest.Completed += (s, e) =>
            {
                _requestPending = false;
                OnPairingRequestComplete(e);
            };
            _pairingRequest.PinRequired += (s, e) =>
            {
                _pinCodeRequested = true;
                OnPairingRequestComplete(null);
            };
            _pairingRequest.StartPairing();
        }

        private void OnPairingRequestComplete(Exception e)
        {
            _pairingError = e;
            _requestComplete.Set();
        }

        public void RadioStateChanged(BluetoothRadioState state)
        {
            // this should get called with BluetoothRadioState.Enabled 
            Debug.WriteLine("RadioStateChanged {0}", state);
            _radioState = state;
        }

        public void RemoteDeviceChanged(ulong btAddr, BluetoothDeviceState eState, uint dwClassOfDevice, string wszName)
        {
            if (btAddr == _deviceAddress)
            {
                Debug.WriteLine("RemoteDeviceChanged {0:x} {1}", btAddr, eState);
                _deviceState = eState;
                _deviceName = wszName;
                _deviceStateChanged.Set();
            }
        }

        public void ProfileConnectionChanged(ulong btAddr, ref Guid guidProfile, BluetoothConnectionState eState)
        {

        }

        public void InitializationComplete()
        {
            // this should get called when IBtRadioController is ready.
            Debug.WriteLine("InitializationComplete");
        }

        public void SignalStrengthChanged(ulong btAddr, int iSignalStrength)
        {
            if (_deviceAddress == btAddr)
            {
                Debug.WriteLine("SignalStrengthChanged  {0:x} {1}", btAddr, iSignalStrength);
                if (_deviceState == BluetoothDeviceState.NotVisible)
                {
                    _deviceState = BluetoothDeviceState.Visible;
                }
                _signalStrength = iSignalStrength;
                _deviceFound.Set();
            }
        }

        public void AdvertisingDataChanged(ulong btAddr, uint cbData, byte[] pbData)
        {
            if (_deviceAddress == btAddr)
            {
                if (_deviceState == BluetoothDeviceState.NotVisible)
                {
                    _deviceState = BluetoothDeviceState.Visible;
                }
                Debug.WriteLine("AdvertisingDataChanged {0:x} {1}", btAddr, cbData);
                byte[] data = new byte[cbData];
                Array.Copy(pbData, data, (int)cbData);
                _advertisement = data;
            }
        }


        class PairingRequest : IBtPairingRequestCallback
        {
            IBtPairingRequest _request;
            ulong _address;
            Exception _error;

            internal PairingRequest(IBtRadioController radioController, ulong address)
            {
                _address = address;
                radioController.CreatePairingRequest(address, this, out _request);
            }

            internal void StartPairing()
            {
                _request.StartPairing(AuthenticationRequirements.MITMProtectionRequiredBonding, IntPtr.Zero);
            }

            internal event EventHandler<Exception> Completed;
            internal event EventHandler PinRequired;

            public void GetPin(ulong btAddr)
            {
                if (_address == btAddr)
                {
                    if (PinRequired != null)
                    {
                        PinRequired(this, EventArgs.Empty);
                    }                    
                }
            }

            public void SetPin(string code)
            {
                if (string.IsNullOrEmpty(code))
                {
                    _error = new ArgumentException("A pinCode is required for this pairing request");
                }
                else
                {
                    // we already have it
                    byte[] pinBytes = Encoding.UTF8.GetBytes(code);
                    _request.SetPin(pinBytes.Length, pinBytes);
                }
            }

            public void ShowPasskey(ulong btAddr, int cbPasskey, byte[] passkey)
            {
                // show pass key to the user, but we assume the user is happy with whatever.
            }

            public void OutgoingPairingCompleted(ulong btAddr, int hr)
            {
                if (hr != 0)
                {
                    if ((uint)hr == 0x80070575)
                    {
                        _error = new Exception("Pairing failed because pin code is incorrect");
                    }
                    else
                    {
                        _error = new Exception(string.Format("Pairing failed with error code {0:x}", (uint)hr));
                    }
                }
                if (Completed != null)
                {
                    Completed(this, _error);
                }
            }

            public void CompareNumber(ulong btAddr, int iNumber)
            {
                // assume user is happy with the number;if (_dispatcher != null)
                _request.AcceptPairing();
            }

        }

    }

}

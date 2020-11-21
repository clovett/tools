using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothConsole
{
    public interface ILog
    {
        void WriteLine(string line);
    }

    public class SensorTag
    {
        GattDeviceService service;
        ILog log;

        public SensorTag(ILog log)
        {
            this.log = log;
        }
        public async void FindPairedDevices()
        {
            foreach (var info in await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.DeviceInformation)))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (bleDevice != null)
                {
                    ulong addr = bleDevice.BluetoothAddress;
                    log.WriteLine(string.Format("Paired device {0} mac addr {1}", info.Name, addr.ToString("x")));
                    if (info.Name.Contains("SensorTag"))
                    {
                        Connect(bleDevice);
                    }
                }
            }
        }

        internal static readonly string CONTAINER_ID_PROPERTY_NAME = "System.Devices.ContainerId";

        private async void Connect(BluetoothLEDevice bleDevice)
        {
            try
            {
                var result = await bleDevice.GetGattServicesForUuidAsync(LightIntensityServiceUuid);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    this.service = result.Services.First();
                    this.service.Session.SessionStatusChanged += OnSessionStatusChanged;

                    // start listening
                    var charres = await service.GetCharacteristicsForUuidAsync(LightIntensityCharacteristicUuid);
                    if (charres.Status == GattCommunicationStatus.Success)
                    {
                        var characteristic = charres.Characteristics.FirstOrDefault();
                        characteristic.ValueChanged -= OnCharacteristicValueChanged;
                        var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        log.WriteLine(string.Format("Register for notification returned status {0}", status));
                        characteristic.ValueChanged += OnCharacteristicValueChanged;

                        // turn on light sensing
                        status = await WriteCharacteristic(LightIntensityCharacteristicConfigUuid, new byte[] { 1 });
                        log.WriteLine(string.Format("Start listening to light sensor returned status {0}", status));
                        status = await WriteCharacteristic(LightIntensityCharacteristicPeriodUuid, new byte[] { 10 });
                        log.WriteLine(string.Format("Set light sensor period returned status {0}", status));
                    }
                }
                else
                {
                    throw new Exception("LightIntensityServiceUuid service not found: " + result.Status);
                }
            }
            catch (Exception ex)
            {
                log.WriteLine(string.Format("Exception: {0}", ex.Message));
            }
        }

        private void OnSessionStatusChanged(GattSession sender, GattSessionStatusChangedEventArgs args)
        {
            // Device.ConnectionStatusChanged += OnConnectionStatusChanged;
            log.WriteLine(string.Format("OnSessionStatusChanged: {0}", args.Status));
        }

        private async Task<GattCommunicationStatus> WriteCharacteristic(Guid guid, byte[] bytes)
        {
            var charres = await service.GetCharacteristicsForUuidAsync(LightIntensityCharacteristicUuid);
            if (charres.Status == GattCommunicationStatus.Success)
            {
                GattCharacteristic config = charres.Characteristics.FirstOrDefault();
                DataWriter writer = new DataWriter();
                writer.WriteBytes(bytes);
                var buffer = writer.DetachBuffer();
                var properties = config.CharacteristicProperties;
                if ((properties & GattCharacteristicProperties.Write) != 0)
                {
                    return await config.WriteValueAsync(buffer, GattWriteOption.WriteWithResponse);
                }
                else if ((properties & GattCharacteristicProperties.WriteWithoutResponse) != 0)
                {
                    return await config.WriteValueAsync(buffer, GattWriteOption.WriteWithResponse);
                }
            }
            throw new Exception("Characteristic is not writable");
        }

        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (sender.Uuid == LightIntensityCharacteristicUuid)
            {
                uint dataLength = args.CharacteristicValue.Length;
                using (DataReader reader = DataReader.FromBuffer(args.CharacteristicValue))
                {
                    if (dataLength == 2)
                    {
                        ushort rawData = ReadBigEndianU16bit(reader);

                        uint m = (uint)(rawData & 0x0FFF);
                        uint e = ((uint)(rawData & 0xF000)) >> 12;

                        double lux = (double)m * (0.01 * Math.Pow(2.0, e));
                        log.WriteLine(string.Format("Received light reading: {0}", lux));
                    }
                }
            }
        }

        protected ushort ReadBigEndianU16bit(DataReader reader)
        {
            byte lo = reader.ReadByte();
            byte hi = reader.ReadByte();
            return (ushort)(((ushort)hi << 8) + (ushort)lo);
        }
        static Guid LightIntensityServiceUuid = Guid.Parse("f000aa70-0451-4000-b000-000000000000");
        static Guid LightIntensityCharacteristicUuid = Guid.Parse("f000aa71-0451-4000-b000-000000000000");
        static Guid LightIntensityCharacteristicConfigUuid = Guid.Parse("f000aa72-0451-4000-b000-000000000000");
        static Guid LightIntensityCharacteristicPeriodUuid = Guid.Parse("f000aa73-0451-4000-b000-000000000000");
    }
}

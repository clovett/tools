using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using System.IO;

namespace BluetoothConsole
{
    class Program
    {
        [DllImport("BluetoothSecurityFix.dll")]
        static extern bool BluetoothSecurityFix();

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "-fix")
            {
                try
                {
                    if (!BluetoothSecurityFix())
                    {
                        Console.WriteLine("### Error: Security fix failed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("### Exception: " + ex.Message);
                }
            }
            Program p = new BluetoothConsole.Program();
            p.Run();
        }

        void Run()
        {
            SensorTag st = new BluetoothConsole.SensorTag(new ConsoleLog(Console.Out));
            st.FindPairedDevices();
            Task.Delay(100000).Wait();
        }
        
        class ConsoleLog : ILog
        {
            TextWriter writer;
            public ConsoleLog(TextWriter writer)
            {
                this.writer = writer;
            }

            public void WriteLine(string line)
            {
                writer.WriteLine(line);
            }
        }
    }
}

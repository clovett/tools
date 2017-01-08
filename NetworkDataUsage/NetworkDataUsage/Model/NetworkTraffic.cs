using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace NetworkDataUsage
{
    /// <summary>
    /// Class that gets the network traffic from the performance counter.
    /// </summary>
    public class NetworkTraffic
    {
        class CounterInfo
        {
            public PerformanceCounter Counter;
            public double Sum;
            public string Name;

            internal double GetDelta()
            {
                double value = Counter.NextValue(); 
                Sum += value;
                return value;
            }
        }

        private List<CounterInfo> sentCounters = new List<CounterInfo>();
        private List<CounterInfo> receivedCounters = new List<CounterInfo>();
        const string BytesSentPerSec = "Bytes Sent/sec";
        const string BytesReceivedPerSec = "Bytes Received/sec";

        public NetworkTraffic()
        {
            TryToInitializeCounters();
        }

        private void TryToInitializeCounters()
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");

            foreach (var name in category.GetInstanceNames())
            {
                if (category.CounterExists(BytesSentPerSec))
                {
                    Console.WriteLine("Found counter: " + name);
                    var bytesSentCounter = new PerformanceCounter();
                    bytesSentCounter.CategoryName = category.CategoryName;
                    bytesSentCounter.CounterName = BytesSentPerSec;
                    bytesSentCounter.InstanceName = name;
                    bytesSentCounter.ReadOnly = true;
                    sentCounters.Add(new CounterInfo()
                    {
                        Counter = bytesSentCounter,
                        Name = name
                    });
                }

                if (category.CounterExists(BytesReceivedPerSec))
                {
                    var bytesReceivedCounter = new PerformanceCounter();
                    bytesReceivedCounter.CategoryName = category.CategoryName;
                    bytesReceivedCounter.CounterName = BytesReceivedPerSec;
                    bytesReceivedCounter.InstanceName = name;
                    bytesReceivedCounter.ReadOnly = true;
                    receivedCounters.Add(new CounterInfo()
                    {
                        Counter = bytesReceivedCounter,
                        Name = name
                    });
                }
            }
        }

        public double GetCurrentBytesSent()
        {
            return (from info in sentCounters select info.GetDelta()).Sum();
        }

        public double GetCurrentBytesReceived()
        {
            return (from info in receivedCounters select info.GetDelta()).Sum();
        }
    }
}
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
using Walkabout.Utilities;

namespace NetworkDataUsage
{
    /// <summary>
    /// Class that gets the network traffic from the performance counter.
    /// </summary>
    public class NetworkTraffic
    {
        class CounterInfo
        {
            private PerformanceCounter counter;
            private double sum;
            private string name;
            private double lastValue;

            public CounterInfo(PerformanceCounter counter, string name)
            {
                this.counter = counter;
                this.name = name;
            }

            public double GetSum()
            {
                lock (counter)
                {
                    return this.sum;
                }
            }
            public double GetLastValue()
            {
                lock (counter)
                {
                    return this.lastValue;
                }
            }

            internal double GetDelta()
            {
                lock (counter)
                {
                    lastValue = counter.NextValue();
                    sum += lastValue;
                    return lastValue;
                }
            }
        }

        private List<CounterInfo> sentCounters = new List<CounterInfo>();
        private List<CounterInfo> receivedCounters = new List<CounterInfo>();
        const string BytesSentPerSec = "Bytes Sent/sec";
        const string BytesReceivedPerSec = "Bytes Received/sec";
        DelayedActions updateAction;

        public event EventHandler Updated;

        public NetworkTraffic()
        {
            TryToInitializeCounters();
        }

        public void Start()
        {
            updateAction = new DelayedActions();
            updateAction.StartDelayedAction("Update", OnUpdate, TimeSpan.FromSeconds(1));
        }

        private void OnUpdate()
        {
            if (updateAction != null)
            {
                updateAction.StartDelayedAction("Update", OnUpdate, TimeSpan.FromSeconds(1));
            }
            foreach (var counter in sentCounters)
            {
                counter.GetDelta();
            }
            foreach (var counter in receivedCounters)
            {
                counter.GetDelta();
            }
            OnUpdated();
        }

        private void OnUpdated()
        {
            EventHandler handler = Updated;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Stop()
        {
            var action = updateAction;
            updateAction = null;
            if (action != null)
            {
                action.CancelDelayedAction("Update");
            }
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
                    sentCounters.Add(new CounterInfo(bytesSentCounter, name));
                }

                if (category.CounterExists(BytesReceivedPerSec))
                {
                    var bytesReceivedCounter = new PerformanceCounter();
                    bytesReceivedCounter.CategoryName = category.CategoryName;
                    bytesReceivedCounter.CounterName = BytesReceivedPerSec;
                    bytesReceivedCounter.InstanceName = name;
                    bytesReceivedCounter.ReadOnly = true;
                    receivedCounters.Add(new CounterInfo(bytesReceivedCounter, name));
                }
            }
        }

        public double GetCurrentBytesSent()
        {
            return (from info in sentCounters select info.GetLastValue()).Sum();
        }

        public double GetCurrentBytesReceived()
        {
            return (from info in receivedCounters select info.GetLastValue()).Sum();
        }

        public double GetTotalBytesSent()
        {
            return (from info in sentCounters select info.GetSum()).Sum();
        }

        public double GetTotalBytesReceived()
        {
            return (from info in receivedCounters select info.GetSum()).Sum();
        }
    }
}
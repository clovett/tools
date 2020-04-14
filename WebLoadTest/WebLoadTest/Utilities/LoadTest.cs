using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebLoadTest.Utilities
{
    public class TestResult
    {
        public long Timestamp;
        public List<long> ResponseTimes;
        public long Errors;
    }

    public class LoadTest
    {
        Uri location;
        long startTime;
        TestResult result;
        CancellationTokenSource cancel;

        public LoadTest()
        {
            UpdateFrequency = 1000; // 1 second
        }

        public int UpdateFrequency { get; set; }

        public event EventHandler<TestResult> DataPoint;

        public void Start(Uri uri)
        {
            this.location = uri;
            this.startTime = Environment.TickCount;
            this.cancel = new CancellationTokenSource();
            this.result = new TestResult() { ResponseTimes = new List<long>() };

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                Task.Run(RunTest, this.cancel.Token);
            }
            Task.Run(MonitorTest, this.cancel.Token);
        }

        public void Stop()
        {
            cancel.Cancel();
        }

        private void RunTest()
        {
            var token = this.cancel.Token;
            HttpClient client = new HttpClient();
            Stopwatch watch = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    watch.Reset();
                    watch.Start();
                    Task task = client.GetStringAsync(this.location);
                    Task.WaitAll(new Task[] { task }, token);
                    watch.Stop();
                    lock (result)
                    {
                        result.ResponseTimes.Add(watch.ElapsedTicks);
                    }
                } 
                catch
                {
                    lock (result)
                    {
                        result.Errors++;
                    }
                }
            }
        }

        private async void MonitorTest()
        {
            var token = this.cancel.Token;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(UpdateFrequency);
                TestResult publish = new TestResult();
                lock (result)
                {
                    publish.Timestamp = Environment.TickCount;
                    publish.ResponseTimes = result.ResponseTimes;
                    publish.Errors = result.Errors;
                    // reset for next batch.
                    result.Errors = 0;
                    result.ResponseTimes = new List<long>();
                }

                var handler = this.DataPoint;
                if (handler != null)
                {
                    handler(this, publish);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using EnergyHub;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        EnergyClient client;
        const string TestLogFile = @"d:\temp\log.csv";

        [TestInitialize]
        public void Initialize()
        {
            client = new EnergyClient();
            client.InitializeAlljoynClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            client.Close();
        }


        [TestMethod]
        public void TestReadLog()
        {
            Debug.WriteLine("============= TestReadLog============================");

            string result = client.OpenLog();
            Debug.WriteLine("Open: " + result);

            for (int i = 0; i < 10; i++)
            {
                string line = client.ReadLog();
                Debug.WriteLine("Read: " + line);
            }

            result = client.CloseLog();

            Debug.WriteLine("CloseLog: " + result);
        }


        [TestMethod]
        public void TestTruncateLog()
        {
            Debug.WriteLine("============= TestTruncateLog ============================");

            string result = client.CloseLog();
            Debug.WriteLine("CloseLog: " + result);

            result = client.TruncateLog();
            Debug.WriteLine("TruncateLog: " + result);

            result = client.OpenLog();
            Debug.WriteLine("Open: " + result);

            client.CloseLog();
        }
    }
}

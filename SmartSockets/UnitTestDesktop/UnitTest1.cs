using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using System.Collections.Generic;

namespace UnitTestDesktop
{
    [TestClass]
    public class UnitTest1
    {
        private static List<Process> testProcesses = new List<Process>();
        private TestContext testContextInstance;
        private static List<DialogWrapper> testWindows = new List<DialogWrapper>();

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            Cleanup();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            Debug.WriteLine("TestInitialize");
            Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Debug.WriteLine("TestCleanup");
            Cleanup();
        }

        void Start()
        {
            // each test decides how many instances of each .exe to launch
        }

        private DialogWrapper StartServer()
        {
            string exe = this.GetType().Assembly.Location;
            string testServerDesktopExe = Path.Combine(Path.GetDirectoryName(exe), @"..\..\..\TestServerDesktop\bin\debug\TestServerDesktop.exe");

            // start desktop server
            ProcessStartInfo psi = new ProcessStartInfo(testServerDesktopExe, "");
            Process serverProcess = Process.Start(psi);
            serverProcess.WaitForInputIdle();
            DialogWrapper serverWindow = DialogWrapper.FindMainWindow(serverProcess.Id, "TestServerDesktop");

            testWindows.Add(serverWindow);
            testProcesses.Add(serverProcess);
            return serverWindow;
        }

        private DialogWrapper StartClient()
        {
            string exe = this.GetType().Assembly.Location;
            // start desktop client
            string testClientDesktopExe = Path.Combine(Path.GetDirectoryName(exe), @"..\..\..\TestClientDesktop\bin\debug\TestClientDesktop.exe");
            ProcessStartInfo psi = new ProcessStartInfo(testClientDesktopExe, "");
            Process clientProcess = Process.Start(psi);
            clientProcess.WaitForInputIdle();
            DialogWrapper clientWindow = DialogWrapper.FindMainWindow(clientProcess.Id, "TestClientDesktop");
            testWindows.Add(clientWindow);
            testProcesses.Add(clientProcess);
            return clientWindow;
        }

        static void Cleanup()
        {
            foreach (Process p in testProcesses)
            {
                try
                {
                    p.Kill();
                }
                catch
                {
                }
            }
            testProcesses.Clear();
            testWindows.Clear();
        }

        [TestMethod]
        public void TestClientRestart()
        {
            // ok, ask the server to send message to the client, etc...
            DialogWrapper serverWindow = StartServer();
            RichTextBoxWrapper serverOutput = serverWindow.GetRichTextBox("TestOutput");
            TextBoxWrapper serverSendBox = serverWindow.GetTextBox("SendText");

            // test that we can connect and reconnect to new clients
            for (int i = 0; i < 3; i++)
            {
                DialogWrapper clientWindow = StartClient();

                DialogWrapper.TileWindows(serverWindow, clientWindow);

                serverOutput.WaitForText("ClientConnected:");

                // Test client can send message to server
                TextBoxWrapper clientSendBox = clientWindow.GetTextBox("SendText");
                clientSendBox.SetValue("This is a test");
                clientSendBox.Focus();
                Input.TapKey(System.Windows.Input.Key.Enter);

                RichTextBoxWrapper clientOutput = clientWindow.GetRichTextBox("TestOutput");
                clientOutput.WaitForText("ReplyReceived: Server says: This is a test");

                // Test server can send message to client
                serverSendBox.SetValue("The quick brown fox");
                serverSendBox.Focus();
                Input.TapKey(System.Windows.Input.Key.Enter);

                clientOutput.WaitForText("ReplyReceived: The quick brown fox");

                // make sure server sees client go away.
                clientWindow.Close();

                serverOutput.WaitForText("ClientDisconnected:");

                serverOutput.Clear();
            }

            return;

        }

        [TestMethod]
        public void TestMultipleClients()
        {
            DialogWrapper serverWindow = StartServer();
            RichTextBoxWrapper serverOutput = serverWindow.GetRichTextBox("TestOutput");
            TextBoxWrapper serverSendBox = serverWindow.GetTextBox("SendText");

            List<DialogWrapper> clients = new List<DialogWrapper>();
            // test that we can connect and reconnect to new clients
            for (int i = 0; i < 3; i++)
            {
                clients.Add(StartClient());
            }
            
            DialogWrapper.TileWindows(testWindows.ToArray());

            serverOutput.WaitForText("ClientConnected:");

            // Test each client can send message to server
            int index = 0;
            foreach (var client in clients)
            {
                serverOutput.Clear();
                TextBoxWrapper clientSendBox = client.GetTextBox("SendText");
                string msg = "This is test message " + index;
                clientSendBox.SetValue(msg);
                clientSendBox.Focus();
                Input.TapKey(System.Windows.Input.Key.Enter);

                serverOutput.WaitForText(msg);

                RichTextBoxWrapper clientOutput = client.GetRichTextBox("TestOutput");
                clientOutput.WaitForText("ReplyReceived: Server says: " + msg);
                index++;
            }


            // Test server can send message to all clients
            serverSendBox.SetValue("The rain in spain");
            serverSendBox.Focus();
            Input.TapKey(System.Windows.Input.Key.Enter);
            foreach (var client in clients)
            {
                RichTextBoxWrapper clientOutput = client.GetRichTextBox("TestOutput");
                clientOutput.WaitForText("ReplyReceived: The rain in spain");
            }

            return;

        }

        private void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }
    }
}

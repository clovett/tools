using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Networking
{
    class UdpReceiveEventArgs : EventArgs
    {
        public UdpReceiveEventArgs(string addr)
        {
            RemoteAddress = addr;
        }
        public string RemoteAddress { get; set; }
    }

    public class ConnectionManager
    {
        int m_portNumber;
        int m_serverPort;
        UdpClient client;
        bool stopped;
        Server server;
        string m_udpMessage;
        string m_udpServerMessage;

        public ConnectionManager(Guid application, int udpPort, int tcpPort)
        {
            m_portNumber = udpPort;
            m_serverPort = tcpPort;
            m_udpMessage = "Client:" + application.ToString();
            m_udpServerMessage = "Server:" + application.ToString();
            server = new Server(application, tcpPort);
            client = new UdpClient(m_portNumber);
            client.EnableBroadcast = true;     
        }

        public void StartListening()
        {
            server.Start();
            server.MessageReceived += OnMessageReceived;
            Task.Run(new Action(Receive));
        }

        void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, e);
            }
        }

        public void StopListening()
        {
            server.Stop();
            stopped = true;
            client.Close();
            pending = null;
        }

        public event EventHandler<MessageEventArgs> MessageReceived;

        IAsyncResult pending;

        private void Receive()
        {
            UdpState state = new UdpState() 
            {
                Client = this.client,
                EndPoint = new IPEndPoint(IPAddress.Any, m_portNumber)
            };

            try
            {
                pending = client.BeginReceive(new AsyncCallback(OnReceive), state);
            }
            catch (Exception x)
            {
                Debug.WriteLine("ConnectionManager Receive caught {0}: {1}", x.GetType().Name, x.Message);
            }
        }

        ManualResetEvent received = new ManualResetEvent(false);

        private void OnReceive(IAsyncResult ar)
        {
            if (pending == ar)
            {
                UdpState state = (UdpState)ar.AsyncState;
                UdpClient u = state.Client;
                IPEndPoint remoteEndPoint = state.EndPoint;

                byte[] message = client.EndReceive(ar, ref remoteEndPoint);
                string receiveString = UTF8Encoding.UTF8.GetString(message);

                if (receiveString.StartsWith(m_udpMessage))
                {
                    string remoteAddress = receiveString.Substring(m_udpMessage.Length + 1);

                    // tell this client how to connect!
                    BroadcastEndPoints(remoteAddress, server.ListeningEndPoints);
                }
            }
            if (!stopped)
            {
                Task.Run(new Action(Receive));
            }
        }

        void BroadcastEndPoints(string remoteAddress, IEnumerable<string> endPoints)
        {
            if (client != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(m_udpServerMessage);

                foreach (string ip in endPoints)
                {
                    sb.Append(":");
                    sb.Append(ip);
                }

                byte[] buffer = UTF8Encoding.UTF8.GetBytes(sb.ToString());

                // hey, we got a ping, so respond with our IP addresses so they can connect to us.
                client.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Parse(remoteAddress), m_portNumber));
            }
        }


        class UdpState
        {
            public UdpClient Client;
            public IPEndPoint EndPoint;
        }
    }
}

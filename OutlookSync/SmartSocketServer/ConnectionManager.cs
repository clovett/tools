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
        UdpClient client;
        bool stopped;
        Server server;
        string m_udpMessage;
        string m_udpServerMessage;

        public ConnectionManager(string applicationId, int udpPort)
        {
            m_portNumber = udpPort;
            m_udpMessage = "Client:" + applicationId;
            m_udpServerMessage = "Server:" + applicationId;
            server = new Server(applicationId);
            client = new UdpClient(m_portNumber);
            client.EnableBroadcast = true;     
        }

        public void StartListening()
        {
            server.Start();
            server.MessageReceived += OnMessageReceived;
            server.ReadException += OnServerException;
            Task.Run(new Action(Receive));
        }

        public IEnumerable<string> ServerEndPoints
        {
            get
            {
                return server.ListeningEndPoints;
            }
        }

        void OnServerException(object sender, ServerExceptionEventArgs e)
        {
            // one of the clients went away.
            if (ReadException != null)
            {
                ReadException(this, e);
            }
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
        public event EventHandler<ServerExceptionEventArgs> ReadException;

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
                Log.WriteException("ConnectionManager Receive caught", x);
            }
        }

        ManualResetEvent received = new ManualResetEvent(false);

        private void OnReceive(IAsyncResult ar)
        {
            if (pending == ar)
            {
                try
                {
                    UdpState state = (UdpState)ar.AsyncState;
                    UdpClient u = state.Client;
                    IPEndPoint remoteEndPoint = state.EndPoint;

                    byte[] message = client.EndReceive(ar, ref remoteEndPoint);
                    string receiveString = UTF8Encoding.UTF8.GetString(message);

                    if (receiveString.StartsWith(m_udpMessage))
                    {
                        string parameter = receiveString.Substring(m_udpMessage.Length + 1);

                        if (MessageReceived != null)
                        {
                            MessageReceived(this, new MessageEventArgs(remoteEndPoint, new Message() { Command = "Hello", Parameters = parameter }));
                        }
                    }
                }
                catch { 
                    // happens during shutdown sometimes if client is disposed 
                }
            }
            if (!stopped)
            {
                Task.Run(new Action(Receive));
            }
        }

        public void AllowRemoteMachine(string remoteAddress)
        {
            // tell this client how to connect!
            BroadcastEndPoints(remoteAddress, server.ListeningEndPoints);
        }

        IPEndPoint ParseAddress(string address)
        {
            int i = address.IndexOf(':');
            if (i > 0)
            {
                string ipaddress = address.Substring(0, i);
                string port = address.Substring(i + 1);
                if (int.TryParse(port, out i))
                {
                    return new IPEndPoint(IPAddress.Parse(ipaddress), i);
                }
            }
            return null;
        }

        void BroadcastEndPoints(string remoteAddress, IEnumerable<string> endPoints)
        {
            if (client != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(m_udpServerMessage);

                foreach (string ip in endPoints)
                {
                    sb.Append("/");
                    sb.Append(ip);
                }

                byte[] buffer = UTF8Encoding.UTF8.GetBytes(sb.ToString());

                IPEndPoint remoteEndPoint = ParseAddress(remoteAddress);

                // hey, we got a ping, so respond with our IP addresses so they can connect to us.
                client.Send(buffer, buffer.Length, remoteEndPoint);
            }
        }


        class UdpState
        {
            public UdpClient Client;
            public IPEndPoint EndPoint;
        }
    }
}

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
    public class NoPortsAvailableException : Exception
    {
    }

    public class ConnectionManager
    {
        int m_portNumber;
        UdpClient client;
        bool stopped;
        Server server;
        string m_udpMessage;
        string m_udpServerMessage;

        public ConnectionManager(string applicationId)
        {
            m_udpMessage = "Client:" + applicationId;
            m_udpServerMessage = "Server:" + applicationId;

            server = new Server(applicationId);            
        }

        // cannot be completely random, the phone has to use the same seed.
        const int randomSeed = 1980527718;

        IEnumerable<int> GetRandomPortNumber()
        {
            // this is what the original phone app is expecting in case user still has old version.
            yield return 12777;

            Random r = new Random(randomSeed);

            // according to wikipedia ports 1024 to 49151 are available
            const int min = 1024;
            const int max = 49151;
            int count = 0;

            while (count++ < 20)
            {
                int port = r.Next(min, max);
                if (port != 12777)
                {
                    yield return port;
                }
            }

            // give up!
            throw new NoPortsAvailableException();
        }

        public void StartListening()
        {
            foreach (int port in GetRandomPortNumber())
            {
                try
                {
                    client = new UdpClient(port);
                    Log.WriteLine("Listening on port: " + port);
                    m_portNumber = port;
                    client.EnableBroadcast = true;
                    break;
                }
                catch (SocketException se)
                {
                    client = null;
                    if (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        // try again!
                    }
                    else
                    {
                        // something else is wrong...
                        throw;
                    }
                }
            }

            if (client == null)
            {
                throw new NoPortsAvailableException();
            }

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
            if (client != null)
            {
                client.Close();
            }
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
                pending = client.BeginReceive(new AsyncCallback(OnReceiveUdpBroadcast), state);
            }
            catch (Exception x)
            {
                Log.WriteException("ConnectionManager Receive caught", x);
            }
        }

        ManualResetEvent received = new ManualResetEvent(false);

        private void OnReceiveUdpBroadcast(IAsyncResult ar)
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
            Log.WriteLine("AllowRemoteMachine: " + remoteAddress);
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

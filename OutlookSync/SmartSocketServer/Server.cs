using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.Networking
{
    class Server
    {
        int m_PortNumber = 12778;
        Dictionary<string, TcpListener> listeners = new Dictionary<string, TcpListener>();
        List<ServerClient> activeClients = new List<ServerClient>();
        bool stopped;

        public Server(Guid application, int port)
        {
            m_PortNumber = port;
        }

        public event EventHandler<MessageEventArgs> MessageReceived;

        public void Start()
        {
            stopped = false;

            string machine = Environment.GetEnvironmentVariable("COMPUTERNAME");

            foreach (var address in System.Net.Dns.GetHostAddresses(machine))
            {
                string addr = address.ToString();
                if (address.AddressFamily == AddressFamily.InterNetwork && !addr.StartsWith("169."))
                {
                    TcpListener listener;
                    if (!listeners.TryGetValue(addr, out listener))
                    {
                        IPEndPoint endPoint = new IPEndPoint(address, m_PortNumber);
                        listener = new TcpListener(endPoint);
                        listeners[addr] = listener;
                        Task.Run(new Action(() => RunListener(endPoint, listener)));
                    }
                }
            }
        }

        public IEnumerable<string> ListeningEndPoints
        {
            get { return listeners.Keys; }
        }

        public void Stop()
        {
            stopped = true;
            foreach (TcpListener listener in listeners.Values)
            {
                listener.Stop();
            }
            listeners.Clear();

            foreach (ServerClient sc in activeClients)
            {
                sc.Stop();
                sc.MessageReceived -= OnMessageReceived;
            }

            activeClients.Clear();

        }

        private void RunListener(IPEndPoint endPoint, TcpListener listener)
        {
            try
            {
                listener.Start();
                while (!stopped)
                {
                    var client = listener.AcceptTcpClient();
                    ServerClient sc = new ServerClient(endPoint, client);
                    sc.MessageReceived += OnMessageReceived;
                    activeClients.Add(sc);
                }
            }
            catch (Exception x)
            { 
                Debug.WriteLine("Listener caught {0}: {1}", x.GetType().Name, x.Message);
            }
        }

        void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, e);
            }
        }

    }

    class ServerClient
    {
        TcpClient client;
        IPEndPoint endPoint;
        XmlSerializer serializer = new XmlSerializer(typeof(Message));
        bool stopped;

        public event EventHandler<MessageEventArgs> MessageReceived;

        public ServerClient(IPEndPoint endPoint, TcpClient client)
        {
            this.endPoint = endPoint;
            this.client = client;
            Task.Run(new Action(ProcessCommands));
        }

        public void Stop()
        {
            stopped = true;
            using (client)
            {
                client.Close();
            }
            client = null;
        }

        public void ProcessCommands()
        {
            try
            {
                // receive commands and process them.
                using (NetworkStream stream = client.GetStream())
                {
                    while (!stopped)
                    {
                        int length = stream.ReadInt32();

                        byte[] buffer = stream.ReadExactBytes(length);

                        Message response = HandleMessage(buffer);

                        if (response != null)
                        {
                            MemoryStream ms = new MemoryStream();
                            serializer.Serialize(ms, response);
                            ms.Seek(0, SeekOrigin.Begin);
                            int len = (int)ms.Length;
                            stream.WriteInt32(len);
                            byte[] bytes = ms.ToArray();
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Flush();
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Debug.WriteLine("ServerClient caught {0}: {1}", x.GetType().Name, x.Message);
            }
        }

        private Message HandleMessage(byte[] buffer)
        {
            if (MessageReceived != null)
            {
                MemoryStream ms = new MemoryStream(buffer);
                Message msg = (Message)serializer.Deserialize(ms);
                var args = new MessageEventArgs(endPoint, msg);
                MessageReceived(this, args);
                return args.Response;
            }
            return null;
        }
    }

    static class NetworkStreamExtensions
    {
        public static int ReadInt32(this NetworkStream stream)
        {
            byte[] buffer = stream.ReadExactBytes(4);

            int value = 0;

            // little endian 
            for (int i = 0; i < 4; i++)
            {
                value <<= 8;
                value += buffer[i];
            }
            return value;
        }

        public static byte[] ReadExactBytes(this NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int len = stream.Read(buffer, 0, length);
            while (len < length)
            {
                len += stream.Read(buffer, len, length - len);
            }
            return buffer;
        }

        public static void WriteInt32(this NetworkStream stream, int value)
        {
            // little endian interpretation 
            byte[] buffer = new byte[4];
            for (int i = 3; i >= 0; i--)
            {
                buffer[i] = (byte)(value & 0xff);
                value >>= 8;
            }            
            stream.Write(buffer, 0, 4);
        }
    }
}

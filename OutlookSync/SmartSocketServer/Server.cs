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
        Dictionary<string, TcpListener> listeners = new Dictionary<string, TcpListener>();
        List<ServerClient> activeClients = new List<ServerClient>();
        bool stopped;

        public Server(string applicationId)
        {
        }

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<ServerExceptionEventArgs> ReadException;

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
                        IPEndPoint endPoint = new IPEndPoint(address, 0);
                        listener = new TcpListener(endPoint);
                        listeners[addr] = listener;
                        Task.Run(new Action(() => RunListener(listener)));
                    }
                }
            }
        }

        public IEnumerable<string> ListeningEndPoints
        {
            get
            {
                List<string> endPoints = new List<string>();
                foreach (TcpListener listener in listeners.Values)
                {
                    // get endpoint including tcp port number
                    string s = listener.LocalEndpoint.ToString();
                    endPoints.Add(s);
                }
                return endPoints;
            }
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

        private void RunListener(TcpListener listener)
        {
            try
            {
                listener.Start();
                IPEndPoint ip = listener.LocalEndpoint as IPEndPoint;

                while (!stopped)
                {
                    var client = listener.AcceptTcpClient();
                    ServerClient sc = new ServerClient((IPEndPoint)listener.LocalEndpoint, client);
                    sc.MessageReceived += OnMessageReceived;
                    sc.ReadException += OnReadException;
                    activeClients.Add(sc);
                }
            }
            catch (Exception x)
            {
                Log.WriteException("Listener caught exception", x);
            }
        }

        private void OnReadException(object sender, ServerExceptionEventArgs e)
        {
            ServerClient client = (ServerClient)sender;
            client.Stop();
            activeClients.Remove(client);
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

    }

    public class ServerExceptionEventArgs
    {
        public IPEndPoint RemoteEndPoint { get; set; }

        public Exception Exception { get; set; }
    }

    class ServerClient
    {
        TcpClient client;
        IPEndPoint localEndPoint;
        IPEndPoint remoteEndPoint;
        XmlSerializer serializer = new XmlSerializer(typeof(Message));
        bool stopped;

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<ServerExceptionEventArgs> ReadException;

        public ServerClient(IPEndPoint localEndPoint, TcpClient client)
        {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            this.client = client;
            Task.Run(new Action(ProcessCommands));
        }

        public void Stop()
        {
            stopped = true;
            using (client)
            {
                if (client != null)
                {
                    client.Close();
                }
            }
            client = null;
        }

        public async void ProcessCommands()
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

                        Message response = await HandleMessage(buffer);

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
                Log.WriteException("ServerClient caught exception", x);

                if (ReadException != null)
                {
                    ReadException(this, new ServerExceptionEventArgs() { Exception = x, RemoteEndPoint = remoteEndPoint });
                }
            }
        }

        private Task<Message> HandleMessage(byte[] buffer)
        {
            if (MessageReceived != null)
            {
                MemoryStream ms = new MemoryStream(buffer);
                Message msg = (Message)serializer.Deserialize(ms);
                var args = new MessageEventArgs((IPEndPoint)this.client.Client.RemoteEndPoint, msg);
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

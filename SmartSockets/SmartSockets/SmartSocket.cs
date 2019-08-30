using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Networking.SmartSockets
{
    [DataContract]
    public class Message
    {        
        public Message(string id, string sender) { this.Id = id; this.Sender = sender; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Sender { get; set; }
    }

    public class MessageEventArgs : EventArgs
    {
        Message msg;

        public MessageEventArgs(Message msg)
        {
            this.msg = msg;
        }

        public Message Message { get { return this.msg; } }
    }

    public class SmartSocketTypeResolver : DataContractResolver
    {
        Dictionary<string, Type> typeMap = new Dictionary<string, Type>();

        public SmartSocketTypeResolver()
        {
            AddBaseTypes();
        }

        public SmartSocketTypeResolver(params Type[] knownTypes)
        {
            AddTypes(knownTypes);
        }

        public SmartSocketTypeResolver(IEnumerable<Type> knownTypes)
        {
            AddTypes(knownTypes);
        }

        private void AddTypes(IEnumerable<Type> knownTypes)
        {
            AddBaseTypes();
            foreach (var t in knownTypes)
            {
                if (!t.IsSubclassOf(typeof(Message)))
                {
                    throw new InvalidCastException("The knownTypes have to derrive from Message");
                }
                this.typeMap[t.FullName] = t;
            }
        }

        private void AddBaseTypes()
        {
            foreach(var t in new Type[] {  typeof(Message) })
            {
                this.typeMap[t.FullName] = t;
            }
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            Type t = null;
            string fullName = typeName;
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                Uri uri = new Uri(typeNamespace);
                string clrNamespace = uri.Segments.Last();
                fullName = clrNamespace + "." + typeName;
            }
            if (!typeMap.TryGetValue(fullName, out t))
            {
                t = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
            }
            return t;
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            return knownTypeResolver.TryResolveType(type, declaredType, knownTypeResolver, out typeName, out typeNamespace);
        }
    }

    /// <summary>
    /// This class wraps the Socket class providing some useful semantics like FindServerAsync which looks
    /// for the UDP message broadcast by the SmartSocketServer.  It also provides a useful SendAsync
    /// message that synchronously waits for a response from the server so that it is always clear 
    /// which direction the traffic is flowing.  It also supports serializing custom message objects via
    /// the DataContractSerializer using known types provided in the SmartSocketTypeResolver.
    /// </summary>
    public class SmartSocket : IDisposable
    {
        Socket client;
        NetworkStream socketStream;
        SmartSocketServer server;
        bool disconnected;
        SmartSocketTypeResolver resolver;
        DataContractSerializer serializer;

        public const string DisconnectMessageId = "DisconnectMessageId.3d9cd318-fcae-4a4f-ae63-34907be2700a";
        public const string ConnectedMessageId = "ConnectedMessageId.822280ed-26f5-4cdd-b45c-412e05d1005a";
        public const string ConnectedMessageAck = "ConnectedMessageAck.822280ed-26f5-4cdd-b45c-412e05d1005a";


        internal SmartSocket(SmartSocketServer server, Socket client, SmartSocketTypeResolver resolver)
        {
            this.client = client;
            this.socketStream = new NetworkStream(client);
            this.server = server;
            this.resolver = resolver;
            client.NoDelay = true;

            DataContractSerializerSettings settings = new DataContractSerializerSettings();
            settings.DataContractResolver = this.resolver;
            settings.PreserveObjectReferences = true;
            serializer = new DataContractSerializer(typeof(MessageWrapper), settings);
        }

        internal void Dispose(bool disposing)
        {
            Close();
        }

        internal Socket Socket { get { return this.client; } }

        public string Name { get; set; }

        /// <summary>
        /// Find a SmartSocketListener on the local network using UDP broadcast.
        /// </summary>
        /// <returns>The connected client or null if task is cancelled.</returns>
        public static async Task<SmartSocket> FindServerAsync(string serviceName, string clientName, SmartSocketTypeResolver resolver, CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                string localHost = FindLocalHostName();
                if (localHost == null)
                {
                    return null;
                }
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        IPEndPoint remoteEP = new IPEndPoint(SmartSocketServer.GroupAddress, SmartSocketServer.GroupPort);
                        UdpClient udpClient = new UdpClient(0);
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write(serviceName.Length);
                        writer.Write(serviceName);
                        byte[] bytes = ms.ToArray();
                        udpClient.Send(bytes, bytes.Length, remoteEP);

                        CancellationTokenSource receiveTaskSource = new CancellationTokenSource();
                        Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();
                        if (receiveTask.Wait(5000, receiveTaskSource.Token))
                        {
                            UdpReceiveResult result = receiveTask.Result;
                            IPEndPoint serverEP = result.RemoteEndPoint;
                            byte[] buffer = result.Buffer;
                            BinaryReader reader = new BinaryReader(new MemoryStream(buffer));
                            int len = reader.ReadInt32();
                            string addr = reader.ReadString();
                            string[] parts = addr.Split(':');
                            if (parts.Length == 2) 
                            {
                                var a = IPAddress.Parse(parts[0]);
                                SmartSocket client = await ConnectAsync(new IPEndPoint(a, int.Parse(parts[1])), clientName, resolver);
                                if (client != null)
                                {
                                    client.Name = localHost;
                                    return client;
                                }
                            }
                        }
                        else
                        {
                            receiveTaskSource.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Something went wrong with Udp connection: " + ex.Message);
                    }
                }
                return null;
            });
        }

        private static async Task<SmartSocket> ConnectAsync(IPEndPoint serverEP, string clientName, SmartSocketTypeResolver resolver)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            bool connected = false;
            CancellationTokenSource src = new CancellationTokenSource();
            try
            {
                Task task = Task.Run(() =>
                {
                    try
                    {
                        client.Connect(serverEP);
                        connected = true;
                    } 
                    catch (Exception e)
                    {
                        Debug.WriteLine("Connect exception: " + e.Message);
                    }
                }, src.Token);

                // give it 30 seconds to connect...
                if (!task.Wait(60000))
                {
                    src.Cancel();
                }
            }
            catch (TaskCanceledException)
            {
                // move on...
            }
            if (connected)
            {
                var result = new SmartSocket(null, client, resolver)
                {
                    Name = clientName,
                    ServerName = GetHostName(serverEP.Address)
                };
                Message response = await result.SendAsync(new Message(ConnectedMessageId, clientName));

                return result;
            }
            return null;
        }

        static string GetHostName(IPAddress addr)
        {
            try
            {
                var entry = Dns.GetHostEntry(addr);
                if (!string.IsNullOrEmpty(entry.HostName))
                {
                    return entry.HostName;
                }
            }
            catch
            {
                // this can fail if machines are in different domains.
            }
            return addr.ToString();
        }

        internal static string FindLocalHostName()
        {
            try
            {
                IPHostEntry e = Dns.GetHostEntry(IPAddress.Loopback);
                return e.HostName;
            }
            catch
            {
            }
            return null;
        }

        internal static List<string> FindLocalIpAddresses()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.SupportsMulticast && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var props = ni.GetIPProperties();
                    if (props.IsDnsEnabled || props.IsDynamicDnsEnabled)
                    {
                        IPHostEntry e = Dns.GetHostEntry(IPAddress.Loopback);
                        List<string> ipAddresses = new List<string>();
                        foreach (var addr in e.AddressList)
                        {
                            ipAddresses.Add(addr.ToString());
                        }
                        return ipAddresses;
                    }
                }
            }
            return null;
        }

        public string ServerName { get; set; }

        public bool IsConnected { get { return !disconnected; } }

        /// <summary>
        /// This event is raised if a socket error is detected.
        /// </summary>
        public event EventHandler<Exception> Error;

        /// <summary>
        /// This even is raised if the socket is disconnected.
        /// </summary>
        public event EventHandler Disconnected;

        internal async void Close()
        {
            try
            {
                await SendAsync(new Message(DisconnectMessageId, this.Name));

                using (client)
                {
                    client.Close();
                }

                disconnected = true;
            }
            catch { }
        }

        private void OnError(Exception ex)
        {
            Exception inner = ex;
            while (inner != null)
            {
                SocketException se = inner as SocketException;
                if (se != null && se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // we're toast!
                    if (server != null) server.RemoveClient(this);
                    disconnected = true;
                }
                inner = inner.InnerException;
            }

            if (Error != null)
            {
                Error(this, ex);
            }
        }

        ManualResetEvent sent = new ManualResetEvent(false);
        
        [DataContract]
        internal class MessageWrapper
        { 
            [DataMember]
            public object Message { get; set; }
        }


        /// <summary>
        /// Send a message back to the client.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>The response message</returns>
        public async Task<Message> SendAsync(Message msg)
        {
            // get the buffer containing the serialized message.
            return await Task.Run(async () =>
            {
                // Begin sending the data to the remote device.
                try
                {
                    await SendResponseAsync(msg);

                    Message response = await this.ReceiveAsync();
                    return response;
                }
                catch (Exception ex)
                {
                    // is the socket dead?
                    OnError(ex);
                }
                return null;
            });
        }


        /// <summary>
        /// Send a message back to the client.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>The response message</returns>
        public async Task SendResponseAsync(Message msg)
        {
            // get the buffer containing the serialized message.
            await Task.Run(() =>
            {
                // Begin sending the data to the remote device.
                try
                {
                    MemoryStream ms = new MemoryStream();
                    serializer.WriteObject(ms, new MessageWrapper() { Message = msg });

                    byte[] buffer = ms.ToArray();

                    BinaryWriter streamWriter = new BinaryWriter(socketStream, Encoding.UTF8, true);
                    streamWriter.Write(buffer.Length);
                    streamWriter.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    // is the socket dead?
                    OnError(ex);
                }
            });
        }



        private void OnClosed()
        {
            this.disconnected = true;
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Receive one message from the socket.  This call blocks until a message has arrived.
        /// </summary>
        /// <returns></returns>
        public async Task<Message> ReceiveAsync()
        {
            Message msg = null;
            try
            {
                using (BinaryReader streamReader = new BinaryReader(socketStream, Encoding.UTF8, true))
                {
                    int len = streamReader.ReadInt32();
                    byte[] block = streamReader.ReadBytes(len);

                    object result = serializer.ReadObject(new MemoryStream(block));
                    var wrapper = result as MessageWrapper;
                    if (wrapper != null && wrapper.Message is Message)
                    {
                        msg = (Message)wrapper.Message;
                        if (msg.Id == SmartSocket.DisconnectMessageId)
                        {
                            // client is politely saying good bye...
                            OnClosed();
                            msg = null;
                        }
                        else if (msg.Id == SmartSocket.ConnectedMessageId)
                        {
                            this.Name = msg.Sender;
                            await this.SendResponseAsync(new Message(ConnectedMessageAck, this.Name));
                            msg = null;
                        }
                    }
                }
            }
            catch (EndOfStreamException eos)
            {
                OnError(eos);
            }
            catch (System.IO.IOException ioe)
            {
                System.Net.Sockets.SocketException se = ioe.InnerException as System.Net.Sockets.SocketException;
                if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    OnClosed();
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            return msg;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~SmartSocket()
        {
            this.Dispose(false);
        }

    }

}

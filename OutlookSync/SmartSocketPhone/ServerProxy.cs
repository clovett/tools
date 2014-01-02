using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Microsoft.Networking
{
    public class ServerExceptionEventArgs
    {
        public Exception Exception { get; set; }
    }

    public class ServerProxy
    {
        StreamSocket client;
        DataReader reader;
        DataWriter writer;
        bool stopped;
        IPEndPoint endPoint;
        XmlSerializer serializer = new XmlSerializer(typeof(Message));

        public event EventHandler<MessageEventArgs> MessageReceived;

        internal async Task ConnectAsync(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
            client = new StreamSocket();
            await client.ConnectAsync(new HostName(endPoint.Address.ToString()), endPoint.Port.ToString());
            reader = new DataReader(client.InputStream);
            writer = new DataWriter(client.OutputStream);
            var nowait = Task.Run(new Action(ReadThread));
        }


        internal void Stop()
        {
            stopped = true;
            using (reader)
            {
                reader = null;
            }
            using (writer)
            {
                writer = null;
            }
            using (client)
            {
                client = null;
            }
        }

        public event EventHandler<ServerExceptionEventArgs> ReadException;

        private async void ReadThread()
        {
            try
            {
                while (!stopped)
                {
                    await reader.LoadAsync(4);
                    int length = reader.ReadInt32();
                    await reader.LoadAsync((uint)length);
                    byte[] buffer = new byte[length];
                    reader.ReadBytes(buffer);
                    
                    MemoryStream ms = new MemoryStream(buffer);
                    Message msg = (Message)serializer.Deserialize(ms);
                    var args = new MessageEventArgs(endPoint, msg);
                    if (MessageReceived != null)
                    {
                        MessageReceived(this, args);
                    }
                    if (args.Response != null)
                    {
                        SendMessage(args.Response).Wait();
                    }
                }
            }
            catch (Exception x)
            {
                Debug.WriteLine("ServerProxy read caught {0}: {1}", x.GetType().Name, x.Message);
                if (ReadException != null)
                {
                    ReadException(this, new ServerExceptionEventArgs() { Exception = x });
                }
            }
        }

        public async Task SendMessage(Message message)
        {
            MemoryStream ms = new MemoryStream();
            serializer.Serialize(ms, message);
            ms.Seek(0, SeekOrigin.Begin);
            int len = (int)ms.Length;
            writer.WriteInt32(len);
            byte[] bytes = ms.ToArray();
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
        }

    }
}

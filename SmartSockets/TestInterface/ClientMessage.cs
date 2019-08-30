using LovettSoftware.Networking.SmartSockets;
using System;
using System.Runtime.Serialization;

namespace ConsoleInterface
{
    [DataContract]
    public class ClientMessage : SocketMessage
    {
        public ClientMessage(string id, string name, DateTime timestamp)
            : base(id, name)
        {
            Timestamp = timestamp;
        }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    [DataContract]
    public class ServerMessage : SocketMessage
    {
        public ServerMessage(string id, string name, DateTime timestamp)
            : base(id, name)
        {
            Timestamp = timestamp;
        }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}


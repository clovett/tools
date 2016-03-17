using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Networking.SmartSockets
{
    /// <summary>
    /// Special built in message types used for book keeping.
    /// Custom messages will use MessageType.None.
    /// </summary>
    public enum MessageType
    {
        None,
        Connect,
        Disconnect,
        Ping
    }

    /// <summary>
    /// You can subclass this class and the contents will be serialized as JSon.
    /// So long as your properties are serializable by Newtonsoft.JSon.JSonConvert you should be good.
    /// The Id and Timestamp properties are automatically filled in.  The Id from the client should
    /// be returned in a Reply message from the server to help with message correlation.
    /// </summary>
    public class Message
    {
        public long Id { get; set; }

        public long Timestamp { get; set; }

        public string Name { get; set; }

        public MessageType Type { get; set; }

        /// <summary>
        ///  This method is called to write this Message to the given writer, the base implementation writes the Id, Timestamp
        ///  and Name, but you can override it to add your custom fields.  If you do this remember to set a MessageFactory on
        ///  the StreamSocketClient so it knows to create your new type of Message object.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Timestamp);
            writer.Write((int)Type);
            writer.Write(Name);
        }

        /// <summary>
        ///  This method is called to read a Message from the reader, the base implementation reads the Id and Timestamp
        ///  but you can override it to add your custom fields. If you do this remember to set a MessageFactory on
        ///  the StreamSocketClient so it knows to create your new type of Message object.
        /// </summary>
        public virtual void Read(BinaryReader reader)
        {
            this.Id = reader.ReadInt64();
            this.Timestamp = reader.ReadInt64();
            int t = reader.ReadInt32();
            this.Type = (MessageType)t;
            this.Name = reader.ReadString();
        }

    }
}

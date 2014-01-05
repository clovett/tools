using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace OutlookSync.Model
{
    public class SyncMessage
    {
        List<ContactVersion> contacts;

        public SyncMessage()
        {
            contacts = new List<ContactVersion>();
        }

        public List<ContactVersion> Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }


        public string ToXml()
        {
            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                XmlSerializer s = new XmlSerializer(typeof(SyncMessage));
                s.Serialize(writer, this);
            }
            return sw.ToString();
        }

        public static SyncMessage Parse(string xml)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
                {
                    XmlSerializer s = new XmlSerializer(typeof(SyncMessage));
                    return (SyncMessage)s.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SyncMessage parse caught exception: " + ex.Message);
                return null;
            }
        }
    }

    public class ContactVersion
    {
        public ContactVersion()
        {
        }

        public string Id { get; set; }
        public int VersionNumber { get; set; }
        public bool Deleted { get; set; }
        public bool Inserted { get; set; }
    }

}

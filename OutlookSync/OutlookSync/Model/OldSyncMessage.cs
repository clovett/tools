using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace OutlookSync.Model.Old
{
    /// <summary>
    /// This is the old sync messaged used on phone version 1.2.
    /// </summary>
    public class SyncMessage
    {
        List<ContactVersion> contacts;

        public SyncMessage()
        {
            contacts = new List<ContactVersion>();
        }

        [XmlArrayItem(ElementName = "cv", Type = typeof(ContactVersion))]
        public List<ContactVersion> Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }


        public string ToXml()
        {
            StringWriter sw = new StringWriter();
            using (XmlWriter writer = XmlWriter.Create(sw))
            {
                XmlSerializer s = new XmlSerializer(typeof(SyncMessage));
                s.Serialize(writer, this);
            }
            string xml = sw.ToString();
            return xml;
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

        public ContactVersion(UnifiedContact contact)
        {
            this.Id = contact.OutlookEntryId;
            this.Name = contact.DisplayName;
            this.VersionNumber = contact.VersionNumber;
        }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Id { get; set; }

        [XmlElement(ElementName = "v")]
        public int VersionNumber { get; set; }

        [XmlElement(ElementName = "d")]
        public bool Deleted { get; set; }

        [XmlElement(ElementName = "i")]
        public bool Inserted { get; set; }
    }

}

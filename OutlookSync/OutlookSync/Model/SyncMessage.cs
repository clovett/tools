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
            this.Id = contact.Id;
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

    public class SyncResult
    {
        public List<ContactVersion> ServerInserted = new List<ContactVersion>();
        public List<ContactVersion> ServerDeleted = new List<ContactVersion>();
        public List<ContactVersion> ServerUpdated = new List<ContactVersion>();

        public List<ContactVersion> PhoneUpdated = new List<ContactVersion>();
        public List<ContactVersion> PhoneInserted = new List<ContactVersion>();
        public List<ContactVersion> PhoneDeleted = new List<ContactVersion>();

        public List<ContactVersion> Unchanged = new List<ContactVersion>();

        public List<ContactVersion> GetTotalInserted()
        {
            return Concat(ServerInserted, PhoneInserted);
        }
        
        public List<ContactVersion> GetTotalUpdated()
        {
            return Concat(ServerUpdated, PhoneUpdated);
        }

        public List<ContactVersion> GetTotalDeleted()
        {
            return Concat(ServerDeleted, PhoneDeleted);
        }

        // for progress bar
        public double TotalChanges { get; set; }
        
        public SyncResult()
        {
        }

        public SyncResult(SyncMessage phoneReport, bool phone)
        {
            int time = UnifiedStore.SyncTime;
            List<ContactVersion> updated = new List<ContactVersion>();
            List<ContactVersion> deleted = new List<ContactVersion>();
            List<ContactVersion> inserted = new List<ContactVersion>();
            List<ContactVersion> unchanged = new List<ContactVersion>();

            foreach (var cv in phoneReport.Contacts)
            {
                if (cv.Deleted)
                {
                    deleted.Add(cv);
                }
                else if (cv.Inserted)
                {
                    inserted.Add(cv);
                }
                else if (cv.VersionNumber == time)
                {
                    updated.Add(cv);
                }
                else
                {
                    unchanged.Add(cv);
                }
            }

            if (phone)
            {
                this.PhoneInserted = inserted;
                this.PhoneDeleted = deleted;
                this.PhoneUpdated = updated;
            }
            else
            {
                this.ServerInserted = inserted;
                this.ServerDeleted = deleted;
                this.ServerUpdated = updated;
            }

            // deletes are instant
            this.TotalChanges = updated.Count + inserted.Count; 

            this.Unchanged = unchanged;
        }

        List<ContactVersion> Concat(List<ContactVersion> list1, List<ContactVersion> list2)
        {
            if (list1 == null && list2 == null)
            {
                return null;
            }
            else if (list1 == null)
            {
                return list2;
            }
            else if (list2 == null)
            {
                return list1;
            }
            List<ContactVersion> combined = new List<ContactVersion>(list1);
            combined.AddRange(list2);
            return combined;
        }

    }

}

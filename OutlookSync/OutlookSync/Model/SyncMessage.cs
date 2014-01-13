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
        List<SyncItem> items;

        public SyncMessage()
        {
            items = new List<SyncItem>();
            Version = "2";
        }

        [XmlAttribute]
        public string Version { get; set; }

        [XmlArrayItem(ElementName = "item", Type = typeof(SyncItem))]
        public List<SyncItem> Items
        {
            get { return items; }
        }

        public string ToXml()
        {
            if (Version == "1")
            {
                // convert back to old format
                OutlookSync.Model.Old.SyncMessage oldMsg = new OutlookSync.Model.Old.SyncMessage();
                foreach (var item in this.Items)
                {
                    OutlookSync.Model.Old.ContactVersion cv = new Old.ContactVersion() { Id = item.Id, Name = item.Name, VersionNumber = item.VersionNumber };
                    switch (item.Change)
                    {
                        case ChangeType.None:
                            break;
                        case ChangeType.Insert:
                            cv.Inserted = true;
                            break;
                        case ChangeType.Update:
                            break;
                        case ChangeType.Delete:
                            cv.Deleted = true;
                            break;
                        default:
                            break;
                    }
                    oldMsg.Contacts.Add(cv);
                }
                return oldMsg.ToXml();
            }
            else
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
        }

        public static SyncMessage Parse(string xml)
        {
            try
            {
                if (xml.Contains("<Contacts"))
                {
                    SyncMessage newmsg = new SyncMessage();
                    newmsg.Version = "1"; // remember this is the old format.

                    // this is the old format, so we must be talking to the published phone app.
                    OutlookSync.Model.Old.SyncMessage oldMsg = OutlookSync.Model.Old.SyncMessage.Parse(xml);
                    foreach (var cv in oldMsg.Contacts)
                    {
                        SyncItem item = new SyncItem() { Id = cv.Id, Name = cv.Name, VersionNumber = cv.VersionNumber };
                        if (cv.Deleted)
                        {
                            item.Change = ChangeType.Delete;
                        }
                        else if (cv.Inserted)
                        {
                            item.Change = ChangeType.Insert;
                        }
                        newmsg.Items.Add(item);
                    }
                    return newmsg;
                }
                else
                {
                    using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
                    {
                        XmlSerializer s = new XmlSerializer(typeof(SyncMessage));
                        return (SyncMessage)s.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SyncMessage parse caught exception: " + ex.Message);
                return null;
            }
        }
    }

    public enum ChangeType 
    {
        None,
        Insert,
        Update,
        Delete
    }

    public class SyncItem
    {
        public SyncItem()
        {
        }

        public SyncItem(UnifiedContact contact)
        {
            this.Id = contact.OutlookEntryId;
            this.Type = "C";
            this.VersionNumber = contact.VersionNumber;
            this.Name = contact.DisplayName;
        }
        
        public SyncItem(UnifiedAppointment appointment)
        {
            this.Id = appointment.Id;
            this.PhoneId = appointment.PhoneId;
            this.Type = "A";
            this.VersionNumber = appointment.VersionNumber;
            this.Name = appointment.Start.Date.ToShortDateString() + " " + appointment.Subject;
        }

        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public ChangeType Change { get; set; }

        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute(AttributeName="pid")]
        public string PhoneId { get; set; }

        [XmlElement(ElementName = "v")]
        public int VersionNumber { get; set; }

        /// <summary>
        /// This is not serialized, but it used in the UI reports.
        /// </summary>
        [XmlIgnore]
        public string Name { get; set; }

    }

    public class SyncResult
    {
        public List<SyncItem> ServerInserted = new List<SyncItem>();
        public List<SyncItem> ServerDeleted = new List<SyncItem>();
        public List<SyncItem> ServerUpdated = new List<SyncItem>();

        public List<SyncItem> PhoneUpdated = new List<SyncItem>();
        public List<SyncItem> PhoneInserted = new List<SyncItem>();
        public List<SyncItem> PhoneDeleted = new List<SyncItem>();

        public List<SyncItem> Unchanged = new List<SyncItem>();

        public List<SyncItem> GetTotalInserted()
        {
            return Concat(ServerInserted, PhoneInserted);
        }
        
        public List<SyncItem> GetTotalUpdated()
        {
            return Concat(ServerUpdated, PhoneUpdated);
        }

        public List<SyncItem> GetTotalDeleted()
        {
            return Concat(ServerDeleted, PhoneDeleted);
        }

        // for progress bar
        public double TotalChanges { get; set; }
        
        public SyncResult()
        {
        }

        public SyncResult(SyncMessage syncMsg, bool phone)
        {
            int time = UnifiedStore.SyncTime;
            List<SyncItem> updated = new List<SyncItem>();
            List<SyncItem> deleted = new List<SyncItem>();
            List<SyncItem> inserted = new List<SyncItem>();
            List<SyncItem> unchanged = new List<SyncItem>();

            foreach (var item in syncMsg.Items)
            {
                if (item.Change == ChangeType.Delete)
                {
                    deleted.Add(item);
                }
                else if (item.Change == ChangeType.Insert)
                {
                    inserted.Add(item);
                }
                else if (item.VersionNumber == time)
                {
                    updated.Add(item);
                }
                else
                {
                    unchanged.Add(item);
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

        List<SyncItem> Concat(List<SyncItem> list1, List<SyncItem> list2)
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
            List<SyncItem> combined = new List<SyncItem>(list1);
            combined.AddRange(list2);
            return combined;
        }

    }

}

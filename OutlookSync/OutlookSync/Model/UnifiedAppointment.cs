using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using OutlookSync.Utilities;

namespace OutlookSync.Model
{
    public class UnifiedAppointment : PropertyBag
    {
        // outlook id
        public string Id
        {
            get { return GetValue<string>("Id"); }
            set { SetValue<string>("Id", value); }
        }

        public string PhoneId
        {
            get { return GetValue<string>("PhoneId"); }
            set { SetValue<string>("PhoneId", value); }
        }

        public PropertyList<UnifiedAttendee> Attendees
        {
            get { return GetValue<PropertyList<UnifiedAttendee>>("Attendees"); }
            private set { SetValue<PropertyList<UnifiedAttendee>>("Attendees", value); }
        }

        public string Subject
        {
            get { return GetValue<string>("Subject"); }
            set { SetValue<string>("Subject", value); }
        }


        public string  Hash
        {
            get { return GetValue<string>("Hash"); }
            set { SetValue<string>("Hash", value); }
        }

        // this is not stored, it is only sent over the wire when syncing appointments.
        public string Details
        {
            get { return GetValue<string>("Details"); }
            set { SetValue<string>("Details", value); }
        }

        public string Location
        {
            get { return GetValue<string>("Location"); }
            set { SetValue<string>("Location", value); }
        }

        public override string ToString()
        {
            return string.Format("Appointment by {0}, about {1} from {2} to {3}", Organizer.Email, this.Subject, this.Start.ToString(), this.End.ToString());
        }

        public DateTimeOffset Start
        {
            get { return GetValue<DateTimeOffset>("Start"); }
            set { SetValue<DateTimeOffset>("Start", value); }
        }


        public DateTimeOffset End
        {
            get { return GetValue<DateTimeOffset>("End"); }
            set { SetValue<DateTimeOffset>("End", value); }
        }


        public bool IsAllDayEvent
        {
            get { return GetValue<bool>("AllDay"); }
            set { SetValue<bool>("AllDay", value); }
        }

        public bool IsPrivate
        {
            get { return GetValue<bool>("IsPrivate"); }
            set { SetValue<bool>("IsPrivate", value); }
        }


        public UnifiedAttendee Organizer
        {
            get { return GetValue<UnifiedAttendee>("Organizer"); }
            set { SetValue<UnifiedAttendee>("Organizer", value); }
        }

        public AppointmentStatus Status
        {
            get { return GetValue<AppointmentStatus>("Status"); }
            set { SetValue<AppointmentStatus>("Status", value); }
        }


        internal UnifiedAttendee GetAttendee(string name)
        {
            if (Attendees != null)
            {
                return (from a in Attendees where a.Name == name select a).FirstOrDefault();
            }
            return null;
        }

        internal void AddAttendee(string name, string email)
        {
            UnifiedAttendee a = GetAttendee(name);
            if (a != null)
            {
                a.Email = email;
            }
            else 
            {
                if (Attendees == null)
                {
                    Attendees = new PropertyList<UnifiedAttendee>();
                    Attendees.Parent = GetPropertyValue("Attendees");
                }
                Attendees.Add(new UnifiedAttendee() { Name = name, Email = email });
            }
        }

        internal void RemoveAttendee(string name)
        {
            if (Attendees != null)
            {
                foreach (var a in (from a in Attendees where a.Name == name select a))
                {
                    // don't actually remove it so we remember this was deleted.
                    // We'll come back and do a cleanup pass on these later after both sides are in sync.
                    a.Deleted = true;
                }
            }
        }

        /// <summary>
        /// This is a non serialized property that can be used to link the unified appointment to the real 
        /// Outlook or Phone object so we don't have to keep separate indexes on each type.
        /// </summary>
        public object LocalStoreObject { get; set; }

        internal static UnifiedAppointment Parse(string xml)
        {
            UnifiedAppointment appointment = new UnifiedAppointment();
            try
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
                {
                    UnifiedStoreSerializer serializer = new UnifiedStoreSerializer(reader);
                    while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.LocalName == "UnifiedAppointment")
                            {
                                appointment.Read(serializer);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception parsing contact xml: " + ex.ToString());
                return null;
            }
            return appointment;
        }

    }


    public enum AppointmentStatus
    {
        // Summary:
        //     The attendee is free during this appointment.
        Free = 0,
        //
        // Summary:
        //     The attendee is tentatively busy during this appointment.
        Tentative = 1,
        //
        // Summary:
        //     The attendee is busy during this appointment.
        Busy = 2,
        //
        // Summary:
        //     The attendee is out of the office during this appointment.
        OutOfOffice = 3,
    }


    public class UnifiedAttendee : PropertyBag
    {
        public string Name
        {
            get { return GetValue<string>("Name"); }
            set { SetValue<string>("Name", value); }
        }
        
        public string Email
        {
            get { return GetValue<string>("Email"); }
            set { SetValue<string>("Email", value); }
        }

        public bool Deleted
        {
            get { return GetValue<bool>("Deleted"); }
            set { SetValue<bool>("Deleted", value); }
        }
    }
}

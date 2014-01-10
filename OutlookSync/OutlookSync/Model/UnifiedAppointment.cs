using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // the computed phone id (which is not persisted because the computed id could
        // change each time we load the phone appointments).
        public string PhoneId
        {
            get;
            set; 
        }

        public int Hash
        {
            get { return GetValue<int>("Hash"); }
            set { SetValue<int>("Hash", value); }
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

        // We don't want to store the entire details, an MD5 hash should be good enough for detecting changes.
        public string DetailsMd5Hash
        {
            get { return GetValue<string>("Details"); }
            set { SetValue<string>("Details", value); }
        }

        public string Location
        {
            get { return GetValue<string>("Location"); }
            set { SetValue<string>("Location", value); }
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


        internal UnifiedAttendee GetAttendee(string email)
        {
            if (Attendees != null)
            {
                return (from a in Attendees where a.Email == email select a).FirstOrDefault();
            }
            return null;
        }

        internal void AddAttendee(string name, string email)
        {
            UnifiedAttendee a = GetAttendee(email);
            if (a != null)
            {
                a.Name = name;
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

        internal void RemoveAttendee(string email)
        {
            if (Attendees != null)
            {
                foreach (var a in (from a in Attendees where a.Email == email select a))
                {
                    // don't actually remove it so we remember this was deleted.
                    // We'll come back and do a cleanup pass on these later after both sides are in sync.
                    a.Deleted = true;
                }
            }
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

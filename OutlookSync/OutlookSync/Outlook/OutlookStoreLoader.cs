using OutlookSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;
using System.Diagnostics;
using System.Threading;
using System.Security;

namespace OutlookSync.Model
{
    class OutlookStoreLoader
    {
        Microsoft.Office.Interop.Outlook.Application outlook;

        Dictionary<string, AddressEntry> localAddresses = new Dictionary<string, AddressEntry>();

        UnifiedStore store;
        DateTime syncTime;
        Dictionary<string, AppointmentItem> appointmentIndex = new Dictionary<string, AppointmentItem>();

        Dictionary<string, UnifiedContact> contactsDeletedLocally = new Dictionary<string, UnifiedContact>();
        Dictionary<string, UnifiedContact> contactsAddedLocally = new Dictionary<string, UnifiedContact>();

        Dictionary<string, UnifiedAppointment> appointmentsDeletedLocally = new Dictionary<string, UnifiedAppointment>();
        Dictionary<string, UnifiedAppointment> appointmentsAddedLocally = new Dictionary<string, UnifiedAppointment>();


        public OutlookStoreLoader()
        {
        }

        public void StartOutlook()
        {
            outlook = new Microsoft.Office.Interop.Outlook.Application();

            // index the address entries so we can match them up on new appointment recipients.
            for (int i = 1; i <= outlook.Session.AddressLists.Count; i++)
            {                
                AddressList addressBook = outlook.Session.AddressLists[i];
                foreach (AddressEntry e in addressBook.AddressEntries)
                {
                    localAddresses[e.Name] = e;
                }
            }
        }

        /// <summary>
        /// Update the unified store with whatever is currently in Outlook.
        /// </summary>
        /// <param name="store">The store to update</param>
        public async Task UpdateAsync(UnifiedStore store)
        {
            this.store = store;
            this.syncTime = DateTime.Now;
            if (outlook == null)
            {
                throw new System.Exception(Properties.Resources.NoOutlook);
            }

            await Task.Run(new System.Action(() =>
            {

                var mapi = outlook.GetNamespace("MAPI");

                HashSet<string> found = new HashSet<string>();

                LoadOutlookContactsAndAppointments(mapi.Folders, found);

                // see if user has deleted contacts in outlook
                foreach (UnifiedContact c in store.Contacts.ToArray())
                {
                    string id = c.Id;
                    if (string.IsNullOrEmpty(id) || !found.Contains(id))
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            contactsDeletedLocally[id] = c;
                        }
                        store.Contacts.Remove(c);
                    }
                }

                Log.WriteLine("Loaded {0} contacts", store.Contacts.Count);

                // see if user has deleted appointments in outlook
                foreach (UnifiedAppointment a in store.Appointments.ToArray())
                {
                    string id = a.Id;
                    if (string.IsNullOrEmpty(id) || !found.Contains(id))
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            appointmentsDeletedLocally[id] = a;
                        }
                        store.Appointments.Remove(a);
                    }
                }

            }));

        }

        private void LoadOutlookContactsAndAppointments(Folders folders, HashSet<string> found)
        {
            if (folders != null)
            {
                foreach (MAPIFolder folder in folders)
                {
                    string name = folder.Name;

                    switch (folder.DefaultItemType)
                    {
                        case OlItemType.olAppointmentItem:
                            LoadAppointments(folder, found);
                            break;

                        case OlItemType.olContactItem:
                            if (name == "Suggested Contacts")
                            {
                                continue;
                            }
                            LoadContacts(folder, found);
                            break;

                        case OlItemType.olDistributionListItem:
                        case OlItemType.olJournalItem:
                        case OlItemType.olMailItem:
                        case OlItemType.olMobileItemMMS:
                        case OlItemType.olMobileItemSMS:
                        case OlItemType.olNoteItem:
                        case OlItemType.olPostItem:
                        case OlItemType.olTaskItem:
                        default:
                            break;
                    }

                    LoadOutlookContactsAndAppointments(folder.Folders, found);
                }
            }
        }

        private void LoadAppointments(MAPIFolder folder, HashSet<string> found)
        {
            foreach (object item in folder.Items)
            {
                AppointmentItem appointment = item as AppointmentItem;
                if (appointment != null)
                {
                    if (appointment.EntryID != null)
                    {
                        found.Add(appointment.EntryID);
                    }
                    MergeAppointment(appointment);
                }
            }
        }

        private void LoadContacts(MAPIFolder folder, HashSet<string> found)
        {

            foreach (object item in folder.Items)
            {
                ContactItem contact = item as ContactItem;
                if (contact != null)
                {
                    if (contact.EntryID != null)
                    {
                        found.Add(contact.EntryID);
                    }
                    MergeContact(contact);
                }
            }
        }

        internal void DeleteContact(string id)
        {
            UnifiedContact cached = this.store.FindContactById(id);
            if (cached != null)
            {
                this.store.Contacts.Remove(cached);

                ContactItem item = cached.LocalStoreObject as ContactItem;
                if (item != null)
                {
                    item.Delete();
                    cached.LocalStoreObject = null;
                }
            }
        }

        private void DeleteAppointment(string id)
        {
            UnifiedAppointment cached = this.store.FindAppointmentById(id);
            if (cached != null)
            {
                this.store.Appointments.Remove(cached);
                AppointmentItem item = cached.LocalStoreObject as AppointmentItem;
                if (item != null)
                {
                    item.Delete();
                }
            }
        }

        private void MergeAppointment(AppointmentItem appointment)
        {
            DateTime endTime = appointment.End;
            DateTime startTime = appointment.Start;

            if (endTime < this.syncTime)
            {
                return;
                // not interested in bloating the phone with past history...
            }

            string id = appointment.EntryID;
            appointmentIndex[id] = appointment;

            UnifiedAppointment uc = store.FindAppointmentById(id);
            bool isNew = false;
            if (uc == null)
            {
                isNew = true;
                uc = new UnifiedAppointment();
                appointmentsAddedLocally[id] = uc;
                uc.Id = id;
            }

            uc.LocalStoreObject = appointment;

            UpdateAppointment(uc, appointment);

            // update the total version number.
            uc.VersionNumber = uc.GetHighestVersionNumber();

            if (isNew && !uc.IsEmpty)
            {
                store.Appointments.Add(uc);
            }
            else if (!isNew && uc.IsEmpty)
            {
                store.Appointments.Remove(uc);
            }

        }

        private void UpdateAppointment(UnifiedAppointment ua, AppointmentItem appointment)
        {
            ua.Start = appointment.Start;
            ua.End = appointment.End;
            ua.Subject = appointment.Subject;
            ua.IsAllDayEvent = appointment.AllDayEvent;
            // if the MF5 hash changes then chances are the body has changed, and the 
            // sync will happen and GetSyncXml will send the real body.  But this optimization
            // ensures our UnifiedStore doesn't get too big.  Technically we could do
            // this with all the string properties, but probably not a big win in storage space.
            ua.Hash = MD5.GetMd5String(appointment.Body);
            ua.Location = appointment.Location;

            switch (appointment.BusyStatus)
            {
                case OlBusyStatus.olBusy:
                    ua.Status = AppointmentStatus.Busy;
                    break;
                case OlBusyStatus.olFree:
                    ua.Status = AppointmentStatus.Free;
                    break;
                case OlBusyStatus.olOutOfOffice:
                    ua.Status = AppointmentStatus.OutOfOffice;
                    break;
                case OlBusyStatus.olTentative:
                    ua.Status = AppointmentStatus.Tentative;
                    break;
            }

            string organizer = appointment.Organizer;
            if (string.IsNullOrEmpty(organizer))
            {
                ua.Organizer = null;
            }
            else
            {
                UnifiedAttendee org = ua.Organizer;
                if (org == null)
                {
                    org = new UnifiedAttendee();
                    ua.Organizer = org;
                }
                org.Name = organizer;
                // todo: parse out the email somehow?
            }

            Dictionary<string, UnifiedAttendee> existing = new Dictionary<string, UnifiedAttendee>();
            if (ua.Attendees != null)
            {
                foreach (UnifiedAttendee a in ua.Attendees)
                {
                    existing[a.Name] = a;
                }
            }

            foreach (Recipient r in appointment.Recipients)
            {
                UnifiedAttendee a = null;
                if (existing.TryGetValue(r.Name, out a))
                {
                    a.Email = r.Address;
                    existing.Remove(r.Name);
                }
                else
                {
                    a = new UnifiedAttendee() { Name = r.Name, Email = r.Address };
                    ua.AddAttendee(r.Name, r.Address);
                }
            }

            // if it was not found in the recipients list we need to remove it from our cache.
            foreach (UnifiedAttendee toRemove in existing.Values)
            {
                ua.RemoveAttendee(toRemove.Name);
            }

            // todo: how to setup occurrences on the phone, the API doesn't seem to allow it...
            if (appointment.RecurrenceState == OlRecurrenceState.olApptOccurrence)
            {
                // skip it...
            }

        }

        private void MergeContact(ContactItem contact)
        {
            string id = contact.EntryID;

            UnifiedContact uc = store.FindContactById(id);
            bool isNew = false;
            if (uc == null)
            {
                isNew = true;
                uc = new UnifiedContact();
                contactsAddedLocally[id] = uc;
                uc.Id = id;         
            }

            uc.LocalStoreObject = contact;

            UpdateContact(uc, contact);

            // update the total version number.
            uc.VersionNumber = uc.GetHighestVersionNumber();

            if (isNew && !uc.IsEmpty)
            {
                store.Contacts.Add(uc);
            }
            else if (!isNew && uc.IsEmpty)
            {
                store.Contacts.Remove(uc);
            }
        }

        private void UpdateContact(UnifiedContact uc, ContactItem contact)
        {
            try
            {
                UpdateAddress(uc, AddressKind.Work, contact.BusinessAddress, contact.BusinessAddressStreet, contact.BusinessAddressCity, contact.BusinessAddressState, contact.BusinessAddressPostalCode, contact.BusinessAddressCountry);
                UpdateAddress(uc, AddressKind.Home, contact.HomeAddress, contact.HomeAddressStreet, contact.HomeAddressCity, contact.HomeAddressState, contact.HomeAddressPostalCode, contact.HomeAddressCountry);
                UpdateAddress(uc, AddressKind.Other, contact.OtherAddress, contact.OtherAddressStreet, contact.OtherAddressCity, contact.OtherAddressState, contact.OtherAddressPostalCode, contact.OtherAddressCountry);

                uc.DisplayName = contact.FullName;

                PersonName pn = uc.Name;
                if (pn == null) 
                {
                    pn = new PersonName();
                }
                
                pn.FirstName = contact.FirstName;
                pn.LastName = contact.LastName;
                pn.MiddleName = contact.MiddleName;
                pn.Suffix = contact.Suffix;
                pn.Title = contact.Title;

                if (uc.Name != pn && !pn.IsEmpty)
                {
                    uc.Name = pn;
                }
                
                uc.SignificantOthers = contact.Spouse;

                UpdateEmailAddress(uc, EmailAddressKind.Personal, contact.Email1Address);
                UpdateEmailAddress(uc, EmailAddressKind.Work, contact.Email2Address);
                UpdateEmailAddress(uc, EmailAddressKind.Other, contact.Email3Address);

                UpdatePhoneNumber(uc, PhoneNumberKind.Home, contact.HomeTelephoneNumber);
                UpdatePhoneNumber(uc, PhoneNumberKind.Work, contact.BusinessTelephoneNumber);
                UpdatePhoneNumber(uc, PhoneNumberKind.WorkFax, contact.BusinessFaxNumber);
                UpdatePhoneNumber(uc, PhoneNumberKind.Mobile, contact.MobileTelephoneNumber);
                UpdatePhoneNumber(uc, PhoneNumberKind.Company, contact.CompanyMainTelephoneNumber);
                UpdatePhoneNumber(uc, PhoneNumberKind.HomeFax, contact.HomeFaxNumber);
                UpdatePhoneNumber(uc, PhoneNumberKind.Pager, contact.PagerNumber);

                UpdateWebSites(uc, CreateMinimalSet(new string[] { contact.BusinessHomePage, contact.PersonalHomePage, contact.WebPage }));
                uc.Children = contact.Children;
                uc.Nickname = contact.NickName;

                uc.VersionNumber = uc.GetHighestVersionNumber();

            }
            catch (System.Exception ex)
            {
                Log.WriteException("Caught exception in UpdateContact", ex);
            }
        }

        HashSet<string> CreateMinimalSet(IEnumerable<string> items)
        {
            HashSet<string> set = new HashSet<string>();
            if (items != null)
            {
                foreach (string s in items)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        set.Add(s);
                    }
                }
            }
            return set;
        }

        private void UpdateWebSites(UnifiedContact uc, HashSet<string> websites)
        {
            HashSet<string> set = CreateMinimalSet(uc.Websites == null ? (IEnumerable<string>)new string[0] : (IEnumerable<string>)uc.Websites);
            if (!set.SetEquals(websites))
            {
                // update the property then.
                uc.Websites = new PropertyList<string>(websites);
                uc.Websites.Parent = uc.GetPropertyValue("Websites");
            }
            else if (uc.Websites != null && uc.Websites.Count == 0)
            {
                uc.Websites = null;
            }
        }

        private void UpdatePhoneNumber(UnifiedContact uc, PhoneNumberKind kind, string number)
        {
            ContactPhoneNumber ca = uc.GetPhoneNumber(kind);

            if (string.IsNullOrEmpty(number))
            {
                if (ca != null)
                {
                    uc.RemovePhoneNumber(kind);
                }
                return;
            }
            else if (ca == null)
            {
                ca = new ContactPhoneNumber() { Kind = kind };
                uc.AddPhoneNumber(ca);
            }
            ca.PhoneNumber = number;
        }

        private void UpdateEmailAddress(UnifiedContact uc, EmailAddressKind kind, string address)
        {
            ContactEmailAddress ca = uc.GetEmailAddress(kind);

            if (string.IsNullOrEmpty(address))
            {
                if (ca != null)
                {
                    uc.RemoveEmailAddress(kind);
                }
                return;
            }
            else if (ca == null) 
            {
                ca = new ContactEmailAddress() { Kind = kind };
                uc.AddEmailAddress(ca);
            }
            ca.EmailAddress = address;
        }

        private void UpdateAddress(UnifiedContact uc, AddressKind kind, string display, string street, string city, string state, string postcode, string country)
        {
            ContactAddress ca = uc.GetAddress(kind);
            PhysicalAddress addr = (ca != null ? ca.PhysicalAddress : null);

            if (string.IsNullOrEmpty(street) && string.IsNullOrEmpty(city) && string.IsNullOrEmpty(state) && string.IsNullOrEmpty(postcode) && string.IsNullOrEmpty(country))
            {
                if (string.IsNullOrEmpty(display))
                {
                    if (ca != null)
                    {
                        uc.RemoveAddress(kind);
                    }
                    return;
                }
                else if (addr == null)
                {
                    // problem parsing the "display" address?
                    addr = new PhysicalAddress()
                    {
                        AddressLine1 = display
                    };
                }
                else
                {
                    addr.AddressLine1 = display;
                    addr.AddressLine2 = addr.Building = addr.City = addr.CountryRegion = addr.FloorLevel = addr.PostalCode = addr.StateProvince = null;
                }
            }
            else
            {
                if (ca == null)
                {
                    ca = new ContactAddress() { Kind = kind };
                    uc.AddAddress(ca);
                }

                if (addr == null)
                {
                    // ignore the "display" string
                    addr = new PhysicalAddress();
                    ca.PhysicalAddress = addr;
                }

                addr.AddressLine1 = street;
                addr.City = city;
                addr.StateProvince = state;
                addr.PostalCode = postcode;
                addr.CountryRegion = country;
                
                if (ca != null && ca.PhysicalAddress != addr)
                {
                    ca.PhysicalAddress = addr;
                }
            }
        }


        /// <summary>
        /// Update Outlook with the changes we just got from the phone...
        /// </summary>
        /// <param name="cached"></param>
        /// <returns>The new outlook id if this is a new contact</returns>
        internal string UpdateContact(UnifiedContact cached)
        {
            try
            {
                ContactItem item = cached.LocalStoreObject as ContactItem;
                if (item == null)
                {
                    // new contact then!
                    item = outlook.CreateItem(OlItemType.olContactItem);
                    item.Account = "Outlook Sync";
                }
                
                MergeContact(cached, item);                

                // let Outlook save the item in the default folder for the item type.                    
                item.Save();

                return item.EntryID;
            }
            catch (System.Exception ex)
            {
                Log.WriteException("Caught exception in UpdateContact", ex);
                return null;
            }
        }

        /// <summary>
        /// Push changes from UnifiedContact back into Outlook.
        /// </summary>
        internal void MergeContact(UnifiedContact uc, ContactItem contact)
        {
            ContactAddress address = uc.GetAddress(AddressKind.Work);
            if (address != null && address.PhysicalAddress != null)
            {
                var pa = address.PhysicalAddress;
                contact.BusinessAddressStreet = pa.AddressLine1;
                contact.BusinessAddressCity = pa.City;
                contact.BusinessAddressState = pa.StateProvince;
                contact.BusinessAddressPostalCode = pa.PostalCode;
                contact.BusinessAddressCountry = pa.CountryRegion;
            }
            else
            {
                contact.BusinessAddressStreet = "";
                contact.BusinessAddressCity = "";
                contact.BusinessAddressState = "";
                contact.BusinessAddressPostalCode = "";
                contact.BusinessAddressCountry = "";
            }

            address = uc.GetAddress(AddressKind.Home);
            if (address != null && address.PhysicalAddress != null)
            {
                var pa = address.PhysicalAddress;
                contact.HomeAddressStreet = pa.AddressLine1;
                contact.HomeAddressCity = pa.City;
                contact.HomeAddressState = pa.StateProvince;
                contact.HomeAddressPostalCode = pa.PostalCode;
                contact.HomeAddressCountry = pa.CountryRegion;
            }
            else
            {
                contact.HomeAddressStreet = "";
                contact.HomeAddressCity = "";
                contact.HomeAddressState = "";
                contact.HomeAddressPostalCode = "";
                contact.HomeAddressCountry = "";
            }

            address = uc.GetAddress(AddressKind.Other);
            if (address != null && address.PhysicalAddress != null)
            {
                var pa = address.PhysicalAddress;
                contact.OtherAddressStreet = pa.AddressLine1;
                contact.OtherAddressCity = pa.City;
                contact.OtherAddressState = pa.StateProvince;
                contact.OtherAddressPostalCode = pa.PostalCode;
                contact.OtherAddressCountry = pa.CountryRegion;
            }
            else
            {
                contact.OtherAddressStreet = "";
                contact.OtherAddressCity = "";
                contact.OtherAddressState = "";
                contact.OtherAddressPostalCode = "";
                contact.OtherAddressCountry = "";
            }

            PersonName pn = uc.Name;
            if (pn == null || (string.IsNullOrEmpty(pn.FirstName) && 
                string.IsNullOrEmpty(pn.LastName) && string.IsNullOrEmpty(pn.MiddleName) &&
                string.IsNullOrEmpty(pn.Suffix) && string.IsNullOrEmpty(pn.Title)))
            {
                contact.FirstName   = "";
                contact.LastName    = "";
                contact.MiddleName  = "";
                contact.Suffix      = "";
                contact.Title = "";

                // must set this AFTER clearing the above fields because if it is an unparsable full name this makes sure we 
                // don't lose the DisplayName which happens when you clear the above fields for some odd reason.
                contact.FullName = uc.DisplayName;
            }
            else
            {
                contact.FullName = uc.DisplayName;
                contact.FirstName = pn.FirstName;
                contact.LastName = pn.LastName;
                contact.MiddleName = pn.MiddleName;
                contact.Suffix = pn.Suffix;
                contact.Title = pn.Title;
            }

            contact.Spouse = uc.SignificantOthers;

            var email = uc.GetEmailAddress(EmailAddressKind.Personal);
            if (email != null)
            {
                contact.Email1Address = email.EmailAddress;
            }
            else
            {
                contact.Email1Address = "";
            }

            email = uc.GetEmailAddress(EmailAddressKind.Work);
            if (email != null)
            {
                contact.Email2Address = email.EmailAddress;
            }
            else
            {
                contact.Email2Address = "";
            }

            email = uc.GetEmailAddress(EmailAddressKind.Other);
            if (email != null)
            {
                contact.Email3Address = email.EmailAddress;
            }
            else
            {
                contact.Email3Address = "";
            }

            var phone = uc.GetPhoneNumber(PhoneNumberKind.Home);
            if (phone != null)
            {
                contact.HomeTelephoneNumber = phone.PhoneNumber;
            }
            else
            {
                contact.HomeTelephoneNumber = "";
            }

            phone = uc.GetPhoneNumber(PhoneNumberKind.Work);
            if (phone != null)
            {
                contact.BusinessTelephoneNumber = phone.PhoneNumber;
            }
            else
            {
                contact.BusinessTelephoneNumber = "";
            }

            phone = uc.GetPhoneNumber(PhoneNumberKind.WorkFax);
            if (phone != null)
            {
                contact.BusinessFaxNumber = phone.PhoneNumber;
            }
            else
            {
                contact.BusinessFaxNumber = "";
            }

            phone = uc.GetPhoneNumber(PhoneNumberKind.Mobile);
            if (phone != null)
            {
                contact.MobileTelephoneNumber = phone.PhoneNumber;
            }
            else
            {
                contact.MobileTelephoneNumber = "";
            }

            phone = uc.GetPhoneNumber(PhoneNumberKind.Company);
            if (phone != null)
            {
                contact.CompanyMainTelephoneNumber = phone.PhoneNumber;
            }
            else
            {
                contact.CompanyMainTelephoneNumber = "";
            }

            phone = uc.GetPhoneNumber(PhoneNumberKind.HomeFax);
            if (phone != null)
            {
                contact.HomeFaxNumber = phone.PhoneNumber;
            }
            else
            {
                contact.HomeFaxNumber = "";
            }

            phone = uc.GetPhoneNumber(PhoneNumberKind.Pager);
            if (phone != null)
            {
                contact.PagerNumber = phone.PhoneNumber;
            }
            else
            {
                contact.PagerNumber = "";
            }

            // bugbug: windows phone doesn't remember which is which.
            if (uc.Websites != null)
            {
                if (uc.Websites.Count > 0)
                {
                    contact.WebPage = uc.Websites[0];
                }
                else
                {
                    contact.WebPage = "";
                }
                if (uc.Websites.Count > 1)
                {
                    contact.PersonalHomePage = uc.Websites[1];
                }
                else
                {
                    contact.PersonalHomePage = "";
                }
                if (uc.Websites.Count > 2)
                {
                    contact.BusinessHomePage = uc.Websites[2];
                }
                else
                {
                    contact.BusinessHomePage = "";
                }
            }
            else
            {
                contact.PersonalHomePage = "";
                contact.WebPage = "";
                contact.BusinessHomePage = "";
            }

            contact.Children = uc.Children;
            contact.NickName = uc.Nickname;

            DateTimeOffset? bday = uc.Birthday;
            if (bday.HasValue)
            {
                contact.Birthday = bday.Value.LocalDateTime;
            }
        }

        /// <summary>
        /// Update Outlook with the changes we just got from the phone...
        /// </summary>
        /// <param name="cached"></param>
        /// <returns>The new outlook id if this is a new appointment </returns>
        internal string UpdateAppointment(UnifiedAppointment cached)
        {
            try
            {
                AppointmentItem item = cached.LocalStoreObject as AppointmentItem;
                if (item == null)
                {
                    // new contact then!
                    item = outlook.CreateItem(OlItemType.olAppointmentItem);
                    cached.LocalStoreObject = item;
                }

                MergeAppointment(cached, item);

                // let Outlook save the item in the default folder for the item type.                    
                item.Save();

                return item.EntryID;
            }
            catch (System.Exception ex)
            {
                Log.WriteException("Caught exception in UpdateAppointment", ex);
                return null;
            }
        }

        /// <summary>
        /// Push changes from UnifiedAppointment back into Outlook.
        /// </summary>
        internal void MergeAppointment (UnifiedAppointment ua, AppointmentItem appointment)
        {
            appointment.Start = ua.Start.LocalDateTime;
            appointment.End = ua.End.LocalDateTime;
            appointment.Subject = ua.Subject;
            appointment.AllDayEvent = ua.IsAllDayEvent;
            appointment.Body = ua.Details;
            appointment.Location = ua.Location;

            switch (ua.Status)
            {
                case AppointmentStatus.Free:
                    appointment.BusyStatus = OlBusyStatus.olFree;
                    break;
                case AppointmentStatus.Tentative:
                    appointment.BusyStatus = OlBusyStatus.olTentative;
                    break;
                case AppointmentStatus.Busy:
                    appointment.BusyStatus = OlBusyStatus.olBusy;
                    break;
                case AppointmentStatus.OutOfOffice:
                    appointment.BusyStatus = OlBusyStatus.olOutOfOffice;
                    break;
                default:
                    break;
            }

            UnifiedAttendee organizer = ua.Organizer;
            if (organizer != null && organizer.Name != null)
            {
                //bugbug: how to set the organizer?
                //appointment.Organizer = organizer.Name;
                // todo: what about the email address?
            }

            Dictionary<string, Recipient> recipients = new Dictionary<string, Recipient>();
            Dictionary<Recipient,int> indexes = new Dictionary<Recipient,int>();

            int i = 1; // index is 1-based
            foreach (Recipient r in appointment.Recipients)
            {
                recipients[r.Name] = r;
                indexes[r] = i;
                i++;
            }

            if (ua.Attendees != null)
            {
                foreach (UnifiedAttendee a in ua.Attendees)
                {
                    Recipient r = null;
                    if (recipients.TryGetValue(a.Name, out r))
                    {
                        recipients.Remove(r.Name);
                    }
                    else
                    {
                        r = appointment.Recipients.Add(a.Name);
                        bool resolved = r.Resolve();
                        if (!resolved)
                        {
                            Debug.WriteLine(string.Format("Recipient {0} not found in local addresses", r.Name));
                        }
                    }

                    if (r.Address != null && a.Email != r.Address)
                    {
                        //bugbug: what to do here?
                        Debug.WriteLine(string.Format("Recipient {0} email '{1}' doesn't match phone email '{2}'", r.Name, r.Address, a.Email));
                    }
                }
            }

            // remove any remaining recipients that were not found in the UnifiedAppointment from the phone.
            foreach (Recipient r in recipients.Values)
            {
                int index = indexes[r];
                appointment.Recipients.Remove(index);
            }

            // todo: how to get occurrence information from the phone...


        }

        AddressEntry GetAddressEntry(string name)
        {
            try
            {
                // not sure we want to be doing this.
                AddressEntry e;
                if (localAddresses.TryGetValue(name, out e))
                {
                    return e;
                }

                // todo: should we add entries for the user???
                //return addressBook.AddressEntries.Add(addressType, name, email); 
            }
            catch (System.Exception)
            {
                return null;
            }
            return null;
        }

        internal SyncMessage GetLocalSyncMessage()
        {
            SyncMessage response = new SyncMessage();

            // first, our local deletes take precedence...
            foreach (var pair in this.contactsDeletedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Delete});
            }

            // and our local inserts the phone won't know about yet
            foreach (var pair in this.contactsAddedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Insert });
            }

            foreach (var contact in this.store.Contacts)
            {
                if (!this.contactsAddedLocally.ContainsKey(contact.Id))
                {
                    Debug.Assert(contact.VersionNumber == contact.GetHighestVersionNumber(), "version is not up to date");
                    response.Items.Add(new SyncItem(contact) { Change = ChangeType.Update });
                }
            }

            // first, our local deletes take precedence...
            foreach (var pair in this.appointmentsDeletedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Delete });
            }

            // and our local inserts the phone won't know about yet
            foreach (var pair in this.appointmentsAddedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Insert });
            }

            foreach (var appointment in this.store.Appointments)
            {
                if (!this.appointmentsAddedLocally.ContainsKey(appointment.Id))
                {
                    Debug.Assert(appointment.VersionNumber == appointment.GetHighestVersionNumber(), "version is not up to date");
                    response.Items.Add(new SyncItem(appointment) { Change = ChangeType.Update });
                }
            }

            return response;
        }

        internal SyncMessage PhoneSync(SyncMessage msg, SyncResult status, out int identical)
        {
            int same = 0;
            SyncMessage response = new SyncMessage();

            // first, our local deletes take precedence...
            foreach (var pair in this.contactsDeletedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Delete });
            }

            // and our local inserts the phone won't know about yet
            foreach (var pair in this.contactsAddedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Insert });
            }

            // first, our local deletes take precedence...
            foreach (var pair in this.appointmentsDeletedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Delete });
            }

            // and our local inserts the phone won't know about yet
            foreach (var pair in this.appointmentsAddedLocally)
            {
                response.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Insert });
            }

            // create index of items from the phone.
            Dictionary<string, SyncItem> map = new Dictionary<string, SyncItem>();

            // Ok, now if the phone has deleted stuff, let's take care of that
            foreach (var item in msg.Items)
            {
                if (item.Change == ChangeType.Delete)
                {
                    status.PhoneDeleted.Add(item);
                    if (item.Type == "C")
                    {
                        DeleteContact(item.Id);
                    }
                    else
                    {
                        DeleteAppointment(item.Id);
                    }
                }
                else if (item.Change == ChangeType.Insert)
                {
                    // wait for actual upload
                    //status.PhoneInserted.Add(contact);
                    Debug.WriteLine("new contact on phone");
                }
                else
                {
                    map[item.Id] = item;
                }
            }

            // Ok, compare the mapped contacts from phone with our own.
            foreach (var contact in this.store.Contacts)
            {
                string id = contact.Id;

                Debug.Assert(!contactsDeletedLocally.ContainsKey(id), "Should have been deleted???");

                if (!this.contactsAddedLocally.ContainsKey(id))
                {
                    int version = contact.VersionNumber;

                    Debug.Assert(version == contact.GetHighestVersionNumber(), "version is not up to date");

                    SyncItem phone = null;
                    if (map.TryGetValue(id, out phone))
                    {
                        if (phone.VersionNumber == version)
                        {
                            same++;
                        }
                        else if (phone.VersionNumber > version)
                        {
                            // wait for actual upload
                            //status.PhoneUpdated.Add(phone);
                        }
                        else if (phone.VersionNumber < version)
                        {
                            status.ServerUpdated.Add(phone);
                        }
                    }

                    response.Items.Add(new SyncItem(contact));
                }
            }


            // Ok, compare the mapped appointments from phone with our own.
            foreach (var appointment in this.store.Appointments)
            {
                string id = appointment.Id;

                Debug.Assert(!appointmentsDeletedLocally.ContainsKey(id), "Should have been deleted???");

                if (!this.appointmentsAddedLocally.ContainsKey(id))
                {
                    int version = appointment.VersionNumber;

                    Debug.Assert(version == appointment.GetHighestVersionNumber(), "version is not up to date");

                    SyncItem phone = null;
                    if (map.TryGetValue(id, out phone))
                    {
                        if (phone.VersionNumber == version)
                        {
                            same++;
                        }
                        else if (phone.VersionNumber > version)
                        {
                            // wait for actual upload
                            //status.PhoneUpdated.Add(phone);
                        }
                        else if (phone.VersionNumber < version)
                        {
                            status.ServerUpdated.Add(phone);

                        }
                    }
                    response.Items.Add(new SyncItem(appointment));
                }
            }

            identical = same;

            return response;
        }


        internal string GetSyncXml(UnifiedAppointment ua)
        {
            AppointmentItem appointment = ua.LocalStoreObject as AppointmentItem;
            if (appointment != null)
            {
                ua.Details = appointment.Body;
                string xml = ua.ToXml();
                ua.ClearValue("Details");
                return xml;
            }
            throw new System.Exception("Why is AppointmentItem missing?");
        }
    }
}

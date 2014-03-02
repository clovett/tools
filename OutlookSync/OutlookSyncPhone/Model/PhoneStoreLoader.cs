using OutlookSync;
using OutlookSync.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Phone.PersonalInformation;
using Appointment = Microsoft.Phone.UserData.Appointment;
using Appointments = Microsoft.Phone.UserData.Appointments;
using AppointmentsSearchEventArgs = Microsoft.Phone.UserData.AppointmentsSearchEventArgs;

namespace OutlookSyncPhone
{
    enum MergeResult
    {
        None,
        DeletedLocally,
        NewEntry,
        Merged,
        ParseError
    };

    class PhoneStoreLoader
    {
        UnifiedStore cache;
        ContactStore store;
        bool isBlueOs;
        SyncMessage syncReport;


        // this is the list of merged items, if a item exists locally that is not in this hashset then
        // it means the item was deleted from outlook.
        HashSet<SyncItem> merged = new HashSet<SyncItem>();

        // items to send back to the server
        HashSet<SyncItem> toSend = new HashSet<SyncItem>();

        // items to pull from the server
        HashSet<SyncItem> toUpdate = new HashSet<SyncItem>();


        // this is the list of new contact we found in the local store.  
        Dictionary<string, UnifiedContact> locallyAddedContacts = new Dictionary<string, UnifiedContact>();

        /// <summary>
        /// Contacts that were deleted on the phone.
        /// </summary>
        Dictionary<string, UnifiedContact> locallyDeletedContacts = new Dictionary<string, UnifiedContact>();


        // this is the list of new appointments we found in the local store.  
        Dictionary<string, UnifiedAppointment> locallyAddedAppointments = new Dictionary<string, UnifiedAppointment>();

        /// <summary>
        /// Appointments that were deleted on the phone.
        /// </summary>
        Dictionary<string, UnifiedAppointment> locallyDeletedAppointments = new Dictionary<string, UnifiedAppointment>();


        public PhoneStoreLoader()
        {
            Version version = Environment.OSVersion.Version;
            isBlueOs = (version.Major > 8) || (version.Major == 8 && version.Minor >= 10);
        }


        internal ContactStore ContactStore { get { return this.store; } }

        internal UnifiedStore UnifiedStore { get { return this.cache; } }

        internal async Task Save()
        {
            await cache.SaveAsync("store.xml");
        }

        internal void StartMerge()
        {
            merged = new HashSet<SyncItem>();
            toSend = new HashSet<SyncItem>();
            toUpdate = new HashSet<SyncItem>();
        }

        internal SyncItem GetNextItemToUpdate()
        {
            SyncItem item= null;
            while (toUpdate.Count > 0 && item == null)
            {
                item = toUpdate.FirstOrDefault();
                toUpdate.Remove(item);
            }
            return item;
        }

        internal SyncItem GetNextItemToSend()
        {
            SyncItem result = null;
            while (toSend.Count > 0 && result == null)
            {
                result = toSend.FirstOrDefault();
                toSend.Remove(result);
            }
            return result;
        }

        internal SyncMessage GetSyncMessage()
        {
            SyncMessage message = new SyncMessage();
            foreach (var pair in locallyDeletedContacts)
            {
                message.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Delete });
            }

            foreach (var pair in locallyAddedContacts)
            {
                message.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Insert });
            }

            foreach (var contact in cache.Contacts)
            {
                if (!locallyAddedContacts.ContainsKey(contact.OutlookEntryId))
                {
                    message.Items.Add(new SyncItem(contact) { Change = ChangeType.Update });
                }
            }


            foreach (var pair in locallyDeletedAppointments)
            {
                string id = pair.Key;
                message.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Delete });
            }

            foreach (var pair in locallyAddedAppointments)
            {
                message.Items.Add(new SyncItem(pair.Value) { Change = ChangeType.Insert });
            }

            foreach (var appointment in cache.Appointments)
            {
                if (!locallyAddedAppointments.ContainsKey(appointment.PhoneId))
                {
                    message.Items.Add(new SyncItem(appointment) { Change = ChangeType.Update });
                }
            }

            syncReport = message;
            return message;
        }

        public async Task LoadContacts()
        {
            // load our cached state
            cache = await UnifiedStore.LoadAsync("store.xml");

            // open our app's phone contact store.
            store = await ContactStore.CreateOrOpenAsync(ContactStoreSystemAccessMode.ReadWrite, ContactStoreApplicationAccessMode.ReadOnly);

            HashSet<string> found = new HashSet<string>();

            // find out what has changed in the store compared to what is in our cache.
            ContactQueryResult result = store.CreateContactQuery();
            uint count = await result.GetContactCountAsync();
            foreach (StoredContact contact in await result.GetContactsAsync())
            {
                found.Add(contact.RemoteId);
                await UpdateContact(contact, found);
            }

            // see if user has deleted contacts on the phone (we need to remember this)
            foreach (UnifiedContact c in cache.Contacts.ToArray())
            {
                string id = c.OutlookEntryId;
                if (string.IsNullOrEmpty(id) || !found.Contains(id))
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        locallyDeletedContacts[id] = c;
                    }
                    cache.Contacts.Remove(c);
                }
            }
        }

        /// <summary>
        /// Load all appointments from now out the given number of months into the future.
        /// </summary>
        /// <param name="months">The numberof months to load (default is 3)</param>
        public async Task LoadAppointments(int months = 3)
        {
            if (cache == null || store == null)
            {
                throw new Exception("Please call LoadContacts first");
            }

            int startTime = Environment.TickCount;
            
            Appointments appts = new Appointments();

            //Identify the method that runs after the asynchronous search completes.
            appts.SearchCompleted += new EventHandler<AppointmentsSearchEventArgs>(OnAppointmentsSearchCompleted);

            DateTime start = DateTime.Now;
            // todo: let user configure how far out we go here...
            DateTime end = start.AddMonths(months);

            //Start the asynchronous search.
            appointmentSearchComplete.Reset();

            var account = (from a in appts.Accounts where a.Kind == Microsoft.Phone.UserData.StorageKind.Outlook select a).FirstOrDefault();
            if (account != null)
            {
                appts.SearchAsync(start, end, account, "outlook appointment search");

                await Task.Run(new Action(() =>
                {
                    appointmentSearchComplete.WaitOne();
                }));

            }
            int endTime = Environment.TickCount;

            int ms = endTime - startTime;
            Debug.WriteLine("Loaded {0} appointments in {1} milliseconds", this.cache.Appointments.Count, ms);
        }

        ManualResetEvent appointmentSearchComplete = new ManualResetEvent(false);

        private void OnAppointmentsSearchCompleted(object sender, AppointmentsSearchEventArgs e)
        {           
            HashSet<string> found = new HashSet<string>();

            foreach (Appointment appointment in e.Results)
            {
                UnifiedAppointment ua = UpdateAppointment(appointment);
                found.Add(ua.PhoneId);
            }

            // see if user has deleted contacts on the phone (we need to remember this)
            foreach (UnifiedAppointment a in cache.Appointments.ToArray())
            {
                string id = a.PhoneId;
                if (string.IsNullOrEmpty(id) || !found.Contains(id))
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        locallyDeletedAppointments[id] = a;
                    }
                    cache.Appointments.Remove(a);
                }
            }

            appointmentSearchComplete.Set();
        }

        private UnifiedAppointment UpdateAppointment(Appointment appointment)
        {
            string phoneId = appointment.GetHashCode().ToString();
            UnifiedAppointment ua = cache.FindAppointmentByPhoneId(phoneId);
            if (ua == null)
            {
                // user added an appointment on the phone.
                ua = new UnifiedAppointment();
                ua.PhoneId = phoneId;
                locallyAddedAppointments[phoneId] = ua;
                cache.Appointments.Add(ua);
            }

            ua.LocalStoreObject = appointment;
            UpdateFromStore(ua, appointment);
            return ua;
        }

        internal async Task DeleteContact(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                UnifiedContact uc = cache.FindContactById(id);
                if (uc != null)
                {
                    cache.Contacts.Remove(uc);
                }

                var sc = await store.FindContactByRemoteIdAsync(id);
                if (sc != null)
                {
                    await store.DeleteContactAsync(sc.Id);
                }
            }
        }

        private async Task UpdateContact(StoredContact contact, HashSet<string> found)
        {
            string id = contact.RemoteId;
            if (string.IsNullOrEmpty(id))
            {
                // user added this one manually?  Then we need to invent an OutlookId until we add this to outlook.
                id = Guid.NewGuid().ToString();
                found.Add(id);
            }

            UnifiedContact uc = cache.FindContactById(id);
            if (uc == null)
            {
                // user added a contact manually.
                uc = new UnifiedContact();
                uc.OutlookEntryId = id;
                locallyAddedContacts[id] = uc;
                cache.Contacts.Add(uc);
            }
            
            uc.LocalStoreObject = contact;

            await UpdateFromStore(uc, contact);
        }

        internal async Task UpdateContactRemoteId(string parameter)
        {
            int i = parameter.IndexOf("=>");
            if (i > 0)
            {
                string oldId = parameter.Substring(0, i);
                string newId = parameter.Substring(i + 2);

                UnifiedContact cached = cache.FindContactById(oldId);
                if (cached != null)
                {
                    cached.OutlookEntryId = newId;
                    // re-index it.
                    cache.Contacts.Remove(cached);
                    cache.Contacts.Add(cached);
                    // we'll save the UnifiedStore again when all contacts have finished updating.
                }

                StoredContact sc = null;
                UnifiedContact added = null;
                locallyAddedContacts.TryGetValue(oldId, out added);
                if (added == null)
                {
                    sc = await store.FindContactByRemoteIdAsync(oldId);
                }
                else
                {
                    sc = added.LocalStoreObject as StoredContact;
                }
                if (sc != null)
                {
                    // Have to create a new contact to update the remote id and replace the old one
                    sc.RemoteId = newId;
                    await sc.SaveAsync();
                }
            }
        }

        public void UpdateAppointmentOutlookId(string parameter)
        {
            int i = parameter.IndexOf("=>");
            if (i > 0)
            {
                string phoneId = parameter.Substring(0, i);
                string outlookId = parameter.Substring(i + 2);

                UnifiedAppointment  cached = cache.FindAppointmentByPhoneId(phoneId);
                if (cached != null)
                {
                    cached.Id = outlookId;
                    // re-index it.
                    cache.Appointments.Remove(cached);
                    cache.Appointments.Add(cached);
                    // we'll save the UnifiedStore again when all contacts have finished updating.
                }

                // Appointments don't store remote id's, so nothing else to do here.
            }
        }

        /// <summary>
        /// Merge the given contact from Outlook
        /// </summary>
        /// <param name="xml">The serialized contact</param>        
        public async Task<Tuple<MergeResult,UnifiedContact>> MergeContact(string xml)
        {
            if (xml == "null" || string.IsNullOrEmpty(xml))
            {
                return new Tuple<MergeResult, UnifiedContact>(MergeResult.None, null);
            }

            bool changedLocally = false;

            // parse the XML into one of our unified store contacts and save it in our ContactStore.
            UnifiedContact contact = UnifiedContact.Parse(xml);
            if (contact == null)
            {
                // error handling.
                return new Tuple<MergeResult, UnifiedContact>(MergeResult.ParseError, null);
            }

            // compare this new contact from outlook with our existing one
            UnifiedContact cached = cache.FindContactById(contact.OutlookEntryId);            
            if (cached != null)
            {
                changedLocally |= cached.Merge(contact);
            }
            else
            {
                // new contact from outlook, unless it was deleted locally.
                if (locallyDeletedContacts.ContainsKey(contact.OutlookEntryId))
                {
                    return new Tuple<MergeResult, UnifiedContact>(MergeResult.DeletedLocally, cached);
                }
                cached = contact;
                cache.Contacts.Add(cached);
                cached.VersionNumber = cached.GetHighestVersionNumber();
            }

            // remember that we've seen this one so we can identify deleted contacts later
            merged.Add(new SyncItem(cached));

            // Now see if user has made any changes on the phone using People Hub

            var rc = MergeResult.Merged;

            StoredContact sc = await store.FindContactByRemoteIdAsync(contact.OutlookEntryId);
            if (sc != null)
            {
                // merge
            }
            else
            {
                rc = MergeResult.NewEntry;
                sc = new StoredContact(store);
                sc.RemoteId = contact.OutlookEntryId;
            }
            try
            {
                await UpdateFromCache(cached, sc);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception updating from cache: " + ex.Message);
            }

            await sc.SaveAsync();

            if (changedLocally)
            {
                // we should pick this up later when we do th sync message
                // toSend.Add(cached);
            }

            return new Tuple<MergeResult, UnifiedContact>(rc, cached);
        }


        /// <summary>
        /// Merge the given Appointment from Outlook
        /// </summary>
        /// <param name="xml">The serialized Appointment</param>        
        public async Task<Tuple<MergeResult, UnifiedAppointment>> MergeAppointment(string xml)
        {
            if (xml == "null")
            {
                return new Tuple<MergeResult, UnifiedAppointment>(MergeResult.None, null);
            }

            bool changedLocally = false;

            // parse the XML into one of our unified store contacts and save it in our ContactStore.
            UnifiedAppointment appointment = UnifiedAppointment.Parse(xml);
            if (appointment == null)
            {
                // error handling.
                return new Tuple<MergeResult, UnifiedAppointment>(MergeResult.ParseError, null);
            }

            // compare this new contact from outlook with our existing one
            UnifiedAppointment cached = null;
            if (!string.IsNullOrEmpty(appointment.PhoneId))
            {
                cached = cache.FindAppointmentByPhoneId(appointment.PhoneId);
            }
            else if (!string.IsNullOrEmpty(appointment.Id))
            {
                cached = cache.FindAppointmentById(appointment.Id);
            }
            if (cached != null)
            {
                changedLocally |= cached.Merge(appointment);
            }
            else
            {
                // new contact from outlook, unless it was deleted locally.
                if (locallyDeletedContacts.ContainsKey(appointment.Id))
                {
                    return new Tuple<MergeResult, UnifiedAppointment>(MergeResult.DeletedLocally, cached);
                }
                cached = appointment;
                cache.Appointments.Add(cached);
                cached.VersionNumber = cached.GetHighestVersionNumber();
            }

            // remember that we've seen this one so we can identify deleted contacts later
            merged.Add(new SyncItem(appointment));

            // Now see if user has made any changes on the phone using People Hub
            var rc = MergeResult.Merged;

            cached.ClearValue("Details"); // don't persist this field.

            Appointment sc = cached.LocalStoreObject as Appointment;
            if (sc != null)
            {
                // merge
            }
            else
            {
                // no way to add a new appointment, FUCK !!!!!
            }
            try
            {
                // the Appointment class is read only, FUCK !!!!!
                //await UpdateFromCache(cached, sc);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception updating from cache: " + ex.Message);
            }

            // shit no way to save appointments, fuck!!!!!
            //await sc.SaveAsync();

            if (changedLocally)
            {
                // we should pick this up later when we do th sync message
                // toSend.Add(cached);
            }

            return new Tuple<MergeResult, UnifiedAppointment>(rc, cached);
        }


        private void UpdateFromStore(UnifiedAppointment cache, Appointment appointment)
        {
            cache.Subject = appointment.Subject;
            // Note: the cache stores DateTimeOffset which picks up the local time zone from the phone
            // which we need for serialization across to the PC.
            cache.Start = appointment.StartTime;
            cache.End = appointment.EndTime;
            cache.Hash = appointment.GetHashCode().ToString();

            HashSet<string> foundAttendees = new HashSet<string>();
            if (appointment.Attendees != null) 
            {
                // make sure we have all the attendees in our cache that are in the phone store.
                foreach (var a in appointment.Attendees)
                {
                    cache.AddAttendee(a.DisplayName, a.EmailAddress);
                    foundAttendees.Add(a.EmailAddress);
                }
            }
            
            if (cache.Attendees != null)
            {
                // remove the attendies in our cache that no longer exist in the phone store.
                foreach (UnifiedAttendee a in cache.Attendees.ToArray())
                {
                    if (!foundAttendees.Contains(a.Email))
                    {
                        cache.RemoveAttendee(a.Email);
                    }
                }
            }

            cache.IsAllDayEvent = appointment.IsAllDayEvent;
            cache.IsPrivate = appointment.IsPrivate;
            cache.Location = appointment.Location;

            var organizer = appointment.Organizer;
            if (organizer == null)
            {
                cache.Organizer = null;
            }
            else
            {
                var cachedOrganizer = cache.Organizer;
                if (cachedOrganizer == null)
                {
                    cache.Organizer = cachedOrganizer = new UnifiedAttendee();
                }
                cachedOrganizer.Name = organizer.DisplayName;
                cachedOrganizer.Email = organizer.EmailAddress;
                cachedOrganizer.Deleted = false;
            }

            // enums are compatible, just cast the integer value over.
            cache.Status = (AppointmentStatus)appointment.Status;


            cache.VersionNumber = cache.GetHighestVersionNumber();
        }


        private async Task UpdateFromStore(UnifiedContact cache, StoredContact contact)
        {
            cache.DisplayName = contact.DisplayName;
            PersonName name = cache.CompleteName;
            if (name == null)
            {
                name = new PersonName();
                cache.CompleteName = name;
            }
            name.FirstName = contact.GivenName;
            name.LastName = contact.FamilyName;
            name.Title = contact.HonorificPrefix;
            name.Suffix = contact.HonorificSuffix;

            // merge extended properties.
            var dictionary = await contact.GetPropertiesAsync();
            if (dictionary.ContainsKey(KnownContactProperties.Birthdate))
            {
                DateTimeOffset dt = (DateTimeOffset)dictionary[KnownContactProperties.Birthdate];

                // bugbug: there is a bug in how the phone handles the time zone, they are 
                // adding it the wrong way, which results date being off by (2*timezone).
                // This bug is not fixed in blue.
                dt -= dt.Offset;
                dt -= dt.Offset;

                cache.Birthday = dt;
            }
            else
            {
                cache.Birthday = null;
            }
            if (dictionary.ContainsKey(KnownContactProperties.SignificantOther))
            {
                cache.SignificantOthers = (string)dictionary[KnownContactProperties.SignificantOther];
            }
            else
            {
                cache.SignificantOthers = null;
            }
            if (dictionary.ContainsKey(KnownContactProperties.Children))
            {
                cache.Children = (string)dictionary[KnownContactProperties.Children];
            }
            else
            {
                cache.Children = null;
            }
            if (dictionary.ContainsKey(KnownContactProperties.Nickname))
            {
                cache.Nickname = (string)dictionary[KnownContactProperties.Nickname];
            }
            else
            {
                cache.Nickname = null;
            }

            UpdateAddressFromStore(KnownContactProperties.WorkAddress, AddressKind.Work, dictionary, cache);
            UpdateAddressFromStore(KnownContactProperties.Address, AddressKind.Home, dictionary, cache);
            UpdateAddressFromStore(KnownContactProperties.OtherAddress, AddressKind.Other, dictionary, cache);

            UpdateEmailAddressFromStore(KnownContactProperties.WorkEmail, EmailAddressKind.Work, dictionary, cache);
            UpdateEmailAddressFromStore(KnownContactProperties.Email, EmailAddressKind.Personal, dictionary, cache);
            UpdateEmailAddressFromStore(KnownContactProperties.OtherEmail, EmailAddressKind.Other, dictionary, cache);

            // phone numbers
            UpdatePhoneNumberFromStore(KnownContactProperties.Telephone, PhoneNumberKind.Home, dictionary, cache);
            UpdatePhoneNumberFromStore(KnownContactProperties.CompanyTelephone, PhoneNumberKind.Company, dictionary, cache);
            UpdatePhoneNumberFromStore(KnownContactProperties.HomeFax, PhoneNumberKind.HomeFax, dictionary, cache);
            UpdatePhoneNumberFromStore(KnownContactProperties.MobileTelephone, PhoneNumberKind.Mobile, dictionary, cache);
            UpdatePhoneNumberFromStore(KnownContactProperties.WorkTelephone, PhoneNumberKind.Work, dictionary, cache);
            UpdatePhoneNumberFromStore(KnownContactProperties.WorkFax, PhoneNumberKind.WorkFax, dictionary, cache);

            UpdateWebsitesFromStore(cache, dictionary);

            cache.VersionNumber = cache.GetHighestVersionNumber();
        }


        private async Task UpdateFromCache(UnifiedContact cache, StoredContact contact)
        {
            contact.DisplayName = "" + cache.DisplayName;

            var fullName = cache.CompleteName;
            if (fullName != null)
            {
                contact.FamilyName = "" + fullName.LastName;
                contact.GivenName = "" + fullName.FirstName;
                contact.HonorificPrefix = "" + fullName.Title;
                contact.HonorificSuffix = "" + fullName.Suffix;
            }
            else
            {
                contact.FamilyName = "";
                contact.GivenName = "";
                contact.HonorificPrefix = "";
                contact.HonorificSuffix = "";
            }

            var dictionary = await contact.GetPropertiesAsync();
            if (cache.Birthday.HasValue)
            {
                dictionary[KnownContactProperties.Birthdate] = cache.Birthday.Value;
            }
            else if (dictionary.ContainsKey(KnownContactProperties.Birthdate))
            {
                dictionary.Remove(KnownContactProperties.Birthdate);
            }

            UpdateDictionary(KnownContactProperties.SignificantOther, cache.SignificantOthers, dictionary);
            UpdateDictionary(KnownContactProperties.Children, cache.Children, dictionary);
            UpdateDictionary(KnownContactProperties.Nickname, cache.Nickname, dictionary);

            // addresses
            UpdateAddressFromCache(KnownContactProperties.WorkAddress, AddressKind.Work, cache, dictionary);
            UpdateAddressFromCache(KnownContactProperties.Address, AddressKind.Home, cache, dictionary);
            UpdateAddressFromCache(KnownContactProperties.OtherAddress, AddressKind.Other, cache, dictionary);

            // email addresses
            UpdateEmailAddressFromCache(KnownContactProperties.WorkEmail, EmailAddressKind.Work, cache, dictionary);
            UpdateEmailAddressFromCache(KnownContactProperties.Email, EmailAddressKind.Personal, cache, dictionary);
            UpdateEmailAddressFromCache(KnownContactProperties.OtherEmail, EmailAddressKind.Other, cache, dictionary);

            // phone numbers
            UpdatePhoneNumberFromCache(KnownContactProperties.Telephone, PhoneNumberKind.Home, cache, dictionary);
            UpdatePhoneNumberFromCache(KnownContactProperties.CompanyTelephone, PhoneNumberKind.Company, cache, dictionary);
            UpdatePhoneNumberFromCache(KnownContactProperties.HomeFax, PhoneNumberKind.HomeFax, cache, dictionary);
            UpdatePhoneNumberFromCache(KnownContactProperties.MobileTelephone, PhoneNumberKind.Mobile, cache, dictionary);
            UpdatePhoneNumberFromCache(KnownContactProperties.WorkTelephone, PhoneNumberKind.Work, cache, dictionary);
            UpdatePhoneNumberFromCache(KnownContactProperties.WorkFax, PhoneNumberKind.WorkFax, cache, dictionary);

            // websites
            UpdateWebsitesFromCache(cache, dictionary);
        }

        private void UpdateDictionary(string key, string newValue, IDictionary<string, object> dictionary)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary.Remove(key);
                }
            }
            else
            {
                dictionary[KnownContactProperties.SignificantOther] = newValue;
            }
        }

        private void UpdateAddressFromCache(string key, AddressKind kind, UnifiedContact contact, IDictionary<string, object> dictionary)
        {

            if (contact.Addresses != null)
            {
                var address = (from a in contact.Addresses where a.Kind == kind select a).FirstOrDefault();
                if (address != null && address.PhysicalAddress != null)
                {
                    var pa = address.PhysicalAddress;

                    Windows.Phone.PersonalInformation.ContactAddress ca = new Windows.Phone.PersonalInformation.ContactAddress();
                    ca.Country = "" + pa.CountryRegion;
                    ca.PostalCode = "" + pa.PostalCode;
                    ca.StreetAddress = "" + pa.AddressLine1;
                    if (!string.IsNullOrEmpty(pa.AddressLine2))
                    {
                        if (!string.IsNullOrEmpty(ca.StreetAddress))
                        {
                            ca.StreetAddress += ", ";
                        }
                        ca.StreetAddress += pa.AddressLine2;
                    }

                    ca.Region = "" + pa.StateProvince;
                    ca.Locality = "" + pa.City;

                    dictionary[key] = ca;
                    return;
                }
            }

            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }

        private void UpdateAddressFromStore(string key, AddressKind kind, IDictionary<string, object> dictionary, UnifiedContact contact)
        {
            if (dictionary.ContainsKey(key))
            {
                Windows.Phone.PersonalInformation.ContactAddress ca = (Windows.Phone.PersonalInformation.ContactAddress)dictionary[key];

                var address = contact.GetAddress(kind);
                if (address == null)
                {
                    address = new OutlookSync.Model.ContactAddress() { Kind = kind, VersionNumber = UnifiedStore.SyncTime };
                    contact.AddAddress(address);
                }
                if (address.PhysicalAddress == null)
                {
                    address.PhysicalAddress = new PhysicalAddress() { VersionNumber = UnifiedStore.SyncTime };
                }

                var pa = address.PhysicalAddress;
                pa.CountryRegion = ca.Country;
                pa.PostalCode = ca.PostalCode;
                pa.AddressLine1 = ca.StreetAddress;
                pa.StateProvince = ca.Region;
                pa.City = ca.Locality;
            }
            else
            {
                contact.RemoveAddress(kind);
            }

        }

        private void UpdateEmailAddressFromCache(string key, EmailAddressKind kind, UnifiedContact contact, IDictionary<string, object> dictionary)
        {
            if (contact.EmailAddresses != null)
            {
                ContactEmailAddress email = contact.GetEmailAddress(kind);
                if (email != null && !string.IsNullOrEmpty(email.EmailAddress))
                {
                    dictionary[key] = email.EmailAddress;
                    return;
                }
            }

            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }

        private void UpdateEmailAddressFromStore(string key, EmailAddressKind kind, IDictionary<string, object> dictionary, UnifiedContact contact)
        {
            if (dictionary.ContainsKey(key))
            {
                string value = (string)dictionary[key];
                if (!string.IsNullOrEmpty(value))
                {
                    ContactEmailAddress email = contact.GetEmailAddress(kind);
                    if (email == null)
                    {
                        email = new ContactEmailAddress() { Kind = kind };
                        contact.AddEmailAddress(email);
                    }
                    email.EmailAddress = value;
                    return;
                }
            }
            contact.RemoveEmailAddress(kind);
        }

        private void UpdatePhoneNumberFromCache(string key, PhoneNumberKind kind, UnifiedContact contact, IDictionary<string, object> dictionary)
        {
            if (contact.PhoneNumbers != null)
            {
                ContactPhoneNumber phone = contact.GetPhoneNumber(kind);
                if (phone != null && !string.IsNullOrEmpty(phone.PhoneNumber))
                {
                    dictionary[key] = phone.PhoneNumber;
                    return;
                }
            }
            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }

        private void UpdatePhoneNumberFromStore(string key, PhoneNumberKind kind, IDictionary<string, object> dictionary, UnifiedContact contact)
        {
            if (dictionary.ContainsKey(key))
            {
                string value = (string)dictionary[key];
                if (!string.IsNullOrEmpty(value))
                {
                    ContactPhoneNumber phone = contact.GetPhoneNumber(kind);
                    if (phone == null)
                    {
                        phone = new ContactPhoneNumber() { Kind = kind, VersionNumber = UnifiedStore.SyncTime };
                        contact.AddPhoneNumber(phone);
                    }
                    phone.PhoneNumber = value;
                    return;
                }
            }
            else
            {
                contact.RemovePhoneNumber(kind);
            }
        }

        private static void UpdateWebsitesFromCache(UnifiedContact cache, IDictionary<string, object> dictionary)
        {
            if (cache.Websites != null && cache.Websites.Count > 0)
            {
                dictionary[KnownContactProperties.Url] = string.Join(";", cache.Websites);
            }
            else if (dictionary.ContainsKey(KnownContactProperties.Url))
            {
                dictionary.Remove(KnownContactProperties.Url);
            }
        }

        private static void UpdateWebsitesFromStore(UnifiedContact cache, IDictionary<string, object> dictionary)
        {
            if (dictionary.ContainsKey(KnownContactProperties.Url))
            {
                string urls = (string)dictionary[KnownContactProperties.Url];
                string[] list = urls.Split(';');
                HashSet<string> storeSet = new HashSet<string>(list);
                HashSet<string> cacheSet = new HashSet<string>(cache.Websites == null ? (IEnumerable<string>)new string[0] : (IEnumerable<string>)cache.Websites);
                if (!cacheSet.SetEquals(storeSet))
                {
                    // then update the property version number.
                    cache.SetWebSites(list);
                }
            }
            else
            {
                cache.Websites = null;
            }
        }

        public async Task HandleServerSync(string xml, SyncResult status)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return;
            }

            SyncMessage server = SyncMessage.Parse(xml);

            SyncMessage phone = this.syncReport;

            // create index of our local items.
            Dictionary<string, SyncItem> map = new Dictionary<string, SyncItem>();
            foreach (var item in phone.Items)
            {
                string id = item.PhoneId == null ? item.Id : item.PhoneId;
                map[id] = item;
            }

            Dictionary<string, SyncItem> serverMap = new Dictionary<string, SyncItem>();

            // Ok, now compare version numbers to see what has changed
            foreach (var item in server.Items)
            {
                string id = item.PhoneId == null ? item.Id : item.PhoneId;

                if (item.Change == ChangeType.Delete)
                {
                    // deleted on the server
                    status.ServerDeleted.Add(item);
                    if (item.Type == "C")
                    {
                        await this.DeleteContact(id);
                    }
                    else
                    {
                        await this.DeleteAppointment(item.PhoneId);
                    }
                }
                else if (item.Change == ChangeType.Insert)
                {
                    // remember to ask for this one.
                    // NOTE: delay updating this until item is actually received oso user sees the progress
                    // status.ServerInserted.Add(contact);
                    status.TotalChanges++;
                    toUpdate.Add(item);
                }
                else
                {
                    serverMap[id] = item;

                    SyncItem phoneVersion = null;
                    if (map.TryGetValue(id, out phoneVersion))
                    {
                        if (phoneVersion.Change == ChangeType.Delete)
                        {
                            // let it stay deleted
                        }
                        else if (phoneVersion.VersionNumber > item.VersionNumber)
                        {
                            // remember to send this one
                            // NOTE: delay updating this until item is actually received oso user sees the progress
                            // status.PhoneUpdated.Add(contact);
                            AssertStoreObject(item);
                            toSend.Add(item);
                        }
                        else if (item.VersionNumber > phoneVersion.VersionNumber)
                        {
                            // remember to pull this one.
                            // NOTE: delay updating this until item is actually received oso user sees the progress
                            //status.ServerUpdated.Add(contact);
                            status.TotalChanges++;
                            toUpdate.Add(item);
                        }
                        else
                        {
                            // they are the same! yay, this saves time...                            
                        }
                    }
                    else
                    {
                        // hmmm, then phone doesn't have this one after all
                        // perhaps phone sync app was uninstalled and reinstalled.
                        // NOTE: delay updating this until item is actually received oso user sees the progress
                        // status.ServerInserted.Add(contact);
                        status.TotalChanges++;
                        toUpdate.Add(item);
                    }
                }
            }


            // check for new contacts on the phone
            foreach (var item in phone.Items)
            {
                string id = item.PhoneId == null ? item.Id : item.PhoneId;
                if (!serverMap.ContainsKey(id) && !locallyDeletedContacts.ContainsKey(id) && !locallyDeletedAppointments.ContainsKey(id))
                {
                    // remember to send this one
                    AssertStoreObject(item);
                    toSend.Add(item);
                }
            }
        }

        private void AssertStoreObject(SyncItem item)
        {
            if (item.Type == "A")
            {
                UnifiedAppointment a = null;
                if (!locallyAddedAppointments.TryGetValue(item.PhoneId, out a))
                {
                    a = cache.FindAppointmentByPhoneId(item.PhoneId);
                }
                if (a == null || a.LocalStoreObject == null)
                {
                    Debug.Assert(a != null && a.LocalStoreObject != null, "Missing local store object");
                }
            }
            else
            {
                UnifiedContact c = null;
                if (!locallyAddedContacts.TryGetValue(item.Id, out c))
                {
                    c =  cache.FindContactById(item.Id);
                }
                if (c == null || c.LocalStoreObject == null)
                {
                    Debug.Assert(c != null && c.LocalStoreObject != null, "Missing local store object");
                }
            }

        }


        private Task DeleteAppointment(string phoneId)
        {
            // bugbug: Phone doesn't give us an API for this...
            throw new NotImplementedException();
        }


        internal SyncResult GetLocalSyncResult()
        {
            var syncReport = this.GetSyncMessage();
            return new SyncResult(syncReport, true);
        }
        
        /// <summary>
        /// The Sync XML contains the body of the appointment
        /// </summary>
        /// <returns></returns>
        internal string GetSyncXml(UnifiedAppointment ua)
        {
            string result = null;
            Appointment appointment = ua.LocalStoreObject as Appointment;
            if (appointment != null)
            {
                ua.Details = appointment.Details;
                result = ua.ToXml();
                ua.ClearValue("Details"); // no version number updates.
                return result;
            }
            throw new Exception("Why is Appointment object missing?");
        }
    }

}

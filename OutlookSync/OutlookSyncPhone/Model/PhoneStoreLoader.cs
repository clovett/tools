using OutlookSync;
using OutlookSync.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Phone.PersonalInformation;

namespace OutlookSyncPhone
{
    enum MergeResult
    {
        None,
        DeletedLocally,
        NewContact,
        Merged,
        ParseError
    };

    class PhoneStoreLoader
    {
        UnifiedStore cache;
        ContactStore store;
        bool isBlueOs;
        SyncMessage syncReport;


        // this is the list of merged contacts, if a contact exists locally that is not in this hashset then
        // it means the contact was deleted from outlook.
        HashSet<string> merged = new HashSet<string>();

        // contacts to send back to the server
        HashSet<string> toSend = new HashSet<string>();

        // contacts to pull from the server
        HashSet<string> toUpdate = new HashSet<string>();


        // this is the list of new contact we found in the local store.  We have to maintain this because
        // when we update the RemoteId, the stupid FindContactByRemoteIdAsync method fails to find it.
        Dictionary<string, StoredContact> addedLocally = new Dictionary<string, StoredContact>();

        /// <summary>
        /// Contacts that were deleted on the phone.
        /// </summary>
        Dictionary<string, UnifiedContact> deletedLocally = new Dictionary<string, UnifiedContact>();

        public PhoneStoreLoader()
        {
            Version version = Environment.OSVersion.Version;
            isBlueOs = (version.Major > 8) || (version.Major == 8 && version.Minor >= 10);
        }


        internal ContactStore ContactStore { get { return this.store; } }

        internal UnifiedStore UnifiedStore { get { return this.cache; } }

        internal void StartMerge()
        {
            merged = new HashSet<string>();
            toSend = new HashSet<string>();
            toUpdate = new HashSet<string>();
        }

        internal string GetNextContactToUpdate()
        {
            string id = null;
            while (toUpdate.Count > 0 && id == null)
            {
                id = toUpdate.FirstOrDefault();
                toUpdate.Remove(id);
            }
            return id;
        }

        internal UnifiedContact GetNextContactToSend()
        {
            UnifiedContact uc = null;
            while (toSend.Count > 0 && uc == null)
            {
                string id = toSend.FirstOrDefault();
                uc = cache.FindOutlookEntry(id);
                toSend.Remove(id);
            }
            return uc;
        }

        internal SyncMessage GetSyncMessage()
        {
            SyncMessage message = new SyncMessage();
            foreach (var pair in deletedLocally)
            {
                string id = pair.Key;
                message.Contacts.Add(new ContactVersion() { Id = id, Deleted = true, Name = pair.Value.DisplayName });
            }

            foreach (var pair in addedLocally)
            {
                message.Contacts.Add(new ContactVersion() { Id = pair.Key, Inserted = true, Name = pair.Value.DisplayName });
            }

            foreach (var contact in cache.Contacts)
            {
                if (!addedLocally.ContainsKey(contact.OutlookEntryId))
                {
                    message.Contacts.Add(new ContactVersion() { Id = contact.OutlookEntryId, VersionNumber = contact.GetHighestVersionNumber(), Name = contact.DisplayName });
                }
            }

            syncReport = message;
            return message;
        }

        public async Task Open()
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
                if (string.IsNullOrEmpty(c.OutlookEntryId) || !found.Contains(c.OutlookEntryId))
                {
                    if (!string.IsNullOrEmpty(c.OutlookEntryId))
                    {
                        deletedLocally[c.OutlookEntryId] = c;
                    }
                    cache.Contacts.Remove(c);
                }
            }
        }

        internal async Task Save()
        {
            await cache.SaveAsync("store.xml");
        }

        internal async Task ServerDelete(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                UnifiedContact uc = cache.FindOutlookEntry(id);
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
            string name = contact.DisplayName;

            string id = contact.RemoteId;
            if (string.IsNullOrEmpty(id))
            {
                // user added this one manually?  Then we need to invent an OutlookId until we add this to outlook.
                id = Guid.NewGuid().ToString();
                addedLocally[id] = contact;
                found.Add(id);
            }

            UnifiedContact uc = cache.FindOutlookEntry(id);
            if (uc == null)
            {
                // user added a contact manually.
                uc = new UnifiedContact();
                uc.OutlookEntryId = id;
                cache.Contacts.Add(uc);
            }
            await UpdateFromStore(uc, contact);
        }

        internal async Task UpdateRemoteId(string parameter)
        {
            int i = parameter.IndexOf("=>");
            if (i > 0)
            {
                string oldId = parameter.Substring(0, i);
                string newId = parameter.Substring(i + 2);

                UnifiedContact cached = cache.FindOutlookEntry(oldId);
                if (cached != null)
                {
                    cached.OutlookEntryId = newId;
                    // re-index it.
                    cache.Contacts.Remove(cached);
                    cache.Contacts.Add(cached);
                    // we'll save the UnifiedStore again when all contacts have finished updating.
                }

                StoredContact sc = null;
                addedLocally.TryGetValue(oldId, out sc);
                if (sc == null)
                {
                    sc = await store.FindContactByRemoteIdAsync(oldId);
                }
                if (sc != null)
                {
                    // Have to create a new contact to update the remote id and replace the old one
                    //StoredContact nsc = new StoredContact(store); 
                    //nsc.RemoteId = newId;
                    //await UpdateFromCache(cached, nsc);                    
                    //await nsc.ReplaceExistingContactAsync(sc.Id);
                    sc.RemoteId = newId;
                    await sc.SaveAsync();
                }
            }
        }

        /// <summary>
        /// Merge the given contact from Outlook
        /// </summary>
        /// <param name="xml">The serialized contact</param>        
        public async Task<Tuple<MergeResult,UnifiedContact>> MergeContact(string xml)
        {
            if (xml == "null")
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
            UnifiedContact cached = cache.FindOutlookEntry(contact.OutlookEntryId);            
            if (cached != null)
            {
                changedLocally |= cached.Merge(contact);
            }
            else
            {
                // new contact from outlook, unless it was deleted locally.
                if (deletedLocally.ContainsKey(contact.OutlookEntryId))
                {
                    return new Tuple<MergeResult, UnifiedContact>(MergeResult.DeletedLocally, cached);
                }
                cached = contact;
                cache.Contacts.Add(cached);
            }

            // remember that we've seen this one so we can identify deleted contacts later
            merged.Add(contact.OutlookEntryId);

            // Now see if user has made any changes on the phone using People Hub

            var rc = MergeResult.Merged;

            StoredContact sc = await store.FindContactByRemoteIdAsync(contact.OutlookEntryId);
            if (sc != null)
            {
                // merge
            }
            else
            {
                rc = MergeResult.NewContact;
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
                    address = new OutlookSync.Model.ContactAddress() { Kind = kind };
                    contact.AddAddress(address);
                }
                if (address.PhysicalAddress == null)
                {
                    address.PhysicalAddress = new PhysicalAddress();
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
                        phone = new ContactPhoneNumber() { Kind = kind };
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
            SyncMessage server = SyncMessage.Parse(xml);

            SyncMessage phone = this.syncReport;
            
            // create index of our contacts.
            Dictionary<string, ContactVersion> map = new Dictionary<string, ContactVersion>();
            foreach (var contact in phone.Contacts)
            {
                map[contact.Id] = contact;
            }

            Dictionary<string, ContactVersion> serverMap = new Dictionary<string, ContactVersion>();

            // Ok, now compare version numbers to see what has changed
            foreach (var contact in server.Contacts)
            {
                if (contact.Deleted)
                {
                    // deleted on the server
                    status.ServerDeleted.Add(contact);
                    await this.ServerDelete(contact.Id);
                }
                else if (contact.Inserted)
                {
                    // remember to ask for this one.
                    // NOTE: delay updating this until item is actually received oso user sees the progress
                    // status.ServerInserted.Add(contact);
                    status.TotalChanges++;
                    toSend.Add(contact.Id);
                }
                else
                {
                    serverMap[contact.Id] = contact;

                    ContactVersion phoneVersion = null;
                    if (map.TryGetValue(contact.Id, out phoneVersion))
                    {
                        if (phoneVersion.Deleted)
                        {
                            // let it stay deleted
                        }
                        else if (phoneVersion.VersionNumber > contact.VersionNumber)
                        {
                            // remember to send this one
                            // NOTE: delay updating this until item is actually received oso user sees the progress
                            // status.PhoneUpdated.Add(contact);
                            toSend.Add(contact.Id);
                        }
                        else if (contact.VersionNumber > phoneVersion.VersionNumber)
                        {
                            // remember to pull this one.
                            // NOTE: delay updating this until item is actually received oso user sees the progress
                            //status.ServerUpdated.Add(contact);
                            status.TotalChanges++;
                            toUpdate.Add(contact.Id);
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
                        toUpdate.Add(contact.Id);
                    }
                }
            }


            // check for new contacts on the phone
            foreach (var contact in phone.Contacts)
            {
                if (!serverMap.ContainsKey(contact.Id))
                {
                    // remember to send this one
                    toSend.Add(contact.Id);
                }
            }

        }


        internal SyncResult GetLocalSyncResult()
        {
            var syncReport = this.GetSyncMessage();
            return new SyncResult(syncReport, true);
        }
    }

}

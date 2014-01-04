using OutlookSync;
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

        // this is the list of merged contacts, if a contact exists locally that is not in this hashset then
        // it means the contact was deleted from outlook.
        HashSet<string> merged = new HashSet<string>();

        // contacts to send back to the server
        List<UnifiedContact> toUpdate = new List<UnifiedContact>();

        // this is the list of new contact we found in the local store.  We have to maintain this because
        // when we update the RemoteId, the stupid FindContactByRemoteIdAsync method fails to find it.
        Dictionary<string, StoredContact> fakeIds = new Dictionary<string, StoredContact>();

        /// <summary>
        /// Contacts that were deleted on the phone.
        /// </summary>
        HashSet<string> deletedLocally = new HashSet<string>();

        /// <summary>
        /// List of pending deletes to send to the server.
        /// </summary>
        List<UnifiedContact> pendingDeletes = new List<UnifiedContact>();

        public PhoneStoreLoader()
        {
        }

        internal void StartMerge()
        {
            merged = new HashSet<string>();
            toUpdate = new List<UnifiedContact>();
        }

        internal void FinishMerge()
        {
            toUpdate.AddRange(from contact in cache.Contacts where !merged.Contains(contact.OutlookEntryId) select contact);
        }

        internal UnifiedContact GetNextContactToSend()
        {
            if (toUpdate.Count > 0)
            {
                UnifiedContact uc = toUpdate[0];
                toUpdate.RemoveAt(0);
                return uc;
            }
            return null;
        }

        internal string GetNextContactToDelete()
        {
            while (pendingDeletes.Count > 0)
            {
                UnifiedContact uc = pendingDeletes[0];
                pendingDeletes.RemoveAt(0);
                if (!string.IsNullOrEmpty(uc.OutlookEntryId))
                {
                    return uc.OutlookEntryId;
                }
            }
            return null;
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
                        deletedLocally.Add(c.OutlookEntryId);
                        pendingDeletes.Add(c);
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
                fakeIds[id] = contact;
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
                fakeIds.TryGetValue(oldId, out sc); 
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
        public async Task<MergeResult> MergeContact(string xml)
        {
            bool changedLocally = false;

            // parse the XML into one of our unified store contacts and save it in our ContactStore.
            UnifiedContact contact = UnifiedContact.Parse(xml);
            if (contact == null)
            {
                // error handling.
                return MergeResult.ParseError;
            }

            if (contact.DisplayName == "Andrew Byrne")
            {
                Debugger.Break();
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
                if (deletedLocally.Contains(contact.OutlookEntryId))
                {
                    return MergeResult.DeletedLocally;
                }
                cached = contact;
                cache.Contacts.Add(cached);
            }

            // remember that we've seen this one so we can identify deleted contacts later
            merged.Add(contact.OutlookEntryId);

            // Now see if user has made any changes on the phone using People Hub

            var result = MergeResult.Merged;

            StoredContact sc = await store.FindContactByRemoteIdAsync(contact.OutlookEntryId);
            if (sc != null)
            {
                // merge
            }
            else
            {
                result = MergeResult.NewContact;
                sc = new StoredContact(store);
                sc.RemoteId = contact.OutlookEntryId;
            }

            await UpdateFromCache(cached, sc);

            await sc.SaveAsync();

            if (changedLocally)
            {
                toUpdate.Add(cached);
            }

            return result;
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
            contact.DisplayName = cache.DisplayName;

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
                    ca.Country = ""+ pa.CountryRegion;
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
                    address = new OutlookSync.ContactAddress() { Kind = kind };
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
    }
}

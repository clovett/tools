using OutlookSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;
using System.Diagnostics;

namespace OutlookSync
{
    class OutlookStoreLoader
    {
        UnifiedStore store;

        /// <summary>
        /// Update the unified store with whatever is currently in Outlook.
        /// </summary>
        /// <param name="store">The store to update</param>
        public async Task UpdateAsync(UnifiedStore store)
        {
            this.store = store;

            await Task.Run(new System.Action(() =>
            {
                var outlook = new Microsoft.Office.Interop.Outlook.Application();

                var mapi = outlook.GetNamespace("MAPI");

                FindContacts(mapi.Folders);

                Log.WriteLine("Loaded {0} contacts", store.Contacts.Count);
            }));
        }

        private void FindContacts(Folders folders)
        {
            if (folders != null)
            {
                foreach (MAPIFolder folder in folders)
                {                                        
                    if (folder.DefaultItemType == OlItemType.olContactItem)
                    {
                        string name = folder.Name;
                        if (name == "Suggested Contacts")
                        {
                            continue;
                        }
                        foreach (object item in folder.Items)
                        {
                            ContactItem contact = item as ContactItem;
                            if (contact != null)
                            {
                                MergeContact(contact);
                            }
                        }
                    }
                    FindContacts(folder.Folders);
                }
            }
        }

        private void MergeContact(ContactItem contact)
        {
            string id = contact.EntryID;
            UnifiedContact uc = store.FindOutlookEntry(id);
            bool isNew = false;
            if (uc == null)
            {
                isNew = true;
                uc = new UnifiedContact();
                uc.OutlookEntryId = id;         
            }

            UpdateContact(uc, contact);

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

                if (!string.IsNullOrEmpty(contact.FirstName) || !string.IsNullOrEmpty(contact.LastName) || !string.IsNullOrEmpty(contact.MiddleName))
                {
                    PersonName pn = new PersonName();
                    pn.FirstName = contact.FirstName;
                    pn.LastName = contact.LastName;
                    pn.MiddleName = contact.MiddleName;
                    pn.Suffix = contact.Suffix;
                    pn.Title = contact.Title;
                    uc.CompleteName = pn;
                }
                else
                {
                    uc.CompleteName = null;
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
            HashSet<string> set = CreateMinimalSet(uc.Websites == null ? new List<string>() : uc.Websites);
            if (!set.SetEquals(websites))
            {
                // update the property then.
                uc.Websites = new List<string>(websites);
            }
            else if (uc.Websites != null && uc.Websites.Count == 0)
            {
                uc.Websites = null;
            }
        }

        private void UpdatePhoneNumber(UnifiedContact uc, PhoneNumberKind phoneNumberKind, string number)
        {
            if (uc.PhoneNumbers == null)
            {
                uc.PhoneNumbers = new List<ContactPhoneNumber>();
            }
            ContactPhoneNumber ca = (from c in uc.PhoneNumbers where c.Kind == phoneNumberKind select c).FirstOrDefault();

            if (string.IsNullOrEmpty(number))
            {
                if (ca != null)
                {
                    uc.PhoneNumbers.Remove(ca);
                }
                return;
            }
            else if (ca == null)
            {
                ca = new ContactPhoneNumber() { Kind = phoneNumberKind };
                uc.PhoneNumbers.Add(ca);
            }
            ca.PhoneNumber = number;
        }

        private void UpdateEmailAddress(UnifiedContact uc, EmailAddressKind emailAddressKind, string address)
        {
            if (uc.EmailAddresses == null)
            {
                uc.EmailAddresses = new List<ContactEmailAddress>();
            }
            ContactEmailAddress ca = (from c in uc.EmailAddresses where c.Kind == emailAddressKind select c).FirstOrDefault();

            if (string.IsNullOrEmpty(address))
            {
                if (ca != null)
                {
                    uc.EmailAddresses.Remove(ca);
                }
                return;
            }
            else if (ca == null) 
            {
                ca = new ContactEmailAddress() { Kind = emailAddressKind };
                uc.EmailAddresses.Add(ca);
            }
            ca.EmailAddress = address;
        }

        private void UpdateAddress(UnifiedContact uc, AddressKind kind, string display, string street, string city, string state, string postcode, string country)
        {
            if (uc.Addresses == null)
            {
                uc.Addresses = new List<ContactAddress>();
            }
            ContactAddress ca = (from c in uc.Addresses where c.Kind == kind select c).FirstOrDefault();
            PhysicalAddress addr = (ca != null ? ca.PhysicalAddress : null);
            
            if (string.IsNullOrEmpty(street) && string.IsNullOrEmpty(city) && string.IsNullOrEmpty(state) && string.IsNullOrEmpty(postcode) && string.IsNullOrEmpty(country))
            {
                if (string.IsNullOrEmpty(display))
                {
                    if (ca != null)
                    {
                        uc.Addresses.Remove(ca);
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
            else if (addr == null)
            {
                // ignore the "display" string
                addr = new PhysicalAddress()
                {
                    AddressLine1 = street,
                    City = city,
                    StateProvince = state,
                    PostalCode = postcode,
                    CountryRegion = country
                };
            }
            else
            {
                addr.Building = addr.FloorLevel = null;
            }

            if (ca == null && addr != null)
            {
                uc.Addresses.Add(new ContactAddress() { Kind = kind, PhysicalAddress = addr });
            }
            else if (ca != null && ca.PhysicalAddress == null && addr != null)
            {
                ca.PhysicalAddress = addr;
            }
        }
        
    }
}

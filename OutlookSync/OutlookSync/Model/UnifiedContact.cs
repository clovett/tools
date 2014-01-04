using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OutlookSync
{
    // This serializable class stores unified contact information across windows phone and outlook.
    // We also store last update timestamps so we know which version of the information is the most 
    // up to date.
    public class UnifiedContact : PropertyBag
    {
        public UnifiedContact()
        {
        }
        
        public override bool IsEmpty 
        { 
            get { return CompleteName == null; } 
        }

        public string OutlookEntryId
        {
            get { return GetValue<string>("OutlookEntryId"); }
            set { SetValue<string>("OutlookEntryId", value); }
        }

        public PropertyList<ContactAddress> Addresses
        {
            get { return GetValue<PropertyList<ContactAddress>>("Addresses"); }
            private set { SetValue<PropertyList<ContactAddress>>("Addresses", value); }
        }

        public DateTimeOffset? Birthday
        {
            get { return GetValue<DateTimeOffset?>("Birthday"); }
            set { SetValue<DateTimeOffset?>("Birthday", value); }
        }

        public PersonName CompleteName
        {
            get { return GetValue<PersonName>("CompleteName"); }
            set { SetValue<PersonName>("CompleteName", value); }
        }

        public string DisplayName
        {
            get { return GetValue<string>("DisplayName"); }
            set { SetValue<string>("DisplayName", value); }
        }

        public PropertyList<ContactEmailAddress> EmailAddresses
        {
            get { return GetValue<PropertyList<ContactEmailAddress>>("EmailAddresses"); }
            private set { SetValue<PropertyList<ContactEmailAddress>>("EmailAddresses", value); }
        }

        public PropertyList<ContactPhoneNumber> PhoneNumbers
        {
            get { return GetValue<PropertyList<ContactPhoneNumber>>("PhoneNumbers"); }
            private set { SetValue<PropertyList<ContactPhoneNumber>>("PhoneNumbers", value); }
        }

        public string SignificantOthers
        {
            get { return GetValue<string>("SignificantOthers"); }
            set { SetValue<string>("SignificantOthers", value); }
        }

        public string Children
        {
            get { return GetValue<string>("Children"); }
            set { SetValue<string>("Children", value); }
        }

        public string Nickname
        {
            get { return GetValue<string>("Nickname"); }
            set { SetValue<string>("Nickname", value); }
        }

        public PropertyList<string> Websites
        {
            get { return GetValue<PropertyList<string>>("Websites"); }
            set { SetValue<PropertyList<string>>("Websites", value); }
        }

        public static UnifiedContact Parse(string xml)
        {
            UnifiedContact contact = new UnifiedContact();
            try
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
                {
                    UnifiedStoreSerializer serializer = new UnifiedStoreSerializer(reader);
                    while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.LocalName == "UnifiedContact")
                            {
                                contact.Read(serializer);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception parsing contact xml: " + ex.Message);
                return null;
            }
            return contact;
        }

        internal string ToXml()
        {
            StringWriter buffer = new StringWriter();
            using (XmlWriter writer = XmlWriter.Create(buffer))
            {                
                this.Write(writer);
            }
            return buffer.ToString();
        }

        internal void SetWebSites(IEnumerable<string> newSet)
        {
            if (Websites == null)
            {
                Websites = new PropertyList<string>();
                Websites.Parent = GetPropertyValue("Websites");
            }
            Websites.Clear();
            foreach (string s in newSet)
            {
                Websites.Add(s);
            }
        }


        internal ContactAddress GetAddress(AddressKind kind)
        {
            if (Addresses != null)
            {
                return (from a in Addresses where a.Kind == kind select a).FirstOrDefault();
            }
            return null;
        }

        internal void AddAddress(ContactAddress address)
        {
            if (Addresses == null)
            {
                Addresses = new PropertyList<ContactAddress>();
                Addresses.Parent = GetPropertyValue("Addresses");
            }
            Addresses.Add(address);
        }

        internal void RemoveAddress(AddressKind kind)
        {
            if (Addresses != null)
            {
                foreach (ContactAddress ca in (from a in Addresses.ToArray() where a.Kind == kind select a))
                {
                    // don't actually remove it, set the value to null so we remember this was deleted.
                    ca.PhysicalAddress = null;
                }
            }
        }

        internal ContactEmailAddress GetEmailAddress(EmailAddressKind kind)
        {
            if (EmailAddresses != null)
            {
                return (from a in EmailAddresses where a.Kind == kind select a).FirstOrDefault();
            }
            return null;
        }

        internal void AddEmailAddress(ContactEmailAddress address)
        {
            if (EmailAddresses == null)
            {
                EmailAddresses = new PropertyList<ContactEmailAddress>();
                EmailAddresses.Parent = GetPropertyValue("EmailAddresses");
            }
            EmailAddresses.Add(address);
        }

        internal void RemoveEmailAddress(EmailAddressKind kind)
        {
            if (EmailAddresses != null)
            {
                foreach (ContactEmailAddress ca in (from a in EmailAddresses.ToArray() where a.Kind == kind select a))
                {
                    // don't actually remove it from the list, instead set the address to null as a reminder that this was deleted.
                    ca.EmailAddress = null;
                }
            }
        }


        internal ContactPhoneNumber GetPhoneNumber(PhoneNumberKind kind)
        {
            if (PhoneNumbers != null)
            {
                return (from a in PhoneNumbers where a.Kind == kind select a).FirstOrDefault();
            }
            return null;
        }

        internal void AddPhoneNumber(ContactPhoneNumber number)
        {
            if (PhoneNumbers == null)
            {
                PhoneNumbers = new PropertyList<ContactPhoneNumber>();
                PhoneNumbers.Parent = GetPropertyValue("PhoneNumbers");
            }
            PhoneNumbers.Add(number);
        }

        internal void RemovePhoneNumber(PhoneNumberKind kind)
        {
            if (PhoneNumbers != null)
            {
                foreach (ContactPhoneNumber number in (from a in PhoneNumbers.ToArray() where a.Kind == kind select a))
                {
                    // don't actually remove it, set the value to null so we remember this was deleted.
                    number.PhoneNumber = null;
                }
            }
        }

    }

    public enum AddressKind
    {
        Home = 0,
        Work = 1,
        Other = 2
    }

    public class ContactAddress : PropertyBag
    {
        public override int? GetKey()
        {
            return (int)Kind;
        }

        public AddressKind Kind
        {
            get { return GetValue<AddressKind>("Kind"); }
            set { SetValue<AddressKind>("Kind", value); }
        }

        public PhysicalAddress PhysicalAddress
        {
            get { return GetValue<PhysicalAddress>("PhysicalAddress"); }
            set { SetValue<PhysicalAddress>("PhysicalAddress", value); }
        }

    }

    public class PhysicalAddress : PropertyBag
    {

        public string AddressLine1
        {
            get { return GetValue<string>("AddressLine1"); }
            set { SetValue<string>("AddressLine1", value); }
        }

        public string AddressLine2
        {
            get { return GetValue<string>("AddressLine2"); }
            set { SetValue<string>("AddressLine2", value); }
        }

        public string Building
        {
            get { return GetValue<string>("Building"); }
            set { SetValue<string>("Building", value); }
        }

        public string City
        {
            get { return GetValue<string>("City"); }
            set { SetValue<string>("City", value); }
        }

        public string CountryRegion
        {
            get { return GetValue<string>("CountryRegion"); }
            set { SetValue<string>("CountryRegion", value); }
        }

        public string FloorLevel
        {
            get { return GetValue<string>("FloorLevel"); }
            set { SetValue<string>("FloorLevel", value); }
        }

        public string PostalCode
        {
            get { return GetValue<string>("PostalCode"); }
            set { SetValue<string>("PostalCode", value); }
        }

        public string StateProvince
        {
            get { return GetValue<string>("StateProvince"); }
            set { SetValue<string>("StateProvince", value); }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string a = this.AddressLine1;
            if (!string.IsNullOrEmpty(a))
            {
                sb.AppendLine(a);
            } 

            a = this.AddressLine2;
            if (!string.IsNullOrEmpty(a))
            {
                sb.AppendLine(a);
            }

            a = this.Building;
            if (!string.IsNullOrEmpty(a))
            {
                sb.AppendLine(a);
            }

            a = this.FloorLevel;
            if (!string.IsNullOrEmpty(a))
            {
                sb.AppendLine(a);
            }

            string citystatezip = this.City;
            string state = this.StateProvince;
            if (!string.IsNullOrEmpty(state))
            {
                if (!string.IsNullOrEmpty(citystatezip))
                {
                    citystatezip += ", ";
                }
                citystatezip += state;
            }
            string zip = this.PostalCode;
            
            if (!string.IsNullOrEmpty(zip))
            {
                if (!string.IsNullOrEmpty(citystatezip))
                {
                    citystatezip += ", ";
                }
                citystatezip += zip;
            }
            if (!string.IsNullOrEmpty(citystatezip))
            {
                sb.AppendLine(citystatezip);
            }

            string country = this.CountryRegion;
            if (!string.IsNullOrEmpty(country))
            {
                sb.AppendLine(country);
            }
            return sb.ToString().Trim();
        }
    }

    public class PersonName : PropertyBag
    {
        public string FirstName
        {
            get { return GetValue<string>("FirstName"); }
            set { SetValue<string>("FirstName", value); }
        }

        public string LastName
        {
            get { return GetValue<string>("LastName"); }
            set { SetValue<string>("LastName", value); }
        }

        public string MiddleName
        {
            get { return GetValue<string>("MiddleName"); }
            set { SetValue<string>("MiddleName", value); }
        }

        public string Nickname
        {
            get { return GetValue<string>("Nickname"); }
            set { SetValue<string>("Nickname", value); }
        }

        public string Suffix
        {
            get { return GetValue<string>("Suffix"); }
            set { SetValue<string>("Suffix", value); }
        }

        public string Title
        {
            get { return GetValue<string>("Title"); }
            set { SetValue<string>("Title", value); }
        }        
    }

    public class ContactEmailAddress : PropertyBag
    {

        public override int? GetKey()
        {
            return (int)Kind;
        }

        public string EmailAddress
        {
            get { return GetValue<string>("EmailAddress"); }
            set { SetValue<string>("EmailAddress", value); }
        }

        public EmailAddressKind Kind
        {
            get { return GetValue<EmailAddressKind>("Kind"); }
            set { SetValue<EmailAddressKind>("Kind", value); }
        }
    }

    public enum EmailAddressKind
    {
        Personal = 0,
        Work = 1,
        Other = 2,
    }

    public enum PhoneNumberKind
    {
        Mobile = 0,
        Home = 1,
        Work = 2,
        Company = 3,
        Pager = 4,
        HomeFax = 5,
        WorkFax = 6,
    }

    public class ContactPhoneNumber : PropertyBag
    {
        public override int? GetKey()
        {
            return (int)Kind;
        }

        public PhoneNumberKind Kind
        {
            get { return GetValue<PhoneNumberKind>("Kind"); }
            set { SetValue<PhoneNumberKind>("Kind", value); }
        }

        public string PhoneNumber
        {
            get { return GetValue<string>("PhoneNumber"); }
            set { SetValue<string>("PhoneNumber", value); }
        }
    }

}


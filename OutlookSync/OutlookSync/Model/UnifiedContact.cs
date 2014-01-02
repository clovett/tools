using System;
using System.Collections;
using System.Collections.Generic;
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

        public bool IsEmpty
        {
            get
            {
                // if we don't know their name, then this is probably an outlook auto-generated contact.
                return CompleteName == null;
            }
        }

        public string OutlookEntryId
        {
            get { return GetValue<string>("OutlookEntryId"); }
            set { SetValue<string>("OutlookEntryId", value); }
        }

        public List<ContactAddress> Addresses
        {
            get { return GetValue<List<ContactAddress>>("Addresses");  }
            set { SetValue<List<ContactAddress>>("Addresses", value); }
        }

        public DateTime Birthday
        {
            get { return GetValue<DateTime>("Birthday"); }
            set { SetValue<DateTime>("Birthday", value); }
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

        public List<ContactEmailAddress> EmailAddresses
        {
            get { return GetValue<List<ContactEmailAddress>>("EmailAddresses"); }
            set { SetValue<List<ContactEmailAddress>>("EmailAddresses", value); }
        }

        public List<ContactPhoneNumber> PhoneNumbers
        {
            get { return GetValue<List<ContactPhoneNumber>>("PhoneNumbers"); }
            set { SetValue<List<ContactPhoneNumber>>("PhoneNumbers", value); }
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
        
        public List<string> Websites
        {
            get { return GetValue<List<string>>("Websites"); }
            set { SetValue<List<string>>("Websites", value); }
        }

        public static UnifiedContact Parse(string xml)
        {
            UnifiedContact contact = new UnifiedContact();
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

    }

    public enum AddressKind
    {
        Home = 0,
        Work = 1,
        Other = 2
    }

    public class ContactAddress : PropertyBag
    {
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


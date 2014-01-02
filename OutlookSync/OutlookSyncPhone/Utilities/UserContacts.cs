using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookSyncPhone
{
    /// <summary>
    /// This class manages the actual total user contacts as you see in the Personal hub
    /// </summary>
    class UserContacts
    {
        public void ReadContacts()
        {
            var contacts = new Microsoft.Phone.UserData.Contacts();
            contacts.SearchCompleted += OnSearchCompleted;
            contacts.SearchAsync("", Microsoft.Phone.UserData.FilterKind.None, this);
            
        }
        void OnSearchCompleted(object sender, Microsoft.Phone.UserData.ContactsSearchEventArgs e)
        {
            int start = Environment.TickCount;
            foreach (Microsoft.Phone.UserData.Contact contact in e.Results)
            {
                StringBuilder accounts = new StringBuilder();
                foreach (Microsoft.Phone.UserData.Account account in contact.Accounts)
                {
                    if (accounts.Length > 0)
                    {
                        accounts.Append(", ");
                    }
                    accounts.Append(account.Name);
                }
                if (contact.CompleteName != null)
                {
                    //Debug.WriteLine(contact.CompleteName.FirstName + " " + contact.CompleteName.LastName + "(" + accounts.ToString() + ")" );
                }
            }
            

            int end = Environment.TickCount;
            int milliseconds = end - start;
            Debug.WriteLine("Enumerated all contacts in " + milliseconds + " milliseconds");
        }

    }
}

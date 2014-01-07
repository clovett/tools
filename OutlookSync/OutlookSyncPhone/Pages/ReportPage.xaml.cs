using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using OutlookSync.Model;
using Microsoft.Phone.UserData;
using System.Diagnostics;
using System.Windows.Media;

namespace OutlookSyncPhone.Pages
{
    public partial class ReportPage : PhoneApplicationPage
    {

        public ReportPage()
        {
            InitializeComponent();
        }

        public List<ContactVersion> ContactList
        {
            get { return (List<ContactVersion>)ReportList.ItemsSource; }
            set { ReportList.ItemsSource = value;  }
        }

        public Brush TitleBackground
        {
            get { return TitlePanel.Background; }
            set { TitlePanel.Background = value; }
        }

        public string PageTitle
        {
            get { return PageTitleText.Text; }
            set { PageTitleText.Text = value; }
        }

        private void OnListItemSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContactVersion contact = ReportList.SelectedItem as ContactVersion;
            if (contact != null)
            {
                Contacts cons = new Contacts();
                
                //Identify the method that runs after the asynchronous search completes.
                cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(Contacts_SearchCompleted);

                //Start the asynchronous search.
                cons.SearchAsync(contact.Name, FilterKind.DisplayName, "search");
            }

        }

        private void Contacts_SearchCompleted(object sender, ContactsSearchEventArgs e)
        {
            foreach (Contact c in e.Results)
            {
                // found the contact, but now how do we navigate to the people hub to show it???
                Debug.WriteLine(c.DisplayName);
            }
        }

        private void OnBackPressed(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }



    }
}
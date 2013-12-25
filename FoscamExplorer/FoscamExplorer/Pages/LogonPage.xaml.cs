using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FoscamExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogonPage : Page
    {
        public LogonPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public string UserName
        {
            get { return TextBoxUserName.Text; }
            set { TextBoxUserName.Text = "" + value; }
        }

        public string Password
        {
            get { return TextBoxPassword.Password; }
            set { TextBoxPassword.Password = "" + value; }
        }

        private void OnLogin(object sender, RoutedEventArgs e)
        {
            this.CloseCurrentFlyout();
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            SettingsPane.Show();
        }
    }
}

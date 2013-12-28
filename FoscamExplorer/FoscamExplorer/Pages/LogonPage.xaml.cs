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
            this.Loaded += LogonPage_Loaded;
            // assume cancelled state unless user clicks the logon button.
            this.Cancelled = true;
            CheckBoxAll.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        void LogonPage_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxUserName.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

        public Visibility CheckboxVisibility
        {
            get { return CheckBoxAll.Visibility; }
            set { CheckBoxAll.Visibility = value; }
        }

        public string CheckBoxAllCaption
        {
            set { CheckBoxAll.Content = value; }
        }

        public bool CheckBoxAllIsChecked
        {
            get { return CheckBoxAll.IsChecked == true; }
        }

        public string Title
        {
            get { return pageTitle.Text; }
            set { pageTitle.Text = value; }
        }

        public string SignOnButtonCaption
        {
            get { return ButtonLoginCaption.Text; }
            set { ButtonLoginCaption.Text = value; }
        }

        public bool Cancelled { get; set; }

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
            this.Cancelled = false;
            this.CloseCurrentFlyout();
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            SettingsPane.Show();
        }

        private void OnUserNameGotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxUserName.SelectAll();
        }

        private void OnPasswordGotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.SelectAll();
        }

        private void OnPasswordKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.OnLogin(sender, e);
            }
        }
    }
}

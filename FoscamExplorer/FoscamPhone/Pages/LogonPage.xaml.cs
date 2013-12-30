using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace FoscamExplorer.Pages
{
    public partial class LogonPage : PhoneApplicationPage
    {

        public LogonPage()
        {
            this.InitializeComponent();
            this.Loaded += LogonPage_Loaded;
            CheckBoxAll.Visibility = Visibility.Collapsed;
        }

        void LogonPage_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxUserName.Focus();
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

        public event EventHandler LoginClicked;

        private void OnLogin(object sender, RoutedEventArgs e)
        {
            if (LoginClicked != null)
            {
                LoginClicked(this, EventArgs.Empty);
            }
            this.NavigationService.GoBack();
        }

        private void OnUserNameGotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxUserName.SelectAll();
        }

        private void OnPasswordGotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxPassword.SelectAll();
        }

        private void OnPasswordKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.OnLogin(sender, e);
            }
        }


    }
}
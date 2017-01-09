using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NetworkDataUsage.Controls
{
    public sealed partial class LoginPanel : UserControl
    {
        bool cancelled = false;

        public LoginPanel()
        {
            this.InitializeComponent();
        }

        public string Prompt
        {
            get { return PromptText.Text; }
            set { PromptText.Text = "" + value; }
        }

        public bool RememberCredentials
        {
            get { return RememberCredentialsCheckBox.IsChecked == true; }
            set { RememberCredentialsCheckBox.IsChecked = value; }
        }

        public string UserName
        {
            get { return UserNameText.Text;  }
            set { UserNameText.Text = value; }
        }

        public string Password
        {
            get { return PasswordText.Password; }
            set { PasswordText.Password = value; }
        }

        public bool Cancelled {  get { return this.cancelled; } }

        public event EventHandler OkCancelClick;

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            cancelled = false;
            OnOkCancel();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            cancelled = true;
            OnOkCancel();
        }

        void OnOkCancel()
        {
            if (OkCancelClick != null)
            {
                OkCancelClick(this, EventArgs.Empty);
            }
        }

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            box.SelectAll();
        }

        private void OnPasswordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox box = (PasswordBox)sender;
            box.SelectAll();
        }

        private void OnPasswordKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                cancelled = false;
                OnOkCancel();
            }
        }

        private void OnUserNameKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                PasswordText.Focus();
            }
        }
    }
}

using NetgearDataUsage.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NetworkDataUsage.Controls
{
    public sealed partial class SettingsPanel : UserControl
    {
        bool cancelled;

        public SettingsPanel()
        {
            this.InitializeComponent();

            TargetUsage.Text = Settings.Instance.TargetUsage.ToString();
        }

        public bool IsCancelled {  get { return this.cancelled;  } }

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            box.SelectAll();
        }

        public event EventHandler OkCancelClick;

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "";
            OKButton.IsEnabled = false;
            cancelled = false;

            int v = 0;
            if (int.TryParse(TargetUsage.Text, out v))
            {
                Settings.Instance.TargetUsage = v;
                Settings.Instance.TrafficMeterUri = TrafficMeterUri.Text;
                await Settings.Instance.SaveAsync();
                OnOkCancel();
            }
            else
            {
                StatusText.Text = "Please enter a valid number";
                TargetUsage.Focus();
            }

            OKButton.IsEnabled = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "";
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

    }
}

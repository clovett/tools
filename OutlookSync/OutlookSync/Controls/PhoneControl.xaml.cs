using OutlookSync.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutlookSync.Controls
{
    /// <summary>
    /// Interaction logic for PhoneControl.xaml
    /// </summary>
    public partial class PhoneControl : UserControl
    {
        ConnectedPhone phone;

        public PhoneControl()
        {
            InitializeComponent();

            this.DataContextChanged += PhoneControl_DataContextChanged;
        }

        void PhoneControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            phone = (ConnectedPhone)e.NewValue;
            if (phone != null)
            {
                ConnectButton.Visibility = (phone.Allowed ? Visibility.Collapsed : System.Windows.Visibility.Visible);
            }
        }        

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (phone != null)
            {
                phone.Allowed = true;
                ConnectButton.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }
}

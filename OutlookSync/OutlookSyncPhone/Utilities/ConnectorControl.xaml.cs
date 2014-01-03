using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace OutlookSyncPhone.Utilities
{
    public partial class ConnectorControl : UserControl
    {
        public ConnectorControl()
        {
            InitializeComponent();
        }


        public bool Connected
        {
            get { return (bool)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Connected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(bool), typeof(ConnectorControl), new PropertyMetadata(false, new PropertyChangedCallback(OnConnectedChanged)));

        private static void OnConnectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ConnectorControl)d).OnConnectedChanged();
        }

        private void OnConnectedChanged()
        {
            PathOpen.Visibility = Connected ? Visibility.Collapsed : Visibility.Visible;
            PathClosed.Visibility = Connected ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

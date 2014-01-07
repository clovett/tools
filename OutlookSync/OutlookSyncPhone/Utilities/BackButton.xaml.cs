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
    public partial class BackButton : UserControl
    {
        public BackButton()
        {
            InitializeComponent();
        }

        public event EventHandler Click;

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            Fill.Visibility = System.Windows.Visibility.Visible;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            Fill.Visibility = System.Windows.Visibility.Collapsed;
            base.OnMouseLeftButtonUp(e);
            if (Click != null)
            {
                Click(this, EventArgs.Empty);
            }
        }
    }
}

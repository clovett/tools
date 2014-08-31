using Microsoft.Journal.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Microsoft.Journal.Controls
{
    public sealed partial class JournalListItem : UserControl
    {
        public JournalListItem()
        {
            this.InitializeComponent();
            this.Loaded += JournalListItem_Loaded;
            this.Unloaded += JournalListItem_Unloaded;
        }

        void JournalListItem_Unloaded(object sender, RoutedEventArgs e)
        {
            ListView list = this.FindParent<ListView>();
            if (list != null)
            {
                list.SelectionChanged -= OnSelectionChanged;
            }
        }

        void JournalListItem_Loaded(object sender, RoutedEventArgs e)
        {
            ListViewItem item = this.FindParent<ListViewItem>();      
            if (item != null && item.IsSelected)
            {
                GotoEditState();
            }

            ListView list = this.FindParent<ListView>();
            if (list != null)
            {
                list.SelectionChanged += OnSelectionChanged;
            }
        }

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var context = this.DataContext;

            foreach (var item in e.RemovedItems)
            {
                if (item == context)
                {
                    GotoViewState();
                    break;
                }
            }

            foreach (var item in e.AddedItems)
            {
                if (item == context)
                {
                    GotoEditState();
                    break;
                }
            }
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            box.SelectAll();
        }


        internal void GotoViewState()
        {
            VisualStateManager.GoToState(this, "View", true);
        }

        internal void GotoEditState()
        {
            VisualStateManager.GoToState(this, "Edit", true);
        }
    }
}

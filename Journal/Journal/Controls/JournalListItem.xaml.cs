using Microsoft.Journal.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
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
    public sealed partial class JournalEntryControl : UserControl
    {
        public JournalEntryControl()
        {
            this.InitializeComponent();
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(JournalEntryControl), new PropertyMetadata(false, new PropertyChangedCallback(OnSelectedChanged)));

        private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((JournalEntryControl)d).OnSelectedChanged();
        }

        public event EventHandler Selected;


        private void OnSelectedChanged()
        {

            if (Selected != null && this.IsSelected)
            {
                Selected(this, EventArgs.Empty);
            }

            var context = this.DataContext;

            if (this.IsSelected)
            {
                GotoEditState();
            } else {
                GotoViewState();
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
            if (!VisualStateManager.GoToState(this, "Edit", true))
            {
                Debug.WriteLine("Goto edit state failed???");
            }
        }

    }
}

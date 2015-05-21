using System;
using System.Collections.Generic;
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

namespace SharedLibrary
{
    public sealed partial class LogListView : UserControl
    {
        LogBook log = new LogBook();

        public LogListView()
        {
            this.InitializeComponent();

            this.LogEntryList.ItemsSource = log.Entries;
        }

        public LogBook LogBook {  get { return this.log;  } }

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        private void OnListItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, e);
            }
        }
    }
}

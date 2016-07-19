using SharedLibrary;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GasBook
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.SizeChanged += OnSizeChanged;

            var book = LogListView.LogBook;
            var rand = new Random();
            DateTime start = DateTime.Today.AddDays(-100);
            for (int i = 0; i < 100; i++)
            {
                book.Entries.Add(new LogBookEntry()
                {
                    Amount = (decimal)rand.NextDouble() * 50,
                    Date = start,
                    Gallons = (decimal)rand.NextDouble() * 16,
                    Mpg = (decimal)rand.NextDouble() * 30
                });
                start = start.AddDays(1);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= 600)
            {
                DetailView.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(LogListView, 2);
            }
            else
            {
                DetailView.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(LogListView, 1);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                LogBookEntry entry = e.AddedItems[0] as LogBookEntry;
                if (entry != null)
                {
                    if (DetailView.Visibility == Visibility.Visible)
                    {
                        DetailView.DataContext = entry;
                    }
                    else
                    {
                        this.Frame.Navigate(typeof(AddEntryPage), entry);
                    }
                }
            }
        }
    }
}

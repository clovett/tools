using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dashboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ObservableCollection<UsageData> context = new ObservableCollection<UsageData>();
            DateTime date = new DateTime(2013, 1, 1);

            for (int i = 0; i < data.Length; i++)
            {
                context.Add(new UsageData() { Date = date, Usage = data[i] });
                date = date.AddDays(15);
            }

            ColumnSeries series = ChartByDay.Series.FirstOrDefault() as ColumnSeries;
            series.ItemsSource = context;

            DateTimeAxis axis = series.ActualIndependentAxis as DateTimeAxis;
            axis.Minimum = new DateTime(2013, 1, 1);
            axis.Maximum = new DateTime(2014, 1, 1);
            axis.Interval = 5;
        }

        static double[] data = new double[] { 10, 15, 20, 22, 25, 29, 35, 45, 42, 40, 35, 30, 31, 32, 31, 30, 35, 40, 42, 44, 40, 35, 36, 42 };

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }

    class UsageData
    {
        public DateTime Date { get; set; }
        public double Usage { get; set; }
    }
}

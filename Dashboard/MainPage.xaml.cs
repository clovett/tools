using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization;
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

        static TimeSpan AnimationDelay = TimeSpan.FromMilliseconds(200);

        RadialDialDataValue CreateDialValue(double angle, string caption, string valueLabel)
        {
            var context = new RadialDialDataValue()
            {
                Angle = angle,
                Caption = caption,
                ValueLabel = valueLabel
            };

            BeginAngleAnimation(context, 0, angle, AnimationDelay);
            return context;
        }

        void BeginAngleAnimation(DependencyObject target, double from, double to, TimeSpan duration)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation() { From = from, To = to, Duration = new Duration(duration) };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, "(RadialDialDataValue.Angle)");
            sb.Children.Add(animation);
            animation.EnableDependentAnimation = true;
            sb.Begin();
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

            UsesPerDayDial.DataContext = CreateDialValue(360.0 * (75.0 / 100.0), "used daily", "75");
            ErrorsPerDayDial.DataContext = CreateDialValue(360.0 * (25.0 / 100.0), "service needed", "25");
            DistributionDial.DataContext = CreateDialValue(360.0 * (60.0 / 100.0), "shared", "60");
            UnusedPerDayDial.DataContext = CreateDialValue(360.0 * (10.0 / 100.0), "unused","10");
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

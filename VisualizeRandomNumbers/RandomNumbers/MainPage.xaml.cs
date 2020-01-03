using RandomNumbers.Controls;
using RandomNumbers.SharedControls;
using RandomNumbers.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Core;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RandomNumbers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int SampleSize = 5000;
        CancellationTokenSource refreshSource;
        enum GeneratorType
        {
            default_random_engine,
            SystemRandom
        }

        public MainPage()
        {
            this.InitializeComponent();

            ComboGenerators.Items.Add(GeneratorType.default_random_engine);
            ComboGenerators.Items.Add(GeneratorType.SystemRandom);
            ComboGenerators.SelectedIndex = 0;
            ComboGenerators.SelectionChanged += ComboGenerators_SelectionChanged;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            TextBoxSampleSize.Text = SampleSize.ToString();
        }

        private void ComboGenerators_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F5)
            {
                Refresh();
            }
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Refresh();
        }

        private void Refresh()
        {
            switch ((GeneratorType)ComboGenerators.SelectedItem)
            {
                case GeneratorType.default_random_engine:
                    UiDispatcher.RunOnUIThread(GenerateGraph);
                    break;
                case GeneratorType.SystemRandom:
                    UiDispatcher.RunOnUIThread(GenerateRandomGraph);
                    break;
            }

        }

        int GetSeed()
        {
            return (int)DateTime.Now.Ticks;
        }

        private void Cancel()
        {
            if (refreshSource != null)
            {
                refreshSource.Cancel();
            }
            refreshSource = new CancellationTokenSource();
        }

        private void GenerateGraph()
        {
            Cancel();
            Generator.RandomGenerator gen = new Generator.RandomGenerator(GetSeed());
            List<DataValue> values = new List<DataValue>();
            SolidColorBrush c = new SolidColorBrush(Colors.Blue);
            for (int i = 0; i < SampleSize; i++)
            {
                values.Add(new DataValue() { X = i, Y = gen.GetNext(), Color = c });
            }
            this.Plot.Token = refreshSource.Token;
            this.Plot.SetData(values);
            
            ShowDistribution(values);
        }

        void GenerateRandomGraph()
        {
            Cancel();
            Random rand = new Random(GetSeed());
            List<DataValue> values = new List<DataValue>();
            SolidColorBrush c = new SolidColorBrush(Colors.Blue);
            for (int i = 0; i < SampleSize; i++)
            {
                values.Add(new DataValue() { X = i, Y = rand.NextDouble(), Color = c });
            }
            this.Plot.Token = refreshSource.Token;
            this.Plot.SetData(values);

            ShowDistribution(values);
        }

        private void ShowDistribution(List<DataValue> values)
        {
            SolidColorBrush green = new SolidColorBrush(Colors.Green);
            List<DataValue> distribution = new List<Controls.DataValue>();
            List<double> yValues = new List<double>(from d in values select d.Y);
            double min = yValues.Min();
            double max = yValues.Max();
            double range = max - min;
            double mean = MathHelpers.Mean(yValues);
            // count number of y values in each strip to build a histogram of the number distribution.
            double slice = range / 100;
            double x = 0;
            for (double dist = mean - range; dist < mean + range; dist += slice)
            {
                double count = (from d in values where d.Y >= dist - (2*slice) && d.Y < dist + (2*slice) select d).Count();
                distribution.Add(new DataValue() { X = x++, Y = count, Color = green });
            }

            this.Plot.Token = refreshSource.Token;
            this.LineChart.SetData(distribution);
        }

        private void OnSampleSizeChanged(object sender, TextChangedEventArgs e)
        {
            string text = TextBoxSampleSize.Text;
            int v;
            if (int.TryParse(text, out v))
            {
                SampleSize = v;
                Refresh();
            }
        }
    }
}

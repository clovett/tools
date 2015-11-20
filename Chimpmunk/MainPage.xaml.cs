using Chimpmunk.Controls;
using Chimpmunk.Model;
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
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Chimpmunk
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Job job = new Job();

        public MainPage()
        {
            this.InitializeComponent();
            PatternListView.SizeChanged += PatternListView_SizeChanged;
        }

        private void PatternListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Rescale();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            job = await Job.LoadAsync();
            if (job == null)
            {
                job = new Job();
            }

            PatternListView.ItemsSource = job.Patterns;
            StockListView.ItemsSource = job.StockPile;
            Rescale();
            Solve();
            UpdateButtonState();
        }

        private void OnQuantityChanged(object sender, TextChangedEventArgs e)
        {
            ShowError(QuantityMessage, "");
            UpdateButtonState();
        }

        private void OnLengthChanged(object sender, TextChangedEventArgs e)
        {
            ShowError(LengthMessage, "");
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            AddPatternButton.IsEnabled = AddStockButton.IsEnabled = !string.IsNullOrEmpty(TextBoxQty.Text) && !string.IsNullOrEmpty(TextBoxLength.Text);
        }

        IEnumerable<Pattern> ParsePatterns()
        {
            int qty = 0;
            if (!int.TryParse(TextBoxQty.Text, out qty))
            {
                ShowError(QuantityMessage, "Please enter valid quantity");

            }
            else
            {
                double length = 0;
                if (!double.TryParse(TextBoxLength.Text, out length))
                {
                    ShowError(LengthMessage, "Please enter valid length");
                }
                else
                {
                    NamedColor c = ColorTable.Instance.GetRandomColor();

                    for (int i = 0; i < qty; i++)
                    {
                        yield return new Pattern() { Length = length, Color = c.Name };
                    }
                }
            }
        }


        private async void OnAddPattern(object sender, RoutedEventArgs e)
        {
            foreach (var pattern in ParsePatterns())
            {
                job.Patterns.Add(pattern);
            }
            Rescale();
            Solve();
            await job.SaveAsync();
        }
        private async void OnAddStock(object sender, RoutedEventArgs e)
        {
            foreach (var pattern in ParsePatterns())
            {
                job.StockPile.Add(pattern);
            }
            Rescale();
            Solve();
            await job.SaveAsync();
        }


        private void Rescale()
        {
            double available = PatternListView.ActualWidth - 8;

            double max = double.MinValue;
            foreach (Pattern p in job.Patterns)
            {
                max = Math.Max(max, p.Length);
            }

            foreach (Pattern p in job.StockPile)
            {
                max = Math.Max(max, p.Length);
            }

            double scale = available / max;

            foreach (Pattern p in job.Patterns)
            {
                p.ScaledLength = p.Length * scale;
            }

            foreach (Pattern p in job.StockPile)
            {
                p.ScaledLength = p.Length * scale;
            }
        }

        private void Solve()
        { 
            if (job.Solve())
            {
                ErrorMessage.Text = "";
            }
            else
            {
                ErrorMessage.Text = "Not enough stock";
            }
        }

        private void ShowError(TextBlock text, string msg)
        {
            text.Text = msg;
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            job.Patterns.Clear();
            job.StockPile.Clear();
            ErrorMessage.Text = "";
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using BarChart.Utilities;
using BarChart.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace BarChart.Views
{
    public partial class MainView : UserControl
    {
        DelayedActions delayedActions = new DelayedActions();
        const double PlaySpeedSeconds = 2;

        public MainView()
        {
            UiDispatcher.Initialize();
            InitializeComponent();

            this.Chart.Orientation = Orientation.Horizontal;
            this.Chart.ColumnHover += OnColumnHover;
            this.Chart.ColumnClicked += OnColumnClicked;
            this.Chart.ToolTipGenerator = OnGenerateTip;
            this.Chart.Foreground = Brushes.SlateGray;

            this.RefreshButton.Click += OnRefresh;
            this.PlayButton.Click += OnPlay;
            this.PauseButton.Click += OnPause;
            this.RotateButton.Click += OnRotate;
            this.AddButton.Click += OnAddSeries;
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainViewModel model)
            {
                var data = model.ChartData;
                model.ChartData = null;
                model.ChartData = data;
            }
        }

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            PauseButton.IsVisible = true;
            PlayButton.IsVisible = false;
            Next();
            delayedActions.StartDelayedAction("play", MoveNext, TimeSpan.FromSeconds(PlaySpeedSeconds));
        }

        private void MoveNext()
        {
            Next();
            delayedActions.StartDelayedAction("play", MoveNext, TimeSpan.FromSeconds(PlaySpeedSeconds));
        }

        private void OnRotate(object sender, RoutedEventArgs e)
        {
            Chart.Orientation = Chart.Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            delayedActions.CancelDelayedAction("play");
            PauseButton.IsVisible = false;
            PlayButton.IsVisible = true;
        }

        private Visual OnGenerateTip(ChartDataValue value)
        {
            return null;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            Next();
            base.OnDataContextChanged(e);
        }

        private void OnColumnHover(object sender, ChartDataValue e)
        {
            Debug.WriteLine("OnColumnHover: " + e.Label + " = " + e.Value);
        }

        private void OnColumnClicked(object sender, ChartDataValue e)
        {
            Next();
        }


        public void Next()
        {
            if (this.DataContext is MainViewModel model)
            {
                var ds = model.GetNextSeries();
                var data = new ChartViewModel();
                data.Series.Add(ds);
                model.ChartData = data;
            }
        }

        public void Previous()
        {
            if (this.DataContext is MainViewModel model)
            {
                var ds = model.GetPreviousSeries();
                var data = new ChartViewModel();
                data.Series.Add(ds);
                model.ChartData = data;
            }
        }

        private void OnAddSeries(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainViewModel model)
            {
                var data = model.ChartData;
                var series = data.Series.Last();
                var newSeries = new ChartDataSeries() { Flipped = series.Flipped, Category = series.Category };
                foreach (var value in series.Values)
                {
                    var newValue = new ChartDataValue()
                    {
                        Color = NextSeriesColor(value.Color.Value),
                        Label = value.Label,
                        Value = value.Value * 0.9
                    };
                    newSeries.Values.Add(newValue);
                }
                data.Series.Add(newSeries);
                model.ChartData = null;
                model.ChartData = data;
            }
        }

        private Color NextSeriesColor(Color c)
        {
            var hls = new HlsColor(c);
            hls.Darken(0.25f);
            return hls.Color;
        }

    }
}
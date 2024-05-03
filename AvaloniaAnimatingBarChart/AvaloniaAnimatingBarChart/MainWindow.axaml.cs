using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaAnimatingBarChart.Controls;
using AvaloniaAnimatingBarChart.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace AvaloniaAnimatingBarChart
{
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();

        List<ChartDataSeries> datasets = new List<ChartDataSeries>();
        int dataset = 0;
        const double PlaySpeedSeconds = 2;

        public MainWindow()
        {
            UiDispatcher.Initialize();
            InitializeComponent();
            RestoreSettings();

            this.SizeChanged += OnWindowSizeChanged;
            this.PositionChanged += OnWindowPositionChanged;
            Settings.Instance.PropertyChanged += OnSettingsChanged;

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

            PauseButton.IsVisible = false;

            LoadSamples();
            Toggle();
        }

        private void OnAddSeries(object sender, RoutedEventArgs e)
        {
            if (Chart.IsVisible)
            {
                var data = Chart.Data;
                var ds = GetNext();
                while (ds.Values.Count != data.Series[0].Values.Count)
                {
                    ds = GetNext();
                }
                data.Series.Add(ds);
                Chart.Data = null;
                Chart.Data = data;
            }
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            delayedActions.CancelDelayedAction("play");
            PauseButton.IsVisible = false;
            PlayButton.IsVisible = true;
        }

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            PauseButton.IsVisible = true;
            PlayButton.IsVisible = false;
            Toggle();
            delayedActions.StartDelayedAction("play", MoveNext, TimeSpan.FromSeconds(PlaySpeedSeconds));
        }

        private void MoveNext()
        {
            Toggle();
            delayedActions.StartDelayedAction("play", MoveNext, TimeSpan.FromSeconds(PlaySpeedSeconds));
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            var data = Chart.Data;
            Chart.Data = null;
            Chart.Data = data;
            // var pieData = PieChart.Series;
            //PieChart.Series = null;
            //PieChart.Series = pieData;
        }

        ChartDataSeries GetNext()
        {
            var ds = datasets[dataset];
            dataset++;
            if (dataset >= datasets.Count)
            {
                dataset = 0;
            }
            return ds;
        }

        void Toggle()
        {
            var ds = GetNext();
            var data = new ChartData();
            data.Series.Add(ds);
            Chart.Data = data;
            if (ds == datasets[0]) {
                // wrap around, toggle layout style
                OnRotate(this, null);
            }
            
            // PieChart.Series = CreatePieData(ds);
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            //Microsoft.Win32.OpenFileDialog od = new Microsoft.Win32.OpenFileDialog();
            //od.Filter = "CSV files (*.csv)|*.csv";
            //if (od.ShowDialog() == true)
            //{
            //    Open(od.FileName);
            //}
        }

        private void OnRotate(object sender, RoutedEventArgs e)
        {
            Chart.Orientation = Chart.Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        }


        private Visual OnGenerateTip(ChartDataValue value)
        {
            var tip = new StackPanel() { Orientation = Orientation.Vertical };
            tip.Children.Add(new TextBlock() { Text = value.Label, FontWeight = Avalonia.Media.FontWeight.Bold });
            tip.Children.Add(new TextBlock() { Text = value.Value.ToString("C0") });
            return tip;
        }

        private void OnColumnHover(object sender, ChartDataValue e)
        {
            Debug.WriteLine("OnColumnHover: " + e.Label + " = " + e.Value);
        }

        private void OnColumnClicked(object sender, ChartDataValue e)
        {
            Toggle();
        }

        void LoadSamples()
        {
            datasets.Add(new ChartDataSeries()
            {
                Name = "Landscaping",
                Values = new List<ChartDataValue>()
                {
                    new ChartDataValue() { Label = "2002", Value = 14813.67 },
                    new ChartDataValue() { Label = "2003", Value = 13260.92 },
                    new ChartDataValue() { Label = "2004", Value = 18412.61 },
                    new ChartDataValue() { Label = "2005", Value = 16824.06 },
                    new ChartDataValue() { Label = "2006", Value = 18653.40 },
                    new ChartDataValue() { Label = "2007", Value = 20828.9  },
                    new ChartDataValue() { Label = "2008", Value = 20534.97 },
                    new ChartDataValue() { Label = "2009", Value = 19595.84 },
                    new ChartDataValue() { Label = "2010", Value = 17665.99 },
                    new ChartDataValue() { Label = "2011", Value = 19748.83 },
                    new ChartDataValue() { Label = "2012", Value = 18100.11 },
                    new ChartDataValue() { Label = "2013", Value = 19053.02 },
                    new ChartDataValue() { Label = "2014", Value = 19971.89 },
                    new ChartDataValue() { Label = "2015", Value = 17086.59 },
                    new ChartDataValue() { Label = "2016", Value = 18769.17 },
                    new ChartDataValue() { Label = "2017", Value = 21448.2  },
                    new ChartDataValue() { Label = "2018", Value = 22470.60 },
                    new ChartDataValue() { Label = "2019", Value = 20611.92 },
                    new ChartDataValue() { Label = "2020", Value = 25070.87 },
                    new ChartDataValue() { Label = "2021", Value = 25200.55 }
                }
            });

            datasets.Add(new ChartDataSeries()
            {
                Name = "Home",
                Values = new List<ChartDataValue>()
                {
                    new ChartDataValue() { Label = "2002",    Value = 2678.25 },
                    new ChartDataValue() { Label = "2003",    Value = 3461.95 },
                    new ChartDataValue() { Label = "2004",    Value = 3034.71 },
                    new ChartDataValue() { Label = "2005",    Value = 2540.69 },
                    new ChartDataValue() { Label = "2006",    Value = 2587.59 },
                    new ChartDataValue() { Label = "2007",    Value = 2729.91 },
                    new ChartDataValue() { Label = "2008",    Value = 2660.03 },
                    new ChartDataValue() { Label = "2009",    Value = 1678.28 },
                    new ChartDataValue() { Label = "2010",    Value = 1811.17 },
                    new ChartDataValue() { Label = "2011",    Value = 2332.09 },
                    new ChartDataValue() { Label = "2012",    Value = 2109.82 },
                    new ChartDataValue() { Label = "2013",    Value = 1227.98 },
                    new ChartDataValue() { Label = "2014",    Value = 1880.62 },
                    new ChartDataValue() { Label = "2015",    Value = 2053.86 },
                    new ChartDataValue() { Label = "2016",    Value = 2019.01 },
                    new ChartDataValue() { Label = "2017",    Value = 2702.83 },
                    new ChartDataValue() { Label = "2018",    Value = 2717.44 },
                    new ChartDataValue() { Label = "2019",    Value = 1638.22 },
                    new ChartDataValue() { Label = "2020",    Value = 1903.7  },
                    new ChartDataValue() { Label = "2021",    Value = -836.56 }
                }
            });

            datasets.Add(new ChartDataSeries()
            {
                Name = "Fun",
                Values = new List<ChartDataValue>()
                {
                    new ChartDataValue() { Label = "14 January", Value = 158.22   , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 February", Value = 226.5   , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 March", Value = 291.19     , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 April", Value = 430.28     , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 May", Value = 140.75       , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 June", Value = 139.82      , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 July", Value = 448.72      , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 August", Value = 149.81    , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 September", Value = 75.84  , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 October", Value = 40.97    , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 November", Value = 140.52  , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 December", Value = 705.66  , Color = Color.FromRgb(0xA3, 0x00, 0x27)   }
                }
            });

            // handy test...
            //datasets.Add(new ChartDataSeries()
            //{
            //    Name = "Test",
            //    Values = new List<ChartDataValue>()
            //    {
            //        new ChartDataValue() { Label = "Singleton!", Value = 705.66 }
            //    }
            //});
        }

        private void OnWindowPositionChanged(object sender, PixelPointEventArgs e)
        {
            if (this.WindowState == WindowState.Normal && Settings.Instance != null)
            {
                Settings.Instance.WindowLocation = e.Point;
                delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal && Settings.Instance != null)
            {
                Settings.Instance.WindowSize = e.NewSize;
                delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                UpdateTheme();
            }
            saveSettingsPending = true;
            delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
        }

        private bool saveSettingsPending;

        void OnSaveSettings()
        {
            if (saveSettingsPending)
            {
                saveSettingsPending = false;
                delayedActions.CancelDelayedAction("SaveSettings");
                if (Settings.Instance != null)
                {
                    try
                    {
                        Settings.Instance.Save();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (saveSettingsPending)
            {
                // then we need to do synchronous save and cancel any delayed action.
                OnSaveSettings();
            }

            base.OnClosing(e);
        }

        private void RestoreSettings()
        {
            Settings settings = Settings.Instance;
            if (settings != null)
            {
                if (settings.WindowLocation.X != 0 && settings.WindowLocation.Y != 0)
                {
                    var topMargin = this.FrameSize.Value.Height - this.ClientSize.Height;
                    var leftMargin = (this.FrameSize.Value.Width - this.ClientSize.Width) / 2;
                    this.Position = settings.WindowLocation
                        .WithY((int)(settings.WindowLocation.Y - topMargin))
                        .WithX((int)(settings.WindowLocation.X - leftMargin));
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                }
                if (settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
                {
                    this.Width = settings.WindowSize.Width;
                    this.Height = settings.WindowSize.Height;
                }
            }
            UpdateTheme();
        }

        void UpdateTheme()
        {
            if (Settings.Instance != null)
            {
                switch (Settings.Instance.Theme)
                {
                    case AppTheme.Dark:
                        this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                        break;
                    case AppTheme.Light:
                        this.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                        break;
                    default:
                        break;
                }
            }
        }

    }
}